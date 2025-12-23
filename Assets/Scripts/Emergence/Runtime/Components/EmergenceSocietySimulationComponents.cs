using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Tracks the society clock used for daily scheduling.
    /// </summary>
    public struct EmergenceSocietyClock : IComponentData
    {
        #region Data
        public float DayLengthSeconds;
        public float TimeOfDay;
        #endregion
    }

    /// <summary>
    /// Defines schedule windows and emitted signals.
    /// </summary>
    public struct EmergenceSocietySchedule : IComponentData
    {
        #region Data
        public float SleepStartHour;
        public float SleepEndHour;
        public float WorkStartHour;
        public float WorkEndHour;
        public float TickIntervalHours;
        public FixedString64Bytes SleepSignalId;
        public FixedString64Bytes WorkSignalId;
        public FixedString64Bytes LeisureSignalId;
        public FixedString64Bytes SleepTickSignalId;
        public FixedString64Bytes WorkTickSignalId;
        public FixedString64Bytes LeisureTickSignalId;
        public BlobAssetReference<EmergenceScheduleCurveBlob> Curve;
        #endregion
    }

    /// <summary>
    /// Stores pre-sampled schedule curves for window scaling.
    /// </summary>
    public struct EmergenceScheduleCurveBlob
    {
        #region Data
        public int SampleCount;
        public BlobArray<float> SleepCurve;
        public BlobArray<float> WorkCurve;
        public BlobArray<float> LeisureCurve;
        #endregion
    }

    /// <summary>
    /// Stores schedule state for signal emission.
    /// </summary>
    public struct EmergenceSocietyScheduleState : IComponentData
    {
        #region Data
        public int CurrentWindow;
        public float TickAccumulatorHours;
        #endregion
    }

    /// <summary>
    /// Controls how often needs are updated.
    /// </summary>
    public struct EmergenceNeedTickSettings : IComponentData
    {
        #region Data
        public float TickRate;
        #endregion
    }

    /// <summary>
    /// Tracks the next need update time.
    /// </summary>
    public struct EmergenceNeedTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    /// <summary>
    /// Controls trade sampling and social influence.
    /// </summary>
    public struct EmergenceTradeSettings : IComponentData
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

    /// <summary>
    /// Tracks the next trade evaluation time.
    /// </summary>
    public struct EmergenceTradeTickState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    /// <summary>
    /// Controls how society resources are distributed to members.
    /// </summary>
    public struct EmergenceSocietyResourceDistributionSettings : IComponentData
    {
        #region Data
        public float DistributionTickRate;
        public int MaxTransfersPerMember;
        public float DefaultTransferAmount;
        public float DefaultNeedSatisfaction;
        #endregion
    }

    /// <summary>
    /// Tracks the next distribution tick time.
    /// </summary>
    public struct EmergenceSocietyResourceDistributionState : IComponentData
    {
        #region Data
        public double NextTick;
        #endregion
    }

    /// <summary>
    /// Stores schedule signals for broadcast to society members.
    /// </summary>
    public struct EmergenceScheduleSignal : IBufferElementData
    {
        #region Data
        public FixedString64Bytes SignalId;
        public float Value;
        #endregion
    }
}
