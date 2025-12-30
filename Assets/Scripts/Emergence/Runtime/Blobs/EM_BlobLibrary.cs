using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_Blob_Library
    {
        #region Data
        public BlobArray<EM_Blob_Signal> Signals;
        public BlobArray<EM_Blob_Metric> Metrics;
        public BlobArray<EM_Blob_Effect> Effects;
        public BlobArray<EM_Blob_RuleSet> RuleSets;
        public BlobArray<EM_Blob_Rule> Rules;
        public BlobArray<EM_Blob_RuleGroup> RuleGroups;
        public BlobArray<EM_Blob_MetricGroup> MetricGroups;
        public BlobArray<int> MetricGroupMetricIndices;
        public BlobArray<EM_Blob_ProbabilityCurve> Curves;
        public BlobArray<EM_Blob_Domain> Domains;
        public BlobArray<int> DomainRuleSetIndices;
        #endregion
    }

    public struct EM_Blob_Signal
    {
        #region Data
        public FixedString64Bytes SignalId;
        public FixedString64Bytes DomainId;
        #endregion
    }

    public struct EM_Blob_Metric
    {
        #region Data
        public FixedString64Bytes MetricId;
        public int SignalIndex;
        public float SampleInterval;
        public EmergenceMetricScope Scope;
        public EmergenceMetricAggregation Aggregation;
        public EmergenceMetricNormalization Normalization;
        #endregion
    }

    public struct EM_Blob_Effect
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

    public struct EM_Blob_RuleSet
    {
        #region Data
        public FixedString64Bytes RuleSetId;
        #endregion
    }

    public struct EM_Blob_Rule
    {
        #region Data
        public int MetricIndex;
        public int EffectIndex;
        public int RuleSetIndex;
        public int CurveIndex;
        public float Weight;
        public float CooldownSeconds;
        #endregion
    }

    public struct EM_Blob_RuleGroup
    {
        #region Data
        public int MetricIndex;
        public int StartIndex;
        public int Length;
        #endregion
    }

    public struct EM_Blob_MetricGroup
    {
        #region Data
        public int SignalIndex;
        public int StartIndex;
        public int Length;
        #endregion
    }

    public struct EM_Blob_ProbabilityCurve
    {
        #region Data
        public BlobArray<float> Samples;
        #endregion
    }

    public struct EM_Blob_Domain
    {
        #region Data
        public FixedString64Bytes DomainId;
        public int StartIndex;
        public int Length;
        #endregion
    }


    public struct EM_Blob_SocietyProfile
    {
        #region Data
        public FixedString64Bytes ProfileId;
        public BlobArray<byte> DomainMask;
        public BlobArray<byte> RuleSetMask;
        public BlobArray<byte> MetricMask;
        #endregion
    }
}
