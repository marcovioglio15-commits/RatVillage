using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a domain used to group rules, signals, and tuning parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Domain Definition", fileName = "EM_DomainDefinition")]
    public sealed class EM_DomainDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key used to group signals and rule sets. Treat as a stable category id so profiles and masks remain valid.")]
        [SerializeField] private string domainId = "Domain.Id";

        [Tooltip("Designer-facing label shown in tools and debug views. Use clear category names (e.g., Economy, Social, Survival).")]
        [SerializeField] private string displayName = "Domain";
        #endregion

        // Serialized visualization
        #region Serialized - Visualization
        [Tooltip("Color used in Emergence Studio and debug overlays to identify this domain category at a glance.")]
        [SerializeField] private Color domainColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        #endregion

        // Serialized tuning
        #region Serialized - Tuning
        [Tooltip("Master enable for this domain in profiles. Disabling the domain mutes all associated rule sets and signals at runtime.")]
        [SerializeField] private bool isEnabled = true;

        [Tooltip("Relative budget weight for this domain. Higher values get more update attention when the simulation is throttled or scaled.")]
        [SerializeField] private float updateWeight = 1f;

        [Tooltip("Rule sets owned by this domain. Use this to keep related behaviors organized and to enable/disable them together.")]
        [SerializeField] private EM_RuleSetDefinition[] ruleSets = new EM_RuleSetDefinition[0];
        #endregion

        // Serialized notes
        #region Serialized - Notes
        [Tooltip("Design intent for this domain, what behaviors it should drive, and how it should be tuned relative to other domains.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the domain identifier.
        /// </summary>
        public string DomainId
        {
            get
            {
                return domainId;
            }
        }

        /// <summary>
        /// Gets the display name for this domain.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the domain color for visualization.
        /// </summary>
        public Color DomainColor
        {
            get
            {
                return domainColor;
            }
        }

        /// <summary>
        /// Gets whether this domain is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
        }

        /// <summary>
        /// Gets the relative update weight for this domain.
        /// </summary>
        public float UpdateWeight
        {
            get
            {
                return updateWeight;
            }
        }

        /// <summary>
        /// Gets the rule sets linked to this domain.
        /// </summary>
        public EM_RuleSetDefinition[] RuleSets
        {
            get
            {
                return ruleSets;
            }
        }

        /// <summary>
        /// Gets the designer description for this domain.
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
