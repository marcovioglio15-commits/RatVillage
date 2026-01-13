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
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup;
        private ComponentLookup<EM_Component_NpcScheduleDuration> scheduleDurationLookup;
        private ComponentLookup<EM_Component_NpcScheduleState> scheduleStateLookup;
        #endregion

        #region Unity Lifecycle
        // Configure lookups for need updates and signal emission.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_BufferElement_NeedSetting>();
            state.RequireForUpdate<EM_Component_NpcNeedTickState>();
            signalSettingsLookup = state.GetComponentLookup<EM_Component_NeedSignalSettings>(true);
            needSignalOverrideLookup = state.GetBufferLookup<EM_BufferElement_NeedSignalOverride>(true);
            needActivityRateLookup = state.GetBufferLookup<EM_BufferElement_NeedActivityRate>(true);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleOverrideLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverride>(true);
            scheduleDurationLookup = state.GetComponentLookup<EM_Component_NpcScheduleDuration>(true);
            scheduleStateLookup = state.GetComponentLookup<EM_Component_NpcScheduleState>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<Entity, NeedTickData> tickDataMap = BuildTickConfigMap(ref state);
            if (tickDataMap.Count() == 0)
            {
                tickDataMap.Dispose();
                return;
            }

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;
            EM_Component_Log debugLog = default;
            RefRW<EM_Component_Log> debugLogRef = default;

            if (hasDebugBuffer)
            {
                debugLogRef = SystemAPI.GetSingletonRW<EM_Component_Log>();
                debugLog = debugLogRef.ValueRO;
                maxEntries = debugLog.MaxEntries;
            }
            signalSettingsLookup.Update(ref state);
            needSignalOverrideLookup.Update(ref state);
            needActivityRateLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleOverrideLookup.Update(ref state);
            scheduleDurationLookup.Update(ref state);
            scheduleStateLookup.Update(ref state);

            foreach ((DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
                DynamicBuffer<EM_BufferElement_SignalEvent> signals, RefRW<EM_Component_NpcNeedTickState> tickState,
                EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Need>, DynamicBuffer<EM_BufferElement_NeedSetting>,
                    DynamicBuffer<EM_BufferElement_SignalEvent>, RefRW<EM_Component_NpcNeedTickState>, EM_Component_SocietyMember>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                NeedTickData tickData;
                bool found = tickDataMap.TryGetValue(member.SocietyRoot, out tickData);

                if (!found)
                    continue;

                if (tickData.TimeSeconds < tickState.ValueRO.NextTick)
                    continue;

                EM_Component_NpcNeedTickState updatedTick = tickState.ValueRO;
                updatedTick.NextTick = tickData.TimeSeconds + tickData.IntervalSeconds;
                tickState.ValueRW = updatedTick;

                EM_Component_NeedSignalSettings signalSettings = default;
                bool hasSignalSettings = member.SocietyRoot != Entity.Null && signalSettingsLookup.HasComponent(member.SocietyRoot);
                bool hasOverrides = member.SocietyRoot != Entity.Null && needSignalOverrideLookup.HasBuffer(member.SocietyRoot);
                DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides = default;

                if (hasSignalSettings)
                    signalSettings = signalSettingsLookup[member.SocietyRoot];

                if (hasOverrides)
                    overrides = needSignalOverrideLookup[member.SocietyRoot];

                EM_Component_NpcScheduleState scheduleState = default;
                FixedString64Bytes activityId = default;
                bool hasScheduleState = scheduleStateLookup.HasComponent(entity);

                if (hasScheduleState)
                {
                    scheduleState = scheduleStateLookup[entity];
                    activityId = scheduleState.CurrentActivityId;
                }

                float activityNormalizedTime = 0f;

                if (hasScheduleState && activityId.Length > 0)
                {
                    EM_Component_NpcSchedule schedule = default;
                    EM_Component_NpcScheduleOverride scheduleOverride = default;
                    EM_Component_NpcScheduleDuration scheduleDuration = default;
                    bool hasSchedule = scheduleLookup.HasComponent(entity);
                    bool hasScheduleOverride = scheduleOverrideLookup.HasComponent(entity);
                    bool hasScheduleDuration = scheduleDurationLookup.HasComponent(entity);

                    if (hasSchedule)
                        schedule = scheduleLookup[entity];

                    if (hasScheduleOverride)
                        scheduleOverride = scheduleOverrideLookup[entity];

                    if (hasScheduleDuration)
                        scheduleDuration = scheduleDurationLookup[entity];

                    activityNormalizedTime = ResolveActivityNormalizedTime(activityId, scheduleState.CurrentEntryIndex, scheduleState.IsOverride,
                        tickData.TimeOfDay, schedule, hasSchedule, scheduleOverride, hasScheduleOverride, scheduleDuration, hasScheduleDuration);
                }

                DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates = default;
                bool hasActivityRates = needActivityRateLookup.HasBuffer(entity);

                if (hasActivityRates)
                    activityRates = needActivityRateLookup[entity];

                ApplyNeedSettings(needs, settings, tickData.DeltaHours, activityId, activityRates, hasActivityRates, activityNormalizedTime,
                    signalSettings, hasSignalSettings, overrides, hasOverrides,
                    signals, entity, member.SocietyRoot, tickData.TimeSeconds, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
            }

            if (hasDebugBuffer)
                debugLogRef.ValueRW = debugLog;

            tickDataMap.Dispose();
        }
        #endregion

        #region Helpers
        // Cache per-society tick configuration from the shared clock.
        private NativeParallelHashMap<Entity, NeedTickData> BuildTickConfigMap(ref SystemState state)
        {
            NativeParallelHashMap<Entity, NeedTickData> tickDataMap = new NativeParallelHashMap<Entity, NeedTickData>(8, Allocator.Temp);

            foreach ((RefRO<EM_Component_NeedTickSettings> settings, RefRO<EM_Component_SocietyClock> clock, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NeedTickSettings>, RefRO<EM_Component_SocietyClock>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TickIntervalHours);
                double timeSeconds = clock.ValueRO.SimulatedTimeSeconds;
                tickDataMap.TryAdd(entity, new NeedTickData(intervalSeconds, timeSeconds, clock.ValueRO.TimeOfDay));
            }

            return tickDataMap;
        }

        private static float GetIntervalSeconds(float intervalHours)
        {
            if (intervalHours <= 0f)
                return 1f;

            return intervalHours * 3600f;
        }

        // Apply decay curves and emit need signals.
        private static void ApplyNeedSettings(DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            float deltaHours, FixedString64Bytes activityId, DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates, bool hasActivityRates,
            float activityNormalizedTime,
            EM_Component_NeedSignalSettings signalSettings, bool hasSignalSettings,
            DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides, bool hasOverrides,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
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

                FixedList128Bytes<float> rateSamples = setting.RatePerHourSamples;

                if (activityId.Length > 0 && hasActivityRates)
                {
                    FixedList128Bytes<float> activitySamples;
                    bool hasActivitySample = TryGetActivityRateSamples(setting.NeedId, activityId, activityRates, out activitySamples);

                    if (hasActivitySample)
                        rateSamples = activitySamples;
                }

                float ratePerHour = SampleRatePerHour(in rateSamples, activityNormalizedTime);
                float rateMultiplier = setting.RateMultiplier;

                if (rateMultiplier <= 0f)
                    rateMultiplier = 1f;

                ratePerHour *= rateMultiplier;
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
                    signals, subject, societyRoot, timeSeconds, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                EmitNeedSignal(urgencySignalId, urgency, setting.NeedId,
                    signals, subject, societyRoot, timeSeconds, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
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

        private readonly struct NeedTickData
        {
            public readonly float IntervalSeconds;
            public readonly float DeltaHours;
            public readonly double TimeSeconds;
            public readonly float TimeOfDay;

            public NeedTickData(float intervalSeconds, double timeSeconds, float timeOfDay)
            {
                IntervalSeconds = intervalSeconds;
                DeltaHours = intervalSeconds / 3600f;
                TimeSeconds = timeSeconds;
                TimeOfDay = timeOfDay;
            }
        }
        #endregion
    }
}
