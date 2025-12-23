using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Defines need decay and trade behavior for a specific need.
    /// </summary>
    public struct EmergenceNeedRule : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public float RatePerHour;
        public float MinValue;
        public float MaxValue;
        public float StartThreshold;
        public float MaxProbability;
        public float ProbabilityExponent;
        public float CooldownSeconds;
        public float ResourceTransferAmount;
        public float NeedSatisfactionAmount;
        #endregion
    }

    /// <summary>
    /// Stores cooldown state for need resolution attempts.
    /// </summary>
    public struct EmergenceNeedResolutionState : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public double NextAttemptTime;
        #endregion
    }

    /// <summary>
    /// Stores a relationship affinity between two entities.
    /// </summary>
    public struct EmergenceRelationship : IBufferElementData
    {
        #region Data
        public Entity Other;
        public float Affinity;
        #endregion
    }

    /// <summary>
    /// Stores a deterministic random seed per entity.
    /// </summary>
    public struct EmergenceRandomSeed : IComponentData
    {
        #region Data
        public uint Value;
        #endregion
    }
}
