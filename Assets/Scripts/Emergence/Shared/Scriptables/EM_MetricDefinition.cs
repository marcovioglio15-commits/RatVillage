using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Metric Definition", fileName = "EM_MetricDefinition")]
    public sealed class EM_MetricDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Unique key for this metric. Used in profiles, dashboards, and sampling.")]
        [Header("Identity")]
        [SerializeField] private string metricId = "Metric.Id";
        #endregion

        #region Behavior
        [Tooltip("Signal sampled by this metric. Metrics map one signal to a sampling policy.")]
        [Header("Behavior")]
        [SerializeField] private EM_SignalDefinition signal;

        [Tooltip("Sampling mode: aggregate events over time or evaluate each event as it arrives.")]
        [SerializeField] private EmergenceMetricSamplingMode samplingMode = EmergenceMetricSamplingMode.Aggregate;

        [Tooltip("Aggregation applied across samples since the last metric tick.")]
        [SerializeField] private EmergenceMetricAggregation aggregationKind = EmergenceMetricAggregation.Average;
        
        [Tooltip("Seconds between samples for this metric.")]
        [SerializeField] private float sampleInterval = 1f;

        [Tooltip("Target scope for samples: society root or individual members.")]
        [SerializeField] private EmergenceMetricScope scope = EmergenceMetricScope.Society;


        [Tooltip("Normalization preset applied to the aggregated value to keep the probability input in 0-1 space.")]
        [SerializeField] private EmergenceMetricNormalization normalization = EmergenceMetricNormalization.Clamp01;
        #endregion
        #endregion

        #region Public Properties
        public string MetricId
        {
            get
            {
                return metricId;
            }
        }

        public float SampleInterval
        {
            get
            {
                return sampleInterval;
            }
        }

        public EmergenceMetricSamplingMode SamplingMode
        {
            get
            {
                return samplingMode;
            }
        }

        public EM_SignalDefinition Signal
        {
            get
            {
                return signal;
            }
        }

        public EmergenceMetricScope Scope
        {
            get
            {
                return scope;
            }
        }

        public EmergenceMetricAggregation Aggregation
        {
            get
            {
                return aggregationKind;
            }
        }

        public EmergenceMetricNormalization Normalization
        {
            get
            {
                return normalization;
            }
        }
        #endregion
    }
}
