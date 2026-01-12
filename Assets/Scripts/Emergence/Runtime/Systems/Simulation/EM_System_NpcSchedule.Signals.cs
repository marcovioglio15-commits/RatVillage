using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcSchedule
    {
        #region SignalTicking
        private static void ProcessTickSignals(float deltaHours, float progress, ref EM_Blob_NpcScheduleEntry entry,
            DynamicBuffer<EM_BufferElement_NpcScheduleSignalState> signalStateBuffer, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
            Entity entity, Entity societyRoot, double timeSeconds, bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer,
            int maxEntries, ref EM_Component_Log debugLog, float timeOfDay)
        {
            if (signalStateBuffer.Length == 0)
                return;

            ref BlobArray<EM_Blob_NpcScheduleSignal> tickSignals = ref entry.Signals;

            for (int stateIndex = 0; stateIndex < signalStateBuffer.Length; stateIndex++)
            {
                EM_BufferElement_NpcScheduleSignalState signalState = signalStateBuffer[stateIndex];

                if (signalState.SignalIndex < 0 || signalState.SignalIndex >= tickSignals.Length)
                    continue;

                ref EM_Blob_NpcScheduleSignal tickSignal = ref tickSignals[signalState.SignalIndex];
                FixedString64Bytes tickSignalId = tickSignal.TickSignalId;

                if (tickSignalId.Length == 0 || tickSignal.TickIntervalHours <= 0f)
                    continue;

                signalState.TickAccumulatorHours += deltaHours;

                if (signalState.TickAccumulatorHours < tickSignal.TickIntervalHours)
                {
                    signalStateBuffer[stateIndex] = signalState;
                    continue;
                }

                signalState.TickAccumulatorHours -= tickSignal.TickIntervalHours;
                signalStateBuffer[stateIndex] = signalState;

                float curveValue = SampleCurve(ref tickSignal.CurveSamples, progress);

                EmitSignal(tickSignalId, entry.ActivityId, signals, entity, societyRoot, timeSeconds, curveValue, hasDebugBuffer, debugBuffer,
                    maxEntries, ref debugLog);

                if (hasDebugBuffer)
                {
                    EM_Component_Event debugEvent = ScheduleLogEvent(EM_DebugEventType.ScheduleTick, timeOfDay,
                        societyRoot, entity, entry.ActivityId, curveValue);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
                }
            }
        }
        #endregion
    }
}
