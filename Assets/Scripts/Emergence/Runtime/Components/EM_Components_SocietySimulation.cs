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
        public double SimulatedTimeSeconds;
        public float BaseSimulationSpeed;
        public float SimulationSpeedMultiplier;
        #endregion
    }

    public struct EM_Component_NeedTickSettings : IComponentData
    {
        #region Data
        public float TickIntervalHours;
        #endregion
    }

    public struct EM_Component_NeedSignalSettings : IComponentData
    {
        #region Data
        public FixedString64Bytes NeedValueSignalId;
        public FixedString64Bytes NeedUrgencySignalId;
        #endregion
    }

    public struct EM_Component_HealthSignalSettings : IComponentData
    {
        #region Data
        public FixedString64Bytes HealthValueSignalId;
        public FixedString64Bytes HealthDamageSignalId;
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
        public float TradeTickIntervalHours;
        public float BaseAcceptance;
        public float AffinityWeight;
        public FixedString64Bytes TradeSuccessSignalId;
        public FixedString64Bytes TradeFailSignalId;
        public float MinIntentUrgency;
        public float MinIntentUrgencyToKeep;
        public float IntentBackoffHours;
        public float IntentBackoffMaxHours;
        public float IntentBackoffJitterHours;
        public int IntentMaxAttempts;
        public int MaxProviderAttemptsPerTick;
        public byte ConsumeResourceOnResolve;
        public byte ConsumeInventoryFirst;
        public byte ClampTransferToNeed;
        public byte LockProviderPerTick;
        #endregion
    }

    public struct EM_Component_ScheduleOverrideSettings : IComponentData
    {
        #region Data
        public byte BlockOverrideWhileOverridden;
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
