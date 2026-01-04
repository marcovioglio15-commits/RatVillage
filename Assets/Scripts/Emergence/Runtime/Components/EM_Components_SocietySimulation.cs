using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_SocietyClock : IComponentData
    {
        #region Data
        public float DayLengthSeconds;
        public float TimeOfDay;
        #endregion
    }

    public struct EM_Component_NeedTickSettings : IComponentData
    {
        #region Data
        public float TickRate;
        #endregion
    }

    public struct EM_Component_NeedSignalSettings : IComponentData
    {
        #region Data
        public FixedString64Bytes NeedValueSignalId;
        public FixedString64Bytes NeedUrgencySignalId;
        #endregion
    }

    public struct EM_BufferElement_NeedSignalOverride : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ValueSignalId;
        public FixedString64Bytes UrgencySignalId;
        #endregion
    }

    public struct EM_Component_NeedTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    public struct EM_Component_TradeSettings : IComponentData
    {
        #region Data
        public float TradeTickRate;
        public float BaseAcceptance;
        public float AffinityWeight;
        public FixedString64Bytes TradeSuccessSignalId;
        public FixedString64Bytes TradeFailSignalId;
        #endregion
    }

    public struct EM_Component_TradeTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    #endregion
}
