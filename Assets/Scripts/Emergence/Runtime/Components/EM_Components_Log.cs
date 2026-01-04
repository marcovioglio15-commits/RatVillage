using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public struct EM_Component_Log : IComponentData
    {
        #region Data
        public int MaxEntries;
        #endregion
    }

    public struct EM_Component_Event : IBufferElementData
    {
        #region Data
        public EM_DebugEventType Type;
        public double Time;
        public Entity Society;
        public Entity Subject;
        public Entity Target;
        public FixedString64Bytes SignalId;
        public FixedString64Bytes IntentId;
        public EmergenceEffectType EffectType;
        public FixedString64Bytes ParameterId;
        public FixedString64Bytes ContextId;
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public FixedString64Bytes ActivityId;
        public FixedString64Bytes Reason;
        public float Value;
        public float Delta;
        public float Before;
        public float After;
        #endregion
    }

    public struct EM_Component_LogColor : IComponentData
    {
        #region Data
        public float4 Value;
        #endregion
    }
}
