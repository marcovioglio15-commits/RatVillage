using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Metric Definition", fileName = "EM_MetricDefinition")]
    public sealed class EM_MetricDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Id definition that supplies the unique key for this metric.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.Metric)]
        [SerializeField] private EM_IdDefinition metricIdDefinition;

        [Tooltip("Legacy metric id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string metricId = "Metric.Id";
        #endregion

        #region Behavior
        [Tooltip("Signal sampled by this metric. Metrics map one signal to a sampling policy.")]
        [Header("Behavior")]
        [SerializeField] private EM_SignalDefinition signal;

        [Tooltip("Sampling mode: aggregate events over time or evaluate each event as it arrives.")]
        [SerializeField] private EmergenceMetricSamplingMode samplingMode = EmergenceMetricSamplingMode.Aggregate;

        [Tooltip("Aggregation applied across samples since the last metric tick.")]
        [SerializeField] private EmergenceMetricAggregation aggregationKind = EmergenceMetricAggregation.Average;
        
        [Tooltip("Hours between samples for this metric (simulated time).")]
        [SerializeField] private float sampleIntervalHours = 0.5f;

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
                return EM_IdUtility.ResolveId(metricIdDefinition, metricId);
            }
        }

        public EM_IdDefinition MetricIdDefinition
        {
            get
            {
                return metricIdDefinition;
            }
        }

        public float SampleIntervalSeconds
        {
            get
            {
                return Mathf.Max(0f, sampleIntervalHours) * 3600f;
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
