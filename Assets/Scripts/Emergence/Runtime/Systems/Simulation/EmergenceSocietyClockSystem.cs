using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Advances the society clock and emits schedule signals.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EmergenceSignalCollectSystem))]
    public partial struct EmergenceSocietyClockSystem : ISystem
    {
        #region Constants
        private const int WindowSleep = 0;
        private const int WindowWork = 1;
        private const int WindowLeisure = 2;
        private static readonly FixedString64Bytes WindowLabelSleep = new FixedString64Bytes("Sleep");
        private static readonly FixedString64Bytes WindowLabelWork = new FixedString64Bytes("Work");
        private static readonly FixedString64Bytes WindowLabelLeisure = new FixedString64Bytes("Leisure");
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceSocietyClock>();
            state.RequireForUpdate<EmergenceSocietySchedule>();
        }

        /// <summary>
        /// Advances time of day and emits schedule signals.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            if (deltaTime <= 0f)
                return;

            // Resolve debug log access
            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer<EmergenceDebugEvent>(out DynamicBuffer<EmergenceDebugEvent> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                EmergenceDebugLog debugLog = SystemAPI.GetSingleton<EmergenceDebugLog>();
                maxEntries = debugLog.MaxEntries;
            }

            foreach ((RefRW<EmergenceSocietyClock> clock, RefRO<EmergenceSocietySchedule> schedule,
                RefRW<EmergenceSocietyScheduleState> scheduleState, DynamicBuffer<EmergenceSignalEvent> signals,
                DynamicBuffer<EmergenceScheduleSignal> scheduleSignals, Entity entity)
                in SystemAPI.Query<RefRW<EmergenceSocietyClock>, RefRO<EmergenceSocietySchedule>, RefRW<EmergenceSocietyScheduleState>, DynamicBuffer<EmergenceSignalEvent>, DynamicBuffer<EmergenceScheduleSignal>>()
                    .WithAll<EmergenceSocietyRoot, EmergenceSignalEmitter>().WithEntityAccess())
            {
                float dayLength = math.max(clock.ValueRO.DayLengthSeconds, 0.01f);
                float deltaHours = (deltaTime / dayLength) * 24f;
                float updatedTime = clock.ValueRO.TimeOfDay + deltaHours;
                float wrappedTime = math.fmod(updatedTime, 24f);

                if (wrappedTime < 0f)
                    wrappedTime += 24f;

                clock.ValueRW.TimeOfDay = wrappedTime;

                int window = GetWindow(wrappedTime, schedule.ValueRO);

                if (window != scheduleState.ValueRO.CurrentWindow)
                {
                    scheduleState.ValueRW.CurrentWindow = window;
                    scheduleState.ValueRW.TickAccumulatorHours = 0f;
                    EmitWindowSignal(window, schedule.ValueRO, signals, scheduleSignals, entity, 1f);

                    if (hasDebugBuffer)
                    {
                        EmergenceDebugEvent debugEvent = BuildScheduleDebugEvent(EmergenceDebugEventType.ScheduleWindow, wrappedTime, entity, window, 1f);
                        EmergenceDebugEventUtility.AppendEvent(debugBuffer, maxEntries, debugEvent);
                    }
                }

                if (schedule.ValueRO.TickIntervalHours <= 0f)
                    continue;

                scheduleState.ValueRW.TickAccumulatorHours += deltaHours;

                if (scheduleState.ValueRO.TickAccumulatorHours < schedule.ValueRO.TickIntervalHours)
                    continue;

                scheduleState.ValueRW.TickAccumulatorHours -= schedule.ValueRO.TickIntervalHours;
                float windowProgress = GetWindowProgress(wrappedTime, schedule.ValueRO, window);
                float curveValue = SampleScheduleCurve(schedule.ValueRO.Curve, window, windowProgress);
                EmitTickSignal(window, schedule.ValueRO, signals, scheduleSignals, entity, curveValue);

                if (hasDebugBuffer)
                {
                    EmergenceDebugEvent debugEvent = BuildScheduleDebugEvent(EmergenceDebugEventType.ScheduleTick, wrappedTime, entity, window, curveValue);
                    EmergenceDebugEventUtility.AppendEvent(debugBuffer, maxEntries, debugEvent);
                }
            }
        }
        #endregion

    }
}
