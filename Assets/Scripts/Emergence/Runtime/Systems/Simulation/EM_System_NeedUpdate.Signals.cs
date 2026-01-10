using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_NeedUpdate : ISystem
    {
        #region Signals
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
        /// <param name="debugLog">The log component storing the rolling sequence counter.</param>
        private static void EmitNeedSignal(FixedString64Bytes signalId, float value, FixedString64Bytes contextId,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
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

            EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildSignalEvent(signalId, value, contextId, subject, Entity.Null, societyRoot,
                timeSeconds);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
        }
        #endregion
    }
}
