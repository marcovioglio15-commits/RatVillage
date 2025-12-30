using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Effect Definition", fileName = "EM_EffectDefinition")]
    public sealed class EM_EffectDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Unique key used by rules to reference this effect. Keep stable once used.")]
        [Header("Identity")]
        [SerializeField] private string effectId = "Effect.Id";

        [Tooltip("Designer-facing label shown in tools and inspectors.")]
        [SerializeField] private string displayName = "Effect";
        #endregion

        #region Behavior
        [Tooltip("Runtime channel to modify, such as needs, resources, reputation, or cohesion.")]
        [Header("Behavior")]
        [SerializeField] private EmergenceEffectType effectType = EmergenceEffectType.ModifyNeed;

        [Tooltip("Target scope for the effect, such as event target or society root.")]
        [SerializeField] private EmergenceEffectTarget target = EmergenceEffectTarget.EventTarget;

        [Tooltip("Optional parameter id, for example Need.Hunger, Resource.Food, or an ActivityId for OverrideSchedule.")]
        [SerializeField] private string parameterId = "";

        [Tooltip("Base magnitude applied before rule and signal weighting.")]
        [SerializeField] private float magnitude = 1f;
        #endregion

        #region Clamping
        [Tooltip("Enable clamping after scaling to keep values in safe bounds.")]
        [Header("Clamping")]
        [SerializeField] private bool useClamp;

        [Tooltip("Lower clamp bound when clamping is enabled.")]
        [SerializeField] private float minValue;

        [Tooltip("Upper clamp bound when clamping is enabled.")]
        [SerializeField] private float maxValue = 1f;
        #endregion
        #endregion

        #region Public Properties
        public string EffectId
        {
            get
            {
                return effectId;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public EmergenceEffectType EffectType
        {
            get
            {
                return effectType;
            }
        }

        public EmergenceEffectTarget Target
        {
            get
            {
                return target;
            }
        }

        public string ParameterId
        {
            get
            {
                return parameterId;
            }
        }

        public float Magnitude
        {
            get
            {
                return magnitude;
            }
        }

        public bool UseClamp
        {
            get
            {
                return useClamp;
            }
        }

        public float MinValue
        {
            get
            {
                return minValue;
            }
        }

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
