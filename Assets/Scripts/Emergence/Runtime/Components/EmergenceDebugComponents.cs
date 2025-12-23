using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Categories of debug events emitted by Emergence systems.
    /// </summary>
    public enum EmergenceDebugEventType
    {
        ScheduleWindow = 0,
        ScheduleTick = 1,
        TradeAttempt = 2,
        TradeSuccess = 3,
        TradeFail = 4,
        DistributionTransfer = 5
    }

    /// <summary>
    /// Stores debug event configuration for the runtime log.
    /// </summary>
    public struct EmergenceDebugLog : IComponentData
    {
        #region Data
        public int MaxEntries;
        #endregion
    }

    /// <summary>
    /// Represents a debug event emitted by Emergence systems.
    /// </summary>
    public struct EmergenceDebugEvent : IBufferElementData
    {
        #region Data
        public EmergenceDebugEventType Type;
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

    /// <summary>
    /// Stores a human-readable name for debug output.
    /// </summary>
    public struct EmergenceDebugName : IComponentData
    {
        #region Data
        public FixedString64Bytes Value;
        #endregion
    }
}
