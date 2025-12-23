using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a runtime effect applied by rule evaluation.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Effect Definition", fileName = "EM_EffectDefinition")]
    public sealed class EM_EffectDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key used by rules to reference this effect. Keep stable once used.")]
        [SerializeField] private string effectId = "Effect.Id";

        [Tooltip("Designer-facing label shown in tools and inspectors.")]
        [SerializeField] private string displayName = "Effect";
        #endregion

        // Serialized behavior
        #region Serialized - Behavior
        [Tooltip("Runtime channel to modify, such as needs, resources, reputation, or cohesion.")]
        [SerializeField] private EmergenceEffectType effectType = EmergenceEffectType.ModifyNeed;

        [Tooltip("Target scope for the effect, such as event target or society root.")]
        [SerializeField] private EmergenceEffectTarget target = EmergenceEffectTarget.EventTarget;

        [Tooltip("Optional parameter id, for example Need.Hunger or Resource.Food, depending on the effect type.")]
        [SerializeField] private string parameterId = "";

        [Tooltip("Base magnitude applied before rule and signal weighting.")]
        [SerializeField] private float magnitude = 1f;
        #endregion

        // Serialized clamping
        #region Serialized - Clamping
        [Tooltip("Enable clamping after scaling to keep values in safe bounds.")]
        [SerializeField] private bool useClamp;

        [Tooltip("Lower clamp bound when clamping is enabled.")]
        [SerializeField] private float minValue;

        [Tooltip("Upper clamp bound when clamping is enabled.")]
        [SerializeField] private float maxValue = 1f;
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the effect identifier.
        /// </summary>
        public string EffectId
        {
            get
            {
                return effectId;
            }
        }

        /// <summary>
        /// Gets the display name for this effect.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the effect type.
        /// </summary>
        public EmergenceEffectType EffectType
        {
            get
            {
                return effectType;
            }
        }

        /// <summary>
        /// Gets the target scope for this effect.
        /// </summary>
        public EmergenceEffectTarget Target
        {
            get
            {
                return target;
            }
        }

        /// <summary>
        /// Gets the parameter identifier for this effect.
        /// </summary>
        public string ParameterId
        {
            get
            {
                return parameterId;
            }
        }

        /// <summary>
        /// Gets the effect magnitude.
        /// </summary>
        public float Magnitude
        {
            get
            {
                return magnitude;
            }
        }

        /// <summary>
        /// Gets whether clamping is enabled.
        /// </summary>
        public bool UseClamp
        {
            get
            {
                return useClamp;
            }
        }

        /// <summary>
        /// Gets the minimum clamp value.
        /// </summary>
        public float MinValue
        {
            get
            {
                return minValue;
            }
        }

        /// <summary>
        /// Gets the maximum clamp value.
        /// </summary>
        public float MaxValue
        {
            get
            {
                return maxValue;
            }
        }
        #endregion
    }
}
