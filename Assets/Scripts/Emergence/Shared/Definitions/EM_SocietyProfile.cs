using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a society profile that controls emergence tuning and scalability.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Society Profile", fileName = "EM_SocietyProfile")]
    public sealed class EM_SocietyProfile : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Stable identifier for this profile.")]
        [SerializeField] private string profileId = "Society.Profile";

        [Tooltip("Designer-facing name for this profile.")]
        [SerializeField] private string displayName = "Society Profile";
        #endregion

        // Serialized stability
        #region Serialized - Stability
        [Tooltip("Controls how volatile the society is to shocks.")]
        [SerializeField] private float volatility = 0.5f;

        [Tooltip("Controls damping of rapid changes.")]
        [SerializeField] private float shockAbsorption = 0.5f;

        [Tooltip("Adds controlled randomness to outcomes.")]
        [SerializeField] private float noiseAmplitude = 0.1f;

        [Tooltip("Threshold used to detect crisis states.")]
        [SerializeField] private float crisisThreshold = 0.8f;
        #endregion

        // Serialized ticks
        #region Serialized - Tick Rates
        [Tooltip("Full simulation tick rate in Hz.")]
        [Min(0.1f)]
        [SerializeField] private float fullSimTickRate = 20f;

        [Tooltip("Simplified simulation tick rate in Hz.")]
        [Min(0.1f)]
        [SerializeField] private float simplifiedSimTickRate = 5f;

        [Tooltip("Aggregated simulation tick rate in Hz.")]
        [Min(0.1f)]
        [SerializeField] private float aggregatedSimTickRate = 1f;
        #endregion

        // Serialized limits
        #region Serialized - Limits
        [Tooltip("Maximum queued signal events before culling.")]
        [Min(32)]
        [SerializeField] private int maxSignalQueue = 2048;

        [Tooltip("Region size used for LOD and aggregation.")]
        [SerializeField] private Vector2 regionSize = new Vector2(250f, 250f);
        #endregion

        // Serialized composition
        #region Serialized - Composition
        [Tooltip("Domains active for this profile.")]
        [SerializeField] private EM_DomainDefinition[] domains = new EM_DomainDefinition[0];

        [Tooltip("Rule sets applied by this profile.")]
        [SerializeField] private EM_RuleSetDefinition[] ruleSets = new EM_RuleSetDefinition[0];

        [Tooltip("Metrics sampled by this profile.")]
        [SerializeField] private EM_MetricDefinition[] metrics = new EM_MetricDefinition[0];

        [Tooltip("Institutions active in this profile.")]
        [SerializeField] private EM_InstitutionDefinition[] institutions = new EM_InstitutionDefinition[0];
        #endregion

        // Serialized notes
        #region Serialized - Notes
        [Tooltip("Optional description for designers.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the profile identifier.
        /// </summary>
        public string ProfileId
        {
            get
            {
                return profileId;
            }
        }

        /// <summary>
        /// Gets the display name for this profile.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the volatility parameter.
        /// </summary>
        public float Volatility
        {
            get
            {
                return volatility;
            }
        }

        /// <summary>
        /// Gets the shock absorption parameter.
        /// </summary>
        public float ShockAbsorption
        {
            get
            {
                return shockAbsorption;
            }
        }

        /// <summary>
        /// Gets the noise amplitude parameter.
        /// </summary>
        public float NoiseAmplitude
        {
            get
            {
                return noiseAmplitude;
            }
        }

        /// <summary>
        /// Gets the crisis threshold parameter.
        /// </summary>
        public float CrisisThreshold
        {
            get
            {
                return crisisThreshold;
            }
        }

        /// <summary>
        /// Gets the full simulation tick rate in Hz.
        /// </summary>
        public float FullSimTickRate
        {
            get
            {
                return fullSimTickRate;
            }
        }

        /// <summary>
        /// Gets the simplified simulation tick rate in Hz.
        /// </summary>
        public float SimplifiedSimTickRate
        {
            get
            {
                return simplifiedSimTickRate;
            }
        }

        /// <summary>
        /// Gets the aggregated simulation tick rate in Hz.
        /// </summary>
        public float AggregatedSimTickRate
        {
            get
            {
                return aggregatedSimTickRate;
            }
        }

        /// <summary>
        /// Gets the maximum signal queue size.
        /// </summary>
        public int MaxSignalQueue
        {
            get
            {
                return maxSignalQueue;
            }
        }

        /// <summary>
        /// Gets the region size used for LOD.
        /// </summary>
        public Vector2 RegionSize
        {
            get
            {
                return regionSize;
            }
        }

        /// <summary>
        /// Gets the active domains.
        /// </summary>
        public EM_DomainDefinition[] Domains
        {
            get
            {
                return domains;
            }
        }

        /// <summary>
        /// Gets the active rule sets.
        /// </summary>
        public EM_RuleSetDefinition[] RuleSets
        {
            get
            {
                return ruleSets;
            }
        }

        /// <summary>
        /// Gets the metrics definitions.
        /// </summary>
        public EM_MetricDefinition[] Metrics
        {
            get
            {
                return metrics;
            }
        }

        /// <summary>
        /// Gets the institution definitions.
        /// </summary>
        public EM_InstitutionDefinition[] Institutions
        {
            get
            {
                return institutions;
            }
        }

        /// <summary>
        /// Gets the designer description for this profile.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }
        #endregion
    }
}
