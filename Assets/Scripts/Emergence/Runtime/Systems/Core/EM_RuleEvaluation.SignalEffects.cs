using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region EmitSignal
        // Emit a signal event on the subject buffer.
        private static bool ApplyEmitSignal(Entity subject, Entity signalTarget, FixedString64Bytes signalId, FixedString64Bytes contextOverride,
            FixedString64Bytes contextId, float value, Entity societyRoot, ref BufferLookup<EM_BufferElement_SignalEvent> signalLookup,
            out FixedString64Bytes emittedSignalId, out FixedString64Bytes emittedContextId)
        {
            emittedSignalId = signalId;
            emittedContextId = contextOverride.Length > 0 ? contextOverride : contextId;

            if (signalId.Length == 0)
                return false;

            if (subject == Entity.Null)
                return false;

            if (!signalLookup.HasBuffer(subject))
                return false;

            DynamicBuffer<EM_BufferElement_SignalEvent> signals = signalLookup[subject];

            signals.Add(new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = value,
                Subject = subject,
                Target = signalTarget,
                SocietyRoot = societyRoot,
                ContextId = emittedContextId,
                Time = 0d
            });

            return true;
        }
        #endregion
    }
}
