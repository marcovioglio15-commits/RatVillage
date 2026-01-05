using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_SocietyClock))]
    [UpdateAfter(typeof(EM_System_NpcSchedule))]
    public partial struct EM_System_NeedUpdate : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_NeedSignalSettings> signalSettingsLookup;
        private BufferLookup<EM_BufferElement_NeedSignalOverride> needSignalOverrideLookup;
        private BufferLookup<EM_BufferElement_NeedActivityRate> needActivityRateLookup;
        private ComponentLookup<EM_Component_NpcScheduleState> scheduleStateLookup;
        #endregion

        #region Unity Lifecycle
        // Configure lookups for need updates and signal emission.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_BufferElement_NeedSetting>();
            signalSettingsLookup = state.GetComponentLookup<EM_Component_NeedSignalSettings>(true);
            needSignalOverrideLookup = state.GetBufferLookup<EM_BufferElement_NeedSignalOverride>(true);
            needActivityRateLookup = state.GetBufferLookup<EM_BufferElement_NeedActivityRate>(true);
            scheduleStateLookup = state.GetComponentLookup<EM_Component_NpcScheduleState>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<Entity, NeedTickData> deltaHoursMap = BuildDeltaHoursMap(ref state);

            if (deltaHoursMap.Count() == 0)
            {
                deltaHoursMap.Dispose();
                return;
            }

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                EM_Component_Log debugLog = SystemAPI.GetSingleton<EM_Component_Log>();
                maxEntries = debugLog.MaxEntries;
            }

            signalSettingsLookup.Update(ref state);
            needSignalOverrideLookup.Update(ref state);
            needActivityRateLookup.Update(ref state);
            scheduleStateLookup.Update(ref state);

            foreach ((DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
                DynamicBuffer<EM_BufferElement_SignalEvent> signals, EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Need>, DynamicBuffer<EM_BufferElement_NeedSetting>,
                    DynamicBuffer<EM_BufferElement_SignalEvent>, EM_Component_SocietyMember>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                NeedTickData tickData;
                bool found = deltaHoursMap.TryGetValue(member.SocietyRoot, out tickData);

                if (!found)
                    continue;

                EM_Component_NeedSignalSettings signalSettings = default;
                bool hasSignalSettings = member.SocietyRoot != Entity.Null && signalSettingsLookup.HasComponent(member.SocietyRoot);
                bool hasOverrides = member.SocietyRoot != Entity.Null && needSignalOverrideLookup.HasBuffer(member.SocietyRoot);
                DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides = default;

                if (hasSignalSettings)
                    signalSettings = signalSettingsLookup[member.SocietyRoot];

                if (hasOverrides)
                    overrides = needSignalOverrideLookup[member.SocietyRoot];

                FixedString64Bytes activityId = default;
                bool hasScheduleState = scheduleStateLookup.HasComponent(entity);

                if (hasScheduleState)
                    activityId = scheduleStateLookup[entity].CurrentActivityId;

                DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates = default;
                bool hasActivityRates = needActivityRateLookup.HasBuffer(entity);

                if (hasActivityRates)
                    activityRates = needActivityRateLookup[entity];

                ApplyNeedSettings(needs, settings, tickData.DeltaHours, activityId, activityRates, hasActivityRates,
                    signalSettings, hasSignalSettings, overrides, hasOverrides,
                    signals, entity, member.SocietyRoot, tickData.TimeSeconds, hasDebugBuffer, debugBuffer, maxEntries);
            }

            deltaHoursMap.Dispose();
        }
        #endregion

        #region Helpers
        // Compute per-society delta hours based on tick settings.
        private NativeParallelHashMap<Entity, NeedTickData> BuildDeltaHoursMap(ref SystemState state)
        {
            NativeParallelHashMap<Entity, NeedTickData> deltaHoursMap = new NativeParallelHashMap<Entity, NeedTickData>(8, Allocator.Temp);

            foreach ((RefRW<EM_Component_NeedTickState> tickState, RefRO<EM_Component_NeedTickSettings> settings,
                RefRO<EM_Component_SocietyClock> clock, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_NeedTickState>, RefRO<EM_Component_NeedTickSettings>, RefRO<EM_Component_SocietyClock>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TickRate);
                double timeSeconds = clock.ValueRO.SimulatedTimeSeconds;

                if (timeSeconds < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = timeSeconds + intervalSeconds;
                deltaHoursMap.TryAdd(entity, new NeedTickData
                {
                    DeltaHours = intervalSeconds / 3600f,
                    TimeSeconds = timeSeconds
                });
            }

            return deltaHoursMap;
        }

        private static float GetIntervalSeconds(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        // Apply decay curves and emit need signals.
        private static void ApplyNeedSettings(DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            float deltaHours, FixedString64Bytes activityId, DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates, bool hasActivityRates,
            EM_Component_NeedSignalSettings signalSettings, bool hasSignalSettings,
            DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides, bool hasOverrides,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                EM_BufferElement_NeedSetting setting = settings[i];

                if (setting.NeedId.Length == 0)
                    continue;

                float minValue = math.min(setting.MinValue, setting.MaxValue);
                float maxValue = math.max(setting.MinValue, setting.MaxValue);
                float range = maxValue - minValue;

                int needIndex = FindNeedIndex(needs, setting.NeedId);
                float currentValue = minValue;

                if (needIndex >= 0)
                    currentValue = needs[needIndex].Value;

                float normalized = 0f;

                if (range > 0f)
                    normalized = math.saturate((currentValue - minValue) / range);

                FixedList128Bytes<float> rateSamples = setting.RatePerHourSamples;

                if (activityId.Length > 0 && hasActivityRates)
                {
                    FixedList128Bytes<float> activitySamples;
                    bool hasActivitySample = TryGetActivityRateSamples(setting.NeedId, activityId, activityRates, out activitySamples);

                    if (hasActivitySample)
                        rateSamples = activitySamples;
                }

                float ratePerHour = SampleRatePerHour(in rateSamples, normalized);
                float delta = ratePerHour * deltaHours;
                float updatedValue = math.clamp(currentValue + delta, minValue, maxValue);

                if (needIndex < 0)
                {
                    EM_BufferElement_Need newNeed = new EM_BufferElement_Need
                    {
                        NeedId = setting.NeedId,
                        Value = updatedValue
                    };

                    needs.Add(newNeed);
                }
                else
                {
                    EM_BufferElement_Need need = needs[needIndex];
                    need.Value = updatedValue;
                    needs[needIndex] = need;
                }

                float urgency = range > 0f ? math.saturate((updatedValue - minValue) / range) : 0f;
                FixedString64Bytes defaultValueSignalId = hasSignalSettings ? signalSettings.NeedValueSignalId : default;
                FixedString64Bytes defaultUrgencySignalId = hasSignalSettings ? signalSettings.NeedUrgencySignalId : default;
                FixedString64Bytes valueSignalId;
                FixedString64Bytes urgencySignalId;

                ResolveNeedSignalIds(setting.NeedId, defaultValueSignalId, defaultUrgencySignalId, overrides, hasOverrides,
                    out valueSignalId, out urgencySignalId);

                EmitNeedSignal(valueSignalId, updatedValue, setting.NeedId,
                    signals, subject, societyRoot, timeSeconds, hasDebugBuffer, debugBuffer, maxEntries);

                EmitNeedSignal(urgencySignalId, urgency, setting.NeedId,
                    signals, subject, societyRoot, timeSeconds, hasDebugBuffer, debugBuffer, maxEntries);
            }
        }

        // Resolve per-need signal overrides with fallback to defaults.
        private static void ResolveNeedSignalIds(FixedString64Bytes needId, FixedString64Bytes defaultValueSignalId,
            FixedString64Bytes defaultUrgencySignalId, DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides, bool hasOverrides,
            out FixedString64Bytes valueSignalId, out FixedString64Bytes urgencySignalId)
        {
            valueSignalId = defaultValueSignalId;
            urgencySignalId = defaultUrgencySignalId;

            if (!hasOverrides || needId.Length == 0)
                return;

            for (int i = 0; i < overrides.Length; i++)
            {
                if (!overrides[i].NeedId.Equals(needId))
                    continue;

                if (overrides[i].ValueSignalId.Length > 0)
                    valueSignalId = overrides[i].ValueSignalId;

                if (overrides[i].UrgencySignalId.Length > 0)
                    urgencySignalId = overrides[i].UrgencySignalId;

                return;
            }
        }

        /// <summary>
        /// Emits a signal event with the specified identifier, value, and context to the provided signal buffer.
        /// </summary>
        /// <remarks>If <paramref name="signalId"/> is empty, no signal or debug event is emitted. When
        /// <paramref name="hasDebugBuffer"/> is <see langword="true"/>, a debug event corresponding to the signal is
        /// also appended to <paramref name="debugBuffer"/>, subject to the specified <paramref
        /// name="maxEntries"/>.</remarks>
        /// <param name="signalId">The unique identifier for the signal to emit. Must not be empty.</param>
        /// <param name="value">The value associated with the signal event.</param>
        /// <param name="contextId">The identifier representing the context in which the signal is emitted.</param>
        /// <param name="signals">The dynamic buffer to which the signal event will be added.</param>
        /// <param name="subject">The entity that is the subject of the signal event.</param>
        /// <param name="societyRoot">The root entity representing the society context for the signal event.</param>
        /// <param name="hasDebugBuffer"><see langword="true"/> to emit a corresponding debug event to the debug buffer; otherwise, <see
        /// langword="false"/>.</param>
        /// <param name="debugBuffer">The dynamic buffer to which debug events are appended, if <paramref name="hasDebugBuffer"/> is <see
        /// langword="true"/>.</param>
        /// <param name="maxEntries">The maximum number of entries allowed in the debug buffer.</param>
        private static void EmitNeedSignal(FixedString64Bytes signalId, float value, FixedString64Bytes contextId,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            if (signalId.Length == 0)
                return;

            EM_BufferElement_SignalEvent signalEvent = new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = value,
                Subject = subject,
                Target = Entity.Null,
                SocietyRoot = societyRoot,
                ContextId = contextId,
                Time = timeSeconds
            };

            signals.Add(signalEvent);

            if (!hasDebugBuffer)
                return;

            EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildSignalEvent(signalId, value, contextId, subject, Entity.Null, societyRoot);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
        }

        // Need rate curve sampling for decay.
        private static float SampleRatePerHour(in FixedList128Bytes<float> samples, float normalized)
        {
            int count = samples.Length;

            if (count <= 0)
                return 0f;

            if (count == 1)
                return samples[0];

            float t = normalized;
            float scaled = t * (count - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, count - 1);
            float lerp = scaled - index;

            return math.lerp(samples[index], samples[nextIndex], lerp);
        }

        private static int FindNeedIndex(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId)
        {
            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }

        private struct NeedTickData
        {
            public float DeltaHours;
            public double TimeSeconds;
        }
        #endregion
    }
}
