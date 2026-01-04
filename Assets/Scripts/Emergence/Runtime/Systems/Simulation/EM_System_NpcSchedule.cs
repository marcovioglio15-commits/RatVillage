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
        #endregion

        #region Unity Lifecycle
        // Allocate lookups required for schedule evaluation.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcSchedule>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
        }

        // Evaluate per-NPC schedules and emit activity signals.
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            if (deltaTime <= 0f)
                return;

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                EM_Component_Log debugLog = SystemAPI.GetSingleton<EM_Component_Log>();
                maxEntries = debugLog.MaxEntries;
            }

            clockLookup.Update(ref state);

            // Schedule evaluation and signal emission.
            foreach ((RefRO<EM_Component_NpcSchedule> schedule, RefRW<EM_Component_NpcScheduleState> scheduleState,
                RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
                EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcSchedule>, RefRW<EM_Component_NpcScheduleState>, RefRW<EM_Component_NpcScheduleOverride>,
                    DynamicBuffer<EM_BufferElement_SignalEvent>, EM_Component_SocietyMember>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                if (!schedule.ValueRO.Schedule.IsCreated)
                    continue;

                Entity societyRoot = member.SocietyRoot;

                if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                    continue;

                EM_Component_SocietyClock clock = clockLookup[societyRoot];
                float dayLength = math.max(clock.DayLengthSeconds, 0.01f);
                float deltaHours = (deltaTime / dayLength) * 24f;
                float timeOfDay = clock.TimeOfDay;

                UpdateOverride(scheduleOverride, deltaHours);
                bool overrideActive = scheduleOverride.ValueRO.RemainingHours > 0f && scheduleOverride.ValueRO.ActivityId.Length > 0;

                ref BlobArray<EM_Blob_NpcScheduleEntry> entries = ref schedule.ValueRO.Schedule.Value.Entries;

                int entryIndex = -1;
                FixedString64Bytes activityId = default;
                FixedString64Bytes startSignalId = default;
                FixedString64Bytes tickSignalId = default;
                float tickIntervalHours = 0f;
                float startHour = 0f;
                float endHour = 0f;
                bool hasEntryData = false;

                if (overrideActive)
                {
                    ResolveOverrideEntry(scheduleOverride, ref entries, out entryIndex);
                    activityId = scheduleOverride.ValueRO.ActivityId;

                    if (entryIndex >= 0 && entryIndex < entries.Length)
                    {
                        ref EM_Blob_NpcScheduleEntry entryData = ref entries[entryIndex];
                        startSignalId = entryData.StartSignalId;
                        tickSignalId = entryData.TickSignalId;
                        tickIntervalHours = entryData.TickIntervalHours;
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
                        startSignalId = entryData.StartSignalId;
                        tickSignalId = entryData.TickSignalId;
                        tickIntervalHours = entryData.TickIntervalHours;
                        startHour = entryData.StartHour;
                        endHour = entryData.EndHour;
                        hasEntryData = true;
                    }
                }

                FixedString64Bytes previousActivityId = scheduleState.ValueRO.CurrentActivityId;
                byte overrideFlag = (byte)(overrideActive ? 1 : 0);
                bool activityChanged = scheduleState.ValueRO.CurrentEntryIndex != entryIndex ||
                    scheduleState.ValueRO.IsOverride != overrideFlag ||
                    !scheduleState.ValueRO.CurrentActivityId.Equals(activityId);

                if (activityChanged)
                {
                    if (hasDebugBuffer && previousActivityId.Length > 0 && !previousActivityId.Equals(activityId))
                    {
                        EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleEnd, timeOfDay,
                            societyRoot, entity, previousActivityId, 0f);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
                    }

                    scheduleState.ValueRW.CurrentEntryIndex = entryIndex;
                    scheduleState.ValueRW.CurrentActivityId = activityId;
                    scheduleState.ValueRW.IsOverride = overrideFlag;
                    scheduleState.ValueRW.TickAccumulatorHours = 0f;

                    if (activityId.Length > 0 && startSignalId.Length > 0)
                        EmitSignal(startSignalId, activityId, signals, entity, societyRoot, 1f, hasDebugBuffer, debugBuffer, maxEntries);

                    if (hasDebugBuffer && activityId.Length > 0)
                    {
                        EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleWindow, timeOfDay,
                            societyRoot, entity, activityId, 1f);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
                    }
                }

                if (!hasEntryData || activityId.Length == 0)
                    continue;

                if (tickIntervalHours <= 0f || tickSignalId.Length == 0)
                    continue;

                scheduleState.ValueRW.TickAccumulatorHours += deltaHours;

                if (scheduleState.ValueRO.TickAccumulatorHours < tickIntervalHours)
                    continue;

                scheduleState.ValueRW.TickAccumulatorHours -= tickIntervalHours;

                float progress = overrideActive
                    ? GetOverrideProgress(scheduleOverride.ValueRO)
                    : GetWindowProgress(timeOfDay, startHour, endHour);
                ref EM_Blob_NpcScheduleEntry tickEntry = ref entries[entryIndex];
                float curveValue = SampleCurve(ref tickEntry.CurveSamples, progress);

                EmitSignal(tickSignalId, activityId, signals, entity, societyRoot, curveValue, hasDebugBuffer, debugBuffer, maxEntries);

                if (hasDebugBuffer)
                {
                    EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleTick, timeOfDay,
                        societyRoot, entity, activityId, curveValue);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
                }
            }
        }
        #endregion
    }
}
