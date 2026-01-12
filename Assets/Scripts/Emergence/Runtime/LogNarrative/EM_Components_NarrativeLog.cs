using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_Component_NarrativeLog : IComponentData
    {
        #region Data
        public int MaxSignalEntries;
        public int MaxLogEntries;
        public ulong NextSignalSequence;
        public ulong NextLogSequence;
        #endregion
    }

    public struct EM_BufferElement_NarrativeSignal : IBufferElementData
    {
        #region Data
        public EM_NarrativeEventType EventType;
        public double Time;
        public ulong Sequence;
        public Entity Society;
        public Entity Subject;
        public Entity Target;
        public FixedString64Bytes SignalId;
        public FixedString64Bytes IntentId;
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public FixedString64Bytes ActivityId;
        public FixedString64Bytes ContextId;
        public FixedString64Bytes ReasonId;
        public EmergenceEffectType EffectType;
        public float Value;
        public float Delta;
        public float Before;
        public float After;
        public byte Flags;
        #endregion
    }

    public struct EM_BufferElement_NarrativeLogEntry : IBufferElementData
    {
        #region Data
        public EM_NarrativeEventType EventType;
        public EM_NarrativeSeverity Severity;
        public EM_NarrativeVisibility Visibility;
        public EM_NarrativeVerbosity Verbosity;
        public EM_NarrativeTagMask Tags;
        public double Time;
        public ulong Sequence;
        public Entity Subject;
        public Entity Target;
        public FixedString128Bytes Title;
        public FixedString512Bytes Text;
        #endregion
    }
}
