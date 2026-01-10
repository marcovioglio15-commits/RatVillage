using Unity.Entities;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_NpcNeedTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    public struct EM_Component_NpcNeedRateSettings : IComponentData
    {
        #region Data
        public float RateMultiplierVariance;
        #endregion
    }
    #endregion
}
