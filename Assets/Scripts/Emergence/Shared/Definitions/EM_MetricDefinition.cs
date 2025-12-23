using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a metric sampled by the emergence telemetry system.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Metric Definition", fileName = "EM_MetricDefinition")]
    public sealed class EM_MetricDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key for this metric. Used in profiles, dashboards, and sampling.")]
        [SerializeField] private string metricId = "Metric.Id";

        [Tooltip("Designer-facing label used in charts and logs.")]
        [SerializeField] private string displayName = "Metric";
        #endregion

        // Serialized behavior
        #region Serialized - Behavior
        [Tooltip("What the metric samples at runtime, such as population or average need.")]
        [SerializeField] private EmergenceMetricType metricType = EmergenceMetricType.PopulationCount;

        [Tooltip("Seconds between samples for each society.")]
        [SerializeField] private float sampleInterval = 1f;

        [Tooltip("Optional parameter id for parameterized metrics, e.g. Need.Hunger or Resource.Food.")]
        [SerializeField] private string parameterId = "";
        #endregion

        // Serialized thresholds
        #region Serialized - Thresholds
        [Tooltip("Soft threshold used for alerts or balancing review.")]
        [SerializeField] private float warningThreshold;

        [Tooltip("Hard threshold used for crisis detection or escalations.")]
        [SerializeField] private float criticalThreshold;

        [Tooltip("Optional signal emitted when thresholds are exceeded, useful for rules or alerts.")]
        [SerializeField] private EM_SignalDefinition thresholdSignal;
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the metric identifier.
        /// </summary>
        public string MetricId
        {
            get
            {
                return metricId;
            }
        }

        /// <summary>
        /// Gets the display name for this metric.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the metric type.
        /// </summary>
        public EmergenceMetricType MetricType
        {
            get
            {
                return metricType;
            }
        }

        /// <summary>
        /// Gets the sample interval in seconds.
        /// </summary>
        public float SampleInterval
        {
            get
            {
                return sampleInterval;
            }
        }

        /// <summary>
        /// Gets the parameter identifier.
        /// </summary>
        public string ParameterId
        {
            get
            {
                return parameterId;
            }
        }

        /// <summary>
        /// Gets the warning threshold.
        /// </summary>
        public float WarningThreshold
        {
            get
            {
                return warningThreshold;
            }
        }

        /// <summary>
        /// Gets the critical threshold.
        /// </summary>
        public float CriticalThreshold
        {
            get
            {
                return criticalThreshold;
            }
        }

        /// <summary>
        /// Gets the threshold signal definition.
        /// </summary>
        public EM_SignalDefinition ThresholdSignal
        {
            get
            {
                return thresholdSignal;
            }
        }
        #endregion
    }
}
