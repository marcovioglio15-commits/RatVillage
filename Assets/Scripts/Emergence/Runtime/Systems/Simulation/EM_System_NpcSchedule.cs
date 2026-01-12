using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_SocietyClock))]
    [UpdateBefore(typeof(EM_System_MetricCollect))]
    public partial struct EM_System_NpcSchedule : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private ComponentLookup<EM_Component_RandomSeed> randomLookup;
        private BufferLookup<EM_BufferElement_SignalEvent> signalLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        #endregion

        #region Unity Lifecycle
        // Allocate lookups required for schedule evaluation.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcSchedule>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            signalLookup = state.GetBufferLookup<EM_BufferElement_SignalEvent>(false);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
        }

        // Evaluate per-NPC schedules and emit activity signals.
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime <= 0f)
                return;

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

            clockLookup.Update(ref state);
            randomLookup.Update(ref state);
            signalLookup.Update(ref state);
            memberLookup.Update(ref state);

            // Schedule evaluation and signal emission.
            foreach ((RefRO<EM_Component_NpcSchedule> schedule, RefRW<EM_Component_NpcScheduleState> scheduleState,
                RefRW<EM_Component_NpcScheduleTarget> scheduleTarget, RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
                RefRW<EM_Component_NpcScheduleDuration> scheduleDuration, RefRW<EM_Component_NpcLocationState> locationState,
                DynamicBuffer<EM_BufferElement_NpcScheduleSignalState> signalStates, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcSchedule>, RefRW<EM_Component_NpcScheduleState>, RefRW<EM_Component_NpcScheduleTarget>,
                    RefRW<EM_Component_NpcScheduleOverride>, RefRW<EM_Component_NpcScheduleDuration>, RefRW<EM_Component_NpcLocationState>,
                    DynamicBuffer<EM_BufferElement_NpcScheduleSignalState>>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                if (!schedule.ValueRO.Schedule.IsCreated)
                    continue;

                if (!memberLookup.HasComponent(entity))
                    continue;

                EM_Component_SocietyMember member = memberLookup[entity];
                Entity societyRoot = member.SocietyRoot;

                if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                    continue;

                if (!signalLookup.HasBuffer(entity))
                    continue;

                DynamicBuffer<EM_BufferElement_SignalEvent> signals = signalLookup[entity];

                EM_Component_SocietyClock clock = clockLookup[societyRoot];
                float dayLength = math.max(clock.DayLengthSeconds, 0.01f);
                float speed = math.max(0f, clock.BaseSimulationSpeed * clock.SimulationSpeedMultiplier);
                double simulatedDeltaSeconds = deltaTime * 86400d / dayLength * speed;
                float deltaHours = (float)(simulatedDeltaSeconds / 3600d);
                float timeOfDay = clock.TimeOfDay;
                double timeSeconds = clock.SimulatedTimeSeconds;

                UpdateOverride(scheduleOverride, deltaHours);
                bool overrideActive = scheduleOverride.ValueRO.RemainingHours > 0f && scheduleOverride.ValueRO.ActivityId.Length > 0;

                UpdateDuration(scheduleDuration, overrideActive ? 0f : deltaHours);
                bool durationActive = scheduleDuration.ValueRO.RemainingHours > 0f && scheduleDuration.ValueRO.ActivityId.Length > 0;

                ref BlobArray<EM_Blob_NpcScheduleEntry> entries = ref schedule.ValueRO.Schedule.Value.Entries;

                int entryIndex = -1;
                FixedString64Bytes activityId = default;
                float startHour = 0f;
                float endHour = 0f;
                bool hasEntryData = false;
                bool hasStartSignals = false;
                bool hasTickSignals = false;
                DynamicBuffer<EM_BufferElement_NpcScheduleSignalState> signalStateBuffer = signalStates;

                if (overrideActive)
                {
                    ResolveOverrideEntry(scheduleOverride, ref entries, out entryIndex);
                    activityId = scheduleOverride.ValueRO.ActivityId;

                    if (entryIndex >= 0 && entryIndex < entries.Length)
                    {
                        ref EM_Blob_NpcScheduleEntry entryData = ref entries[entryIndex];
                        startHour = entryData.StartHour;
                        endHour = entryData.EndHour;
                        hasEntryData = true;
                    }
                }
                else if (durationActive)
                {
                    ResolveDurationEntry(scheduleDuration, ref entries, out entryIndex);
                    activityId = scheduleDuration.ValueRO.ActivityId;

                    if (entryIndex >= 0 && entryIndex < entries.Length)
                    {
                        ref EM_Blob_NpcScheduleEntry entryData = ref entries[entryIndex];
                        startHour = entryData.StartHour;
                        endHour = entryData.EndHour;
                        hasEntryData = true;
                    }
                }
                else
                {
                    entryIndex = FindEntryForTime(timeOfDay, ref entries);

                    if (entryIndex >= 0 && entryIndex < entries.Length)
                    {
                        ref EM_Blob_NpcScheduleEntry entryData = ref entries[entryIndex];
                        activityId = entryData.ActivityId;
                        startHour = entryData.StartHour;
                        endHour = entryData.EndHour;
                        hasEntryData = true;

                        if (entryData.UseDuration != 0)
                        {
                            EM_Component_RandomSeed seed = default;
                            bool hasRandom = randomLookup.HasComponent(entity);

                            if (hasRandom)
                                seed = randomLookup[entity];

                            bool seedChanged = false;
                            float durationHours = ResolveDurationHours(ref entryData, hasRandom, ref seed, out seedChanged);

                            if (durationHours > 0f)
                            {
                                scheduleDuration.ValueRW.ActivityId = activityId;
                                scheduleDuration.ValueRW.RemainingHours = durationHours;
                                scheduleDuration.ValueRW.DurationHours = durationHours;
                                scheduleDuration.ValueRW.EntryIndex = entryIndex;
                                durationActive = true;
                            }

                            if (seedChanged)
                                randomLookup[entity] = seed;
                        }
                    }
                }

                FixedString64Bytes targetLocationId = default;
                byte targetTradeCapable = 0;

                if (hasEntryData)
                {
                    ref EM_Blob_NpcScheduleEntry entryData = ref entries[entryIndex];
                    ref BlobArray<EM_Blob_NpcScheduleSignal> signalEntries = ref entryData.Signals;
                    targetLocationId = entryData.LocationId;
                    targetTradeCapable = entryData.TradeCapable;

                    for (int signalIndex = 0; signalIndex < signalEntries.Length; signalIndex++)
                    {
                        if (!hasStartSignals && signalEntries[signalIndex].StartSignalId.Length > 0)
                            hasStartSignals = true;

                        if (!hasTickSignals && signalEntries[signalIndex].TickSignalId.Length > 0 &&
                            signalEntries[signalIndex].TickIntervalHours > 0f)
                            hasTickSignals = true;

                        if (hasStartSignals && hasTickSignals)
                            break;
                    }
                }

                byte overrideFlag = (byte)(overrideActive ? 1 : 0);
                UpdateScheduleTarget(scheduleTarget, entryIndex, activityId, targetLocationId, overrideFlag, targetTradeCapable);

                bool locationReady = IsTargetLocationReady(targetLocationId, locationState.ValueRO.CurrentLocationId);
                bool canStart = hasEntryData && activityId.Length > 0 && locationReady;
                int activeEntryIndex = canStart ? entryIndex : -1;
                FixedString64Bytes activeActivityId = canStart ? activityId : default;
                byte activeOverrideFlag = (byte)(canStart && overrideActive ? 1 : 0);

                FixedString64Bytes previousActivityId = scheduleState.ValueRO.CurrentActivityId;
                bool activityChanged = scheduleState.ValueRO.CurrentEntryIndex != activeEntryIndex ||
                    scheduleState.ValueRO.IsOverride != activeOverrideFlag ||
                    !scheduleState.ValueRO.CurrentActivityId.Equals(activeActivityId);

                if (activityChanged)
                {
                    if (hasDebugBuffer && previousActivityId.Length > 0 && !previousActivityId.Equals(activeActivityId))
                    {
                        EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleEnd, timeOfDay,
                            societyRoot, entity, previousActivityId, 0f);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
                    }

                    scheduleState.ValueRW.CurrentEntryIndex = activeEntryIndex;
                    scheduleState.ValueRW.CurrentActivityId = activeActivityId;
                    scheduleState.ValueRW.IsOverride = activeOverrideFlag;

                    signalStateBuffer.Clear();

                    if (activeEntryIndex >= 0 && activeEntryIndex < entries.Length)
                    {
                        ref EM_Blob_NpcScheduleEntry entryData = ref entries[activeEntryIndex];
                        int signalCount = entryData.Signals.Length;

                        if (signalCount > 0)
                        {
                            signalStateBuffer.ResizeUninitialized(signalCount);

                            for (int signalIndex = 0; signalIndex < signalCount; signalIndex++)
                            {
                                signalStateBuffer[signalIndex] = new EM_BufferElement_NpcScheduleSignalState
                                {
                                    SignalIndex = signalIndex,
                                    TickAccumulatorHours = 0f
                                };
                            }
                        }
                    }

                    if (activeActivityId.Length > 0 && hasStartSignals)
                    {
                        ref BlobArray<EM_Blob_NpcScheduleSignal> signalEntries = ref entries[activeEntryIndex].Signals;

                        for (int signalIndex = 0; signalIndex < signalEntries.Length; signalIndex++)
                        {
                            FixedString64Bytes startSignalId = signalEntries[signalIndex].StartSignalId;

                            if (startSignalId.Length == 0)
                                continue;

                            EmitSignal(startSignalId, activeActivityId, signals, entity, societyRoot, timeSeconds, 1f, hasDebugBuffer, debugBuffer,
                                maxEntries, ref debugLog);
                        }
                    }

                    if (hasDebugBuffer && activeActivityId.Length > 0)
                    {
                        EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleWindow, timeOfDay,
                            societyRoot, entity, activeActivityId, 1f);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
                    }

                    if (canStart && targetTradeCapable != 0 && locationState.ValueRO.CurrentLocationAnchor != Entity.Null)
                        locationState.ValueRW.LastTradeAnchor = locationState.ValueRO.CurrentLocationAnchor;
                }

                if (!canStart)
                    continue;

                if (!hasTickSignals || signalStateBuffer.Length == 0)
                    continue;

                float progress = overrideActive
                    ? GetOverrideProgress(scheduleOverride.ValueRO)
                    : durationActive
                        ? GetDurationProgress(scheduleDuration.ValueRO)
                        : GetWindowProgress(timeOfDay, startHour, endHour);
                ref EM_Blob_NpcScheduleEntry tickEntry = ref entries[activeEntryIndex];
                ProcessTickSignals(deltaHours, progress, ref tickEntry, signalStateBuffer, signals, entity, societyRoot, timeSeconds,
                    hasDebugBuffer, debugBuffer, maxEntries, ref debugLog, timeOfDay);
            }

            if (hasDebugBuffer)
                debugLogRef.ValueRW = debugLog;
        }
        #endregion
    }
}
