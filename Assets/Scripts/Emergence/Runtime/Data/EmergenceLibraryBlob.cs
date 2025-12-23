using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Blob container with compiled emergence definitions.
    /// </summary>
    public struct EmergenceLibraryBlob
    {
        #region Data
        public BlobArray<EmergenceSignalBlob> Signals;
        public BlobArray<EmergenceEffectBlob> Effects;
        public BlobArray<EmergenceRuleSetBlob> RuleSets;
        public BlobArray<EmergenceRuleBlob> Rules;
        public BlobArray<EmergenceRuleGroupBlob> RuleGroups;
        public BlobArray<EmergenceMetricBlob> Metrics;
        #endregion
    }

    /// <summary>
    /// Blob data for a signal definition.
    /// </summary>
    public struct EmergenceSignalBlob
    {
        #region Data
        public FixedString64Bytes SignalId;
        public FixedString64Bytes DomainId;
        public EmergenceLodTier MinimumLod;
        public float DefaultWeight;
        #endregion
    }

    /// <summary>
    /// Blob data for an effect definition.
    /// </summary>
    public struct EmergenceEffectBlob
    {
        #region Data
        public FixedString64Bytes EffectId;
        public EmergenceEffectType EffectType;
        public EmergenceEffectTarget Target;
        public FixedString64Bytes ParameterId;
        public float Magnitude;
        public byte UseClamp;
        public float MinValue;
        public float MaxValue;
        #endregion
    }

    /// <summary>
    /// Blob data for a rule set definition.
    /// </summary>
    public struct EmergenceRuleSetBlob
    {
        #region Data
        public FixedString64Bytes RuleSetId;
        public FixedString64Bytes DomainId;
        public byte IsEnabled;
        #endregion
    }

    /// <summary>
    /// Blob data for a rule entry.
    /// </summary>
    public struct EmergenceRuleBlob
    {
        #region Data
        public int SignalIndex;
        public int EffectIndex;
        public int RuleSetIndex;
        public int Priority;
        public float Weight;
        public float MinimumSignalValue;
        public float CooldownSeconds;
        #endregion
    }

    /// <summary>
    /// Blob data grouping rules by signal id.
    /// </summary>
    public struct EmergenceRuleGroupBlob
    {
        #region Data
        public FixedString64Bytes SignalId;
        public int StartIndex;
        public int Length;
        #endregion
    }

    /// <summary>
    /// Blob data for a metric definition.
    /// </summary>
    public struct EmergenceMetricBlob
    {
        #region Data
        public FixedString64Bytes MetricId;
        public EmergenceMetricType MetricType;
        public float SampleInterval;
        public FixedString64Bytes ParameterId;
        public float WarningThreshold;
        public float CriticalThreshold;
        public int ThresholdSignalIndex;
        #endregion
    }

    /// <summary>
    /// Blob data for a society profile.
    /// </summary>
    public struct EmergenceSocietyProfileBlob
    {
        #region Data
        public FixedString64Bytes ProfileId;
        public float Volatility;
        public float ShockAbsorption;
        public float NoiseAmplitude;
        public float CrisisThreshold;
        public float FullSimTickRate;
        public float SimplifiedSimTickRate;
        public float AggregatedSimTickRate;
        public int MaxSignalQueue;
        public float2 RegionSize;
        public BlobArray<byte> RuleSetMask;
        public BlobArray<byte> MetricMask;
        #endregion
    }
}
