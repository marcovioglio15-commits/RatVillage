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
            double time = SystemAPI.Time.ElapsedTime;
            NativeParallelHashMap<Entity, float> deltaHoursMap = BuildDeltaHoursMap(ref state, time);

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
                float deltaHours;
                bool found = deltaHoursMap.TryGetValue(member.SocietyRoot, out deltaHours);

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

                ApplyNeedSettings(needs, settings, deltaHours, activityId, activityRates, hasActivityRates,
                    signalSettings, hasSignalSettings, overrides, hasOverrides,
                    signals, entity, member.SocietyRoot, hasDebugBuffer, debugBuffer, maxEntries);
            }

            deltaHoursMap.Dispose();
        }
        #endregion

        #region Helpers
        // Compute per-society delta hours based on tick settings.
        private NativeParallelHashMap<Entity, float> BuildDeltaHoursMap(ref SystemState state, double time)
        {
            NativeParallelHashMap<Entity, float> deltaHoursMap = new NativeParallelHashMap<Entity, float>(8, Allocator.Temp);

            foreach ((RefRW<EM_Component_NeedTickState> tickState, RefRO<EM_Component_NeedTickSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_NeedTickState>, RefRO<EM_Component_NeedTickSettings>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TickRate);

                if (time < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = time + intervalSeconds;
                deltaHoursMap.TryAdd(entity, intervalSeconds / 3600f);
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
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot,
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
                    signals, subject, societyRoot, hasDebugBuffer, debugBuffer, maxEntries);

                EmitNeedSignal(urgencySignalId, urgency, setting.NeedId,
                    signals, subject, societyRoot, hasDebugBuffer, debugBuffer, maxEntries);
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

        private static void EmitNeedSignal(FixedString64Bytes signalId, float value, FixedString64Bytes contextId,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot,
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
                Time = 0d
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
        #endregion
    }
}
