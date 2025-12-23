using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a norm used by institutions to regulate behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Norm Definition", fileName = "EM_NormDefinition")]
    public sealed class EM_NormDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key for this norm. Used by institutions and profiles to reference behavior rules.")]
        [SerializeField] private string normId = "Norm.Id";

        [Tooltip("Designer-facing label shown in tools.")]
        [SerializeField] private string displayName = "Norm";
        #endregion

        // Serialized behavior
        #region Serialized - Behavior
        [Tooltip("Weight used when evaluating compliance versus violation.")]
        [SerializeField] private float complianceWeight = 1f;

        [Tooltip("Effect applied when the norm is violated.")]
        [SerializeField] private EM_EffectDefinition violationEffect;

        [Tooltip("Severity used by institutions to scale enforcement response.")]
        [SerializeField] private float severity = 1f;
        #endregion

        // Serialized notes
        #region Serialized - Notes
        [Tooltip("Describe the social rule and how it should influence behavior.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the norm identifier.
        /// </summary>
        public string NormId
        {
            get
            {
                return normId;
            }
        }

        /// <summary>
        /// Gets the display name for this norm.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the compliance weight.
        /// </summary>
        public float ComplianceWeight
        {
            get
            {
                return complianceWeight;
            }
        }

        /// <summary>
        /// Gets the effect applied on violation.
        /// </summary>
        public EM_EffectDefinition ViolationEffect
        {
            get
            {
                return violationEffect;
            }
        }

        /// <summary>
        /// Gets the severity of this norm.
        /// </summary>
        public float Severity
        {
            get
            {
                return severity;
            }
        }

        /// <summary>
        /// Gets the designer description for this norm.
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
