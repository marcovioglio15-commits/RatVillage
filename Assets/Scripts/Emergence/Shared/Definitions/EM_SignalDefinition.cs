using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a signal emitted by simulation systems or gameplay sources.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Signal Definition", fileName = "EM_SignalDefinition")]
    public sealed class EM_SignalDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Stable runtime key used by emitters and rule sets. Treat this like an API id: keep it stable after it is referenced in rules or profiles to avoid breaking mappings.")]
        [SerializeField] private string signalId = "Signal.Id";

        [Tooltip("Designer-facing label shown in Emergence Studio and inspectors. Safe to rename for readability without affecting runtime links.")]
        [SerializeField] private string displayName = "Signal";
        #endregion

        // Serialized classification
        #region Serialized - Classification
        [Tooltip("Domain owner for this signal. Domains are high-level categories that group related mechanics and let you enable, disable, or rebalance whole families of behaviors at once.")]
        [SerializeField] private EM_DomainDefinition domain;

        [Tooltip("Lowest LOD tier allowed to process this signal. Use this to drop low-priority signals when the simulation is simplified or far from the camera.")]
        [SerializeField] private EmergenceLodTier minimumLod = EmergenceLodTier.Full;
        #endregion

        // Serialized tuning
        #region Serialized - Tuning
        [Tooltip("Base multiplier applied to all rules triggered by this signal. Use this to dampen or boost the entire signal category without editing each rule.")]
        [SerializeField] private float defaultWeight = 1f;

        [Tooltip("Explain what event this signal represents, which systems should emit it, and typical gameplay moments that produce it.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the stable identifier for this signal.
        /// </summary>
        public string SignalId
        {
            get
            {
                return signalId;
            }
        }

        /// <summary>
        /// Gets the display name for this signal.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the domain definition for this signal.
        /// </summary>
        public EM_DomainDefinition Domain
        {
            get
            {
                return domain;
            }
        }

        /// <summary>
        /// Gets the minimum LOD tier required to process this signal.
        /// </summary>
        public EmergenceLodTier MinimumLod
        {
            get
            {
                return minimumLod;
            }
        }

        /// <summary>
        /// Gets the default weight for rule evaluation.
        /// </summary>
        public float DefaultWeight
        {
            get
            {
                return defaultWeight;
            }
        }

        /// <summary>
        /// Gets the designer description for this signal.
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
