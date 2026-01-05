using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Effect Definition", fileName = "EM_EffectDefinition")]
    public sealed class EM_EffectDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Id definition that supplies the unique key for this effect.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.Effect)]
        [SerializeField] private EM_IdDefinition effectIdDefinition;

        [Tooltip("Legacy effect id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string effectId = "Effect.Id";
        #endregion

        #region Behavior
        [Tooltip("Runtime channel to modify, such as needs, resources, reputation, or cohesion.")]
        [Header("Behavior")]
        [SerializeField] private EmergenceEffectType effectType = EmergenceEffectType.ModifyNeed;

        [Tooltip("Target scope for the effect, such as event target or society root.")]
        [SerializeField] private EmergenceEffectTarget target = EmergenceEffectTarget.EventTarget;

        [Tooltip("Optional id definition used as the primary parameter for this effect (need, resource, activity, intent, signal, context).")]
        [EM_IdSelector(EM_IdCategory.Any)]
        [SerializeField] private EM_IdDefinition parameterIdDefinition;

        [Tooltip("Legacy parameter id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string parameterId = "";

        [Tooltip("Optional id definition used as a secondary parameter for this effect (resource or context overrides).")]
        [EM_IdSelector(EM_IdCategory.Any)]
        [SerializeField] private EM_IdDefinition secondaryIdDefinition;

        [Tooltip("Legacy secondary id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string secondaryId = "";

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
                return EM_IdUtility.ResolveId(effectIdDefinition, effectId);
            }
        }

        public EM_IdDefinition EffectIdDefinition
        {
            get
            {
                return effectIdDefinition;
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
                return EM_IdUtility.ResolveId(parameterIdDefinition, parameterId);
            }
        }

        public string SecondaryId
        {
            get
            {
                return EM_IdUtility.ResolveId(secondaryIdDefinition, secondaryId);
            }
        }

        public EM_IdDefinition ParameterIdDefinition
        {
            get
            {
                return parameterIdDefinition;
            }
        }

        public EM_IdDefinition SecondaryIdDefinition
        {
            get
            {
                return secondaryIdDefinition;
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
