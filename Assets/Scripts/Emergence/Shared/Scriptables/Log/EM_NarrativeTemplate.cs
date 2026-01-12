using System;
using UnityEngine;

namespace EmergentMechanics
{
    [Serializable]
    public struct EM_NarrativeTemplate
    {
        #region Serialized
        #region Identity
        [Tooltip("Friendly name used to identify this template in the editor.")]
        [Header("Identity")]
        [SerializeField] private string name;

        [Tooltip("Narrative event type matched by this template.")]
        [SerializeField] private EM_NarrativeEventType eventType;

        [Tooltip("Visibility scope for this template.")]
        [SerializeField] private EM_NarrativeVisibility visibility;

        [Tooltip("Severity assigned to the generated narrative entry.")]
        [SerializeField] private EM_NarrativeSeverity severity;

        [Tooltip("Verbosity tier required to select this template.")]
        [SerializeField] private EM_NarrativeVerbosity verbosity;

        [Tooltip("Tags applied to the generated narrative entry.")]
        [SerializeField] private EM_NarrativeTagMask tags;
        #endregion

        #region Selection
        [Tooltip("Template selection weight when multiple templates match.")]
        [Header("Selection")]
        [SerializeField] private float weight;

        [Tooltip("Minimum in-game hours before this template can fire again for the same event key.")]
        [SerializeField] private float cooldownHours;
        #endregion

        #region Filters
        [Tooltip("Exact signal id required for this template. Leave empty to ignore.")]
        [Header("Filters")]
        [SerializeField] private string signalIdEquals;

        [Tooltip("Signal id prefix required for this template. Leave empty to ignore.")]
        [SerializeField] private string signalIdPrefix;

        [Tooltip("Exact need id required for this template. Leave empty to ignore.")]
        [SerializeField] private string needIdEquals;

        [Tooltip("Exact resource id required for this template. Leave empty to ignore.")]
        [SerializeField] private string resourceIdEquals;

        [Tooltip("Exact activity id required for this template. Leave empty to ignore.")]
        [SerializeField] private string activityIdEquals;

        [Tooltip("Exact context id required for this template. Leave empty to ignore.")]
        [SerializeField] private string contextIdEquals;

        [Tooltip("Exact reason id required for this template. Leave empty to ignore.")]
        [SerializeField] private string reasonIdEquals;

        [Tooltip("Filter by effect type when enabled.")]
        [SerializeField] private bool useEffectType;

        [Tooltip("Effect type required for this template.")]
        [SerializeField] private EmergenceEffectType effectType;

        [Tooltip("Filter by minimum Value when enabled.")]
        [SerializeField] private bool useMinValue;

        [Tooltip("Minimum Value required for this template.")]
        [SerializeField] private float minValue;

        [Tooltip("Filter by maximum Value when enabled.")]
        [SerializeField] private bool useMaxValue;

        [Tooltip("Maximum Value required for this template.")]
        [SerializeField] private float maxValue;

        [Tooltip("Filter by minimum Delta when enabled.")]
        [SerializeField] private bool useMinDelta;

        [Tooltip("Minimum Delta required for this template.")]
        [SerializeField] private float minDelta;

        [Tooltip("Filter by maximum Delta when enabled.")]
        [SerializeField] private bool useMaxDelta;

        [Tooltip("Maximum Delta required for this template.")]
        [SerializeField] private float maxDelta;

        [Tooltip("Filter by minimum After when enabled.")]
        [SerializeField] private bool useMinAfter;

        [Tooltip("Minimum After required for this template.")]
        [SerializeField] private float minAfter;

        [Tooltip("Filter by maximum After when enabled.")]
        [SerializeField] private bool useMaxAfter;

        [Tooltip("Maximum After required for this template.")]
        [SerializeField] private float maxAfter;
        #endregion

        #region Output
        [Tooltip("Template for the narrative title line. Tokens: {time}, {subject}, {target}, {society}, {need}, {resource}, {activity}, {intent}, {signal}, {effect}, {context}, {reason}, {value}, {delta}, {before}, {after}, {event}.")]
        [Header("Output")]
        [TextArea(1, 2)]
        [SerializeField] private string titleTemplate;

        [Tooltip("Template for the narrative body line. Tokens: {time}, {subject}, {target}, {society}, {need}, {resource}, {activity}, {intent}, {signal}, {effect}, {context}, {reason}, {value}, {delta}, {before}, {after}, {event}.")]
        [TextArea(2, 4)]
        [SerializeField] private string bodyTemplate;
        #endregion
        #endregion

        #region Public Properties
        public string Name => name;
        public EM_NarrativeEventType EventType => eventType;
        public EM_NarrativeVisibility Visibility => visibility;
        public EM_NarrativeSeverity Severity => severity;
        public EM_NarrativeVerbosity Verbosity => verbosity;
        public EM_NarrativeTagMask Tags => tags;
        public float Weight => weight;
        public float CooldownHours => cooldownHours;
        public string SignalIdEquals => signalIdEquals;
        public string SignalIdPrefix => signalIdPrefix;
        public string NeedIdEquals => needIdEquals;
        public string ResourceIdEquals => resourceIdEquals;
        public string ActivityIdEquals => activityIdEquals;
        public string ContextIdEquals => contextIdEquals;
        public string ReasonIdEquals => reasonIdEquals;
        public bool UseEffectType => useEffectType;
        public EmergenceEffectType EffectType => effectType;
        public bool UseMinValue => useMinValue;
        public float MinValue => minValue;
        public bool UseMaxValue => useMaxValue;
        public float MaxValue => maxValue;
        public bool UseMinDelta => useMinDelta;
        public float MinDelta => minDelta;
        public bool UseMaxDelta => useMaxDelta;
        public float MaxDelta => maxDelta;
        public bool UseMinAfter => useMinAfter;
        public float MinAfter => minAfter;
        public bool UseMaxAfter => useMaxAfter;
        public float MaxAfter => maxAfter;
        public string TitleTemplate => titleTemplate;
        public string BodyTemplate => bodyTemplate;
        #endregion
    }
}
