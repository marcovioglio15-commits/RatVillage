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
        public float AffinityChangeOnSuccess;
        public float AffinityChangeOnFail;
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

    public struct EM_Component_SocietyResourceDistributionSettings : IComponentData
    {
        #region Data
        public float DistributionTickRate;
        public int MaxTransfersPerMember;
        public float DefaultTransferAmount;
        public float DefaultNeedSatisfaction;
        #endregion
    }

    public struct EM_Component_SocietyResourceDistributionState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }
    #endregion
}
