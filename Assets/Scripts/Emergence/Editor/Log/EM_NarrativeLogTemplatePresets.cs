using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_NarrativeLogTemplatePresets
    {
        #region Presets
        public static void ApplySampleTemplates(EM_NarrativeLogTemplates templates)
        {
            if (templates == null)
                return;

            SerializedObject serializedObject = new SerializedObject(templates);
            SerializedProperty templatesProperty = serializedObject.FindProperty("templates");
            EM_NarrativeTemplateDefinition[] definitions = BuildSampleDefinitions();
            templatesProperty.arraySize = definitions.Length;

            for (int i = 0; i < definitions.Length; i++)
            {
                SerializedProperty element = templatesProperty.GetArrayElementAtIndex(i);
                WriteTemplate(element, definitions[i]);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(templates);
        }

        public static void ApplyFilteredTemplates(EM_NarrativeLogTemplates templates)
        {
            if (templates == null)
                return;

            SerializedObject serializedObject = new SerializedObject(templates);
            SerializedProperty templatesProperty = serializedObject.FindProperty("templates");
            EM_NarrativeTemplateDefinition[] definitions = BuildFilteredDefinitions();
            templatesProperty.arraySize = definitions.Length;

            for (int i = 0; i < definitions.Length; i++)
            {
                SerializedProperty element = templatesProperty.GetArrayElementAtIndex(i);
                WriteTemplate(element, definitions[i]);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(templates);
        }
        #endregion

        #region Serialization
        private static void WriteTemplate(SerializedProperty element, EM_NarrativeTemplateDefinition definition)
        {
            SetString(element, "name", definition.Name);
            SetEnum(element, "eventType", (int)definition.EventType);
            SetEnum(element, "visibility", (int)definition.Visibility);
            SetEnum(element, "severity", (int)definition.Severity);
            SetEnum(element, "verbosity", (int)definition.Verbosity);
            SetInt(element, "tags", (int)definition.Tags);
            SetFloat(element, "weight", definition.Weight);
            SetFloat(element, "cooldownHours", definition.CooldownHours);
            SetString(element, "signalIdEquals", definition.SignalIdEquals);
            SetString(element, "signalIdPrefix", definition.SignalIdPrefix);
            SetString(element, "needIdEquals", definition.NeedIdEquals);
            SetString(element, "resourceIdEquals", definition.ResourceIdEquals);
            SetString(element, "activityIdEquals", definition.ActivityIdEquals);
            SetString(element, "contextIdEquals", definition.ContextIdEquals);
            SetString(element, "reasonIdEquals", definition.ReasonIdEquals);
            SetBool(element, "useEffectType", definition.UseEffectType);
            SetEnum(element, "effectType", (int)definition.EffectType);
            SetBool(element, "useMinValue", definition.UseMinValue);
            SetFloat(element, "minValue", definition.MinValue);
            SetBool(element, "useMaxValue", definition.UseMaxValue);
            SetFloat(element, "maxValue", definition.MaxValue);
            SetBool(element, "useMinDelta", definition.UseMinDelta);
            SetFloat(element, "minDelta", definition.MinDelta);
            SetBool(element, "useMaxDelta", definition.UseMaxDelta);
            SetFloat(element, "maxDelta", definition.MaxDelta);
            SetBool(element, "useMinAfter", definition.UseMinAfter);
            SetFloat(element, "minAfter", definition.MinAfter);
            SetBool(element, "useMaxAfter", definition.UseMaxAfter);
            SetFloat(element, "maxAfter", definition.MaxAfter);
            SetString(element, "titleTemplate", definition.TitleTemplate);
            SetString(element, "bodyTemplate", definition.BodyTemplate);
        }

        private static void SetString(SerializedProperty element, string fieldName, string value)
        {
            SerializedProperty property = element.FindPropertyRelative(fieldName);

            if (property == null)
                return;

            property.stringValue = value ?? string.Empty;
        }

        private static void SetFloat(SerializedProperty element, string fieldName, float value)
        {
            SerializedProperty property = element.FindPropertyRelative(fieldName);

            if (property == null)
                return;

            property.floatValue = value;
        }

        private static void SetBool(SerializedProperty element, string fieldName, bool value)
        {
            SerializedProperty property = element.FindPropertyRelative(fieldName);

            if (property == null)
                return;

            property.boolValue = value;
        }

        private static void SetEnum(SerializedProperty element, string fieldName, int value)
        {
            SerializedProperty property = element.FindPropertyRelative(fieldName);

            if (property == null)
                return;

            property.enumValueIndex = value;
        }

        private static void SetInt(SerializedProperty element, string fieldName, int value)
        {
            SerializedProperty property = element.FindPropertyRelative(fieldName);

            if (property == null)
                return;

            property.intValue = value;
        }
        #endregion

        #region Definition
        private struct EM_NarrativeTemplateDefinition
        {
            public string Name;
            public EM_NarrativeEventType EventType;
            public EM_NarrativeVisibility Visibility;
            public EM_NarrativeSeverity Severity;
            public EM_NarrativeVerbosity Verbosity;
            public EM_NarrativeTagMask Tags;
            public float Weight;
            public float CooldownHours;
            public string SignalIdEquals;
            public string SignalIdPrefix;
            public string NeedIdEquals;
            public string ResourceIdEquals;
            public string ActivityIdEquals;
            public string ContextIdEquals;
            public string ReasonIdEquals;
            public bool UseEffectType;
            public EmergenceEffectType EffectType;
            public bool UseMinValue;
            public float MinValue;
            public bool UseMaxValue;
            public float MaxValue;
            public bool UseMinDelta;
            public float MinDelta;
            public bool UseMaxDelta;
            public float MaxDelta;
            public bool UseMinAfter;
            public float MinAfter;
            public bool UseMaxAfter;
            public float MaxAfter;
            public string TitleTemplate;
            public string BodyTemplate;

            public static EM_NarrativeTemplateDefinition CreateDefault()
            {
                return new EM_NarrativeTemplateDefinition
                {
                    Name = "",
                    EventType = EM_NarrativeEventType.NeedUrgency,
                    Visibility = EM_NarrativeVisibility.Player,
                    Severity = EM_NarrativeSeverity.Info,
                    Verbosity = EM_NarrativeVerbosity.Standard,
                    Tags = EM_NarrativeTagMask.None,
                    Weight = 1f,
                    CooldownHours = 0f,
                    SignalIdEquals = string.Empty,
                    SignalIdPrefix = string.Empty,
                    NeedIdEquals = string.Empty,
                    ResourceIdEquals = string.Empty,
                    ActivityIdEquals = string.Empty,
                    ContextIdEquals = string.Empty,
                    ReasonIdEquals = string.Empty,
                    UseEffectType = false,
                    EffectType = EmergenceEffectType.ModifyNeed,
                    UseMinValue = false,
                    MinValue = 0f,
                    UseMaxValue = false,
                    MaxValue = 0f,
                    UseMinDelta = false,
                    MinDelta = 0f,
                    UseMaxDelta = false,
                    MaxDelta = 0f,
                    UseMinAfter = false,
                    MinAfter = 0f,
                    UseMaxAfter = false,
                    MaxAfter = 0f,
                    TitleTemplate = string.Empty,
                    BodyTemplate = string.Empty
                };
            }

            public EM_NarrativeTemplateDefinition WithVerbosity(EM_NarrativeVerbosity verbosity)
            {
                Verbosity = verbosity;
                return this;
            }
        }
        #endregion
    }
}
