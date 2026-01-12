using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Narrative Log Settings", fileName = "EM_NarrativeLogSettings")]
    public sealed class EM_NarrativeLogSettings : ScriptableObject
    {
        #region Serialized
        #region Refresh
        [Tooltip("Seconds between narrative log refreshes.")]
        [Header("Refresh")]
        [SerializeField] private float refreshIntervalSeconds = 0.25f;
        #endregion

        #region Capacity
        [Tooltip("Maximum number of narrative signals kept in the buffer.")]
        [Header("Capacity")]
        [SerializeField] private int maxSignalEntries = 1024;

        [Tooltip("Maximum number of narrative log entries kept in the buffer.")]
        [SerializeField] private int maxLogEntries = 512;
        #endregion

        #region Filtering
        [Tooltip("Default verbosity filter for narrative templates.")]
        [Header("Filtering")]
        [SerializeField] private EM_NarrativeVerbosity verbosity = EM_NarrativeVerbosity.Standard;

        [Tooltip("Allow designer-only entries in the narrative UI.")]
        [SerializeField] private bool includeDesignerEntries;
        #endregion

        #region Dedup
        [Tooltip("Minimum in-game hours between repeated events with the same key.")]
        [Header("Dedup")]
        [SerializeField] private float dedupWindowHours = 0.25f;
        #endregion

        #region Needs
        [Tooltip("Urgency threshold for medium-level need warnings (0-1).")]
        [Header("Needs")]
        [SerializeField] private float needMediumThreshold = 0.6f;

        [Tooltip("Urgency threshold for high-level need warnings (0-1).")]
        [SerializeField] private float needHighThreshold = 0.8f;

        [Tooltip("Urgency threshold for critical need warnings (0-1).")]
        [SerializeField] private float needCriticalThreshold = 0.95f;

        [Tooltip("Urgency threshold for relief events (0-1).")]
        [SerializeField] private float needReliefThreshold = 0.4f;

        [Tooltip("Minimum in-game hours between need narrative events for the same NPC/need.")]
        [SerializeField] private float needCooldownHours = 0.25f;
        #endregion

        #region Health
        [Tooltip("Normalized health value that triggers a low health narrative (0-1).")]
        [Header("Health")]
        [SerializeField] private float healthLowThreshold = 0.35f;

        [Tooltip("Normalized health value that triggers a critical health narrative (0-1).")]
        [SerializeField] private float healthCriticalThreshold = 0.15f;

        [Tooltip("Normalized health value that signals recovery (0-1).")]
        [SerializeField] private float healthRecoveryThreshold = 0.6f;

        [Tooltip("Minimum normalized damage to log as a health damage narrative.")]
        [SerializeField] private float healthDamageMin = 0.02f;

        [Tooltip("Minimum in-game hours between health narratives for the same NPC.")]
        [SerializeField] private float healthCooldownHours = 0.25f;
        #endregion

        #region Resources
        [Tooltip("Minimum resource delta required to log a resource change.")]
        [Header("Resources")]
        [SerializeField] private float resourceDeltaMin = 1f;

        [Tooltip("Resource amount that marks a low resource warning.")]
        [SerializeField] private float resourceLowThreshold = 1f;

        [Tooltip("Resource amount that marks a depleted resource warning.")]
        [SerializeField] private float resourceDepletedThreshold = 0f;
        #endregion

        #region Relationships
        [Tooltip("Minimum relationship delta to log a relationship change.")]
        [Header("Relationships")]
        [SerializeField] private float relationshipDeltaMin = 0.1f;
        #endregion
        #endregion

        #region Public Properties
        public float RefreshIntervalSeconds
        {
            get
            {
                return refreshIntervalSeconds;
            }
        }

        public int MaxSignalEntries
        {
            get
            {
                return maxSignalEntries;
            }
        }

        public int MaxLogEntries
        {
            get
            {
                return maxLogEntries;
            }
        }

        public EM_NarrativeVerbosity Verbosity
        {
            get
            {
                return verbosity;
            }
        }

        public bool IncludeDesignerEntries
        {
            get
            {
                return includeDesignerEntries;
            }
        }

        public float DedupWindowHours
        {
            get
            {
                return dedupWindowHours;
            }
        }

        public float NeedMediumThreshold
        {
            get
            {
                return needMediumThreshold;
            }
        }

        public float NeedHighThreshold
        {
            get
            {
                return needHighThreshold;
            }
        }

        public float NeedCriticalThreshold
        {
            get
            {
                return needCriticalThreshold;
            }
        }

        public float NeedReliefThreshold
        {
            get
            {
                return needReliefThreshold;
            }
        }

        public float NeedCooldownHours
        {
            get
            {
                return needCooldownHours;
            }
        }

        public float HealthLowThreshold
        {
            get
            {
                return healthLowThreshold;
            }
        }

        public float HealthCriticalThreshold
        {
            get
            {
                return healthCriticalThreshold;
            }
        }

        public float HealthRecoveryThreshold
        {
            get
            {
                return healthRecoveryThreshold;
            }
        }

        public float HealthDamageMin
        {
            get
            {
                return healthDamageMin;
            }
        }

        public float HealthCooldownHours
        {
            get
            {
                return healthCooldownHours;
            }
        }

        public float ResourceDeltaMin
        {
            get
            {
                return resourceDeltaMin;
            }
        }

        public float ResourceLowThreshold
        {
            get
            {
                return resourceLowThreshold;
            }
        }

        public float ResourceDepletedThreshold
        {
            get
            {
                return resourceDepletedThreshold;
            }
        }

        public float RelationshipDeltaMin
        {
            get
            {
                return relationshipDeltaMin;
            }
        }
        #endregion
    }
}
