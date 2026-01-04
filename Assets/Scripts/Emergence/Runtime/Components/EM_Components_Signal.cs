using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_BufferElement_SignalEvent : IBufferElementData
    {
        #region Data
        public FixedString64Bytes SignalId;
        public float Value;
        public Entity Subject;
        public Entity Target;
        public Entity SocietyRoot;
        public FixedString64Bytes ContextId;
        public double Time;
        #endregion
    }

    public struct EM_Component_SignalEmitter : IComponentData
    {
    }
}
