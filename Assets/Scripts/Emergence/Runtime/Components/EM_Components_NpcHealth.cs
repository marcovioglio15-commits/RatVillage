using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_NpcHealth : IComponentData
    {
        #region Data
        public float Current;
        public float Max;
        #endregion
    }

    public struct EM_Component_NpcHealthTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }
    #endregion

    #region Buffers
    public struct EM_BufferElement_NeedDamageSetting : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public float UrgencyThreshold;
        public float DamagePerHour;
        #endregion
    }
    #endregion
}
