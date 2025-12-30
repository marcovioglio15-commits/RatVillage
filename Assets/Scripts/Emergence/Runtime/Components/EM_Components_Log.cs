using Unity.Collections;
using Unity.Entities;

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
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public FixedString64Bytes WindowId;
        public FixedString64Bytes Reason;
        public float Value;
        #endregion
    }
}
