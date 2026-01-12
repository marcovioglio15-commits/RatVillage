using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    [CustomEditor(typeof(EM_EffectDefinition))]
    public sealed class EM_EffectDefinitionEditor : Editor
    {
        #region Fields
        private SerializedProperty effectIdDefinitionProperty;
        private SerializedProperty effectTypeProperty;
        private SerializedProperty targetProperty;
        private SerializedProperty parameterIdDefinitionProperty;
        private SerializedProperty secondaryIdDefinitionProperty;
        private SerializedProperty magnitudeProperty;
        private SerializedProperty useClampProperty;
        private SerializedProperty minValueProperty;
        private SerializedProperty maxValueProperty;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            effectIdDefinitionProperty = serializedObject.FindProperty("effectIdDefinition");
            effectTypeProperty = serializedObject.FindProperty("effectType");
            targetProperty = serializedObject.FindProperty("target");
            parameterIdDefinitionProperty = serializedObject.FindProperty("parameterIdDefinition");
            secondaryIdDefinitionProperty = serializedObject.FindProperty("secondaryIdDefinition");
            magnitudeProperty = serializedObject.FindProperty("magnitude");
            useClampProperty = serializedObject.FindProperty("useClamp");
            minValueProperty = serializedObject.FindProperty("minValue");
            maxValueProperty = serializedObject.FindProperty("maxValue");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawIdentitySection();
            EditorGUILayout.Space(6f);
            DrawBehaviorSection();
            EditorGUILayout.Space(6f);
            DrawClampingSection();

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Identity
        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);

            GUIContent idContent = new GUIContent("Effect Id", "Id definition that supplies the unique key for this effect.");
            EditorGUILayout.PropertyField(effectIdDefinitionProperty, idContent);

            if (effectIdDefinitionProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign an Effect Id Definition to keep effects stable across renames.", MessageType.Warning);
            }
        }
        #endregion

        #region Behavior
        private void DrawBehaviorSection()
        {
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(effectTypeProperty, new GUIContent("Effect Type", "Defines what the effect modifies at runtime."));

            EmergenceEffectType effectType = GetEffectType();
            string summary = GetEffectSummary(effectType);

            if (!string.IsNullOrEmpty(summary))
                EditorGUILayout.HelpBox(summary, MessageType.Info);

            EditorGUILayout.PropertyField(targetProperty, new GUIContent("Target", "Select which entity receives the effect."));

            string targetHint = GetTargetHint();

            if (!string.IsNullOrEmpty(targetHint))
                EditorGUILayout.HelpBox(targetHint, MessageType.None);

            DrawParameterFields(effectType);
            DrawMagnitudeField(effectType);
        }

        private void DrawParameterFields(EmergenceEffectType effectType)
        {
            ParameterLabels labels = GetParameterLabels(effectType);

            if (!labels.UsesParameter && !labels.UsesSecondary)
            {
                EditorGUILayout.HelpBox("This effect does not use Parameter or Secondary ids.", MessageType.None);
                return;
            }

            if (labels.UsesParameter)
            {
                EditorGUILayout.PropertyField(parameterIdDefinitionProperty, labels.ParameterLabel);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(parameterIdDefinitionProperty,
                        new GUIContent("Parameter Id (Unused)", "Unused for this effect type."));
                }
            }

            if (labels.UsesSecondary)
            {
                EditorGUILayout.PropertyField(secondaryIdDefinitionProperty, labels.SecondaryLabel);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(secondaryIdDefinitionProperty,
                        new GUIContent("Secondary Id (Unused)", "Unused for this effect type."));
                }
            }

            if (!string.IsNullOrEmpty(labels.Help))
                EditorGUILayout.HelpBox(labels.Help, MessageType.None);
        }

        private void DrawMagnitudeField(EmergenceEffectType effectType)
        {
            GUIContent magnitudeLabel = GetMagnitudeLabel(effectType);
            EditorGUILayout.PropertyField(magnitudeProperty, magnitudeLabel);
        }
        #endregion

        #region Clamping
        private void DrawClampingSection()
        {
            EditorGUILayout.LabelField("Clamping", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useClampProperty, new GUIContent("Use Clamp", "Clamp the scaled magnitude to safe bounds."));

            if (!useClampProperty.boolValue)
                return;

            EditorGUILayout.PropertyField(minValueProperty, new GUIContent("Min Value", "Lower clamp bound."));
            EditorGUILayout.PropertyField(maxValueProperty, new GUIContent("Max Value", "Upper clamp bound."));
        }
        #endregion

        #region Helpers
        private EmergenceEffectType GetEffectType()
        {
            if (effectTypeProperty == null)
                return EmergenceEffectType.ModifyNeed;

            return (EmergenceEffectType)effectTypeProperty.enumValueIndex;
        }

        private static string GetTargetHint()
        {
            return "Target: EventTarget uses the rule subject, SignalTarget uses the signal target, SocietyRoot uses the subject society root.";
        }

        private static string GetEffectSummary(EmergenceEffectType effectType)
        {
            switch (effectType)
            {
                case EmergenceEffectType.ModifyNeed:
                    return "Modify a need value on the target NPC.";
                case EmergenceEffectType.ModifyResource:
                    return "Modify a resource amount on the target NPC.";
                case EmergenceEffectType.ModifyReputation:
                    return "Modify society reputation on the resolved target.";
                case EmergenceEffectType.ModifyCohesion:
                    return "Modify society cohesion on the resolved target.";
                case EmergenceEffectType.OverrideSchedule:
                    return "Override the NPC schedule for a duration in hours.";
                case EmergenceEffectType.ModifyRelationship:
                    return "Adjust affinity between the target NPC and the other party in the event.";
                case EmergenceEffectType.AddIntent:
                    return "Create or update an intent entry on the target NPC.";
                case EmergenceEffectType.EmitSignal:
                    return "Emit a signal event from the target NPC.";
                case EmergenceEffectType.ModifyHealth:
                    return "Modify health on the target NPC.";
                default:
                    return string.Empty;
            }
        }

        private static GUIContent GetMagnitudeLabel(EmergenceEffectType effectType)
        {
            switch (effectType)
            {
                case EmergenceEffectType.OverrideSchedule:
                    return new GUIContent("Duration (Hours)", "Duration in hours for the schedule override.");
                case EmergenceEffectType.AddIntent:
                    return new GUIContent("Urgency Weight", "Base urgency scaled by rule priority (clamped 0-1).");
                case EmergenceEffectType.EmitSignal:
                    return new GUIContent("Signal Value", "Value carried by the emitted signal.");
                default:
                    return new GUIContent("Magnitude", "Base magnitude applied after rule and effect weighting.");
            }
        }

        private static ParameterLabels GetParameterLabels(EmergenceEffectType effectType)
        {
            switch (effectType)
            {
                case EmergenceEffectType.ModifyNeed:
                    return new ParameterLabels(true, false,
                        new GUIContent("Need Id", "Need id modified by this effect."),
                        new GUIContent("Secondary Id", "Unused."),
                        "Secondary id is ignored for Modify Need.");
                case EmergenceEffectType.ModifyResource:
                    return new ParameterLabels(true, false,
                        new GUIContent("Resource Id", "Resource id modified by this effect."),
                        new GUIContent("Secondary Id", "Unused."),
                        "Secondary id is ignored for Modify Resource.");
                case EmergenceEffectType.OverrideSchedule:
                    return new ParameterLabels(true, false,
                        new GUIContent("Activity Id", "Schedule activity to override."),
                        new GUIContent("Secondary Id", "Unused."),
                        "Secondary id is ignored for Schedule Override.");
                case EmergenceEffectType.AddIntent:
                    return new ParameterLabels(true, true,
                        new GUIContent("Intent Id", "Intent definition to add or update."),
                        new GUIContent("Resource Override", "Optional resource id that overrides the intent resource."),
                        "Uses the signal context as the Need Id when present.");
                case EmergenceEffectType.EmitSignal:
                    return new ParameterLabels(true, true,
                        new GUIContent("Signal Id", "Signal id to emit."),
                        new GUIContent("Context Override", "Optional context id override for the emitted signal."),
                        "Signal value is taken from Magnitude.");
                default:
                    return new ParameterLabels(false, false, GUIContent.none, GUIContent.none, string.Empty);
            }
        }

        private readonly struct ParameterLabels
        {
            public readonly bool UsesParameter;
            public readonly bool UsesSecondary;
            public readonly GUIContent ParameterLabel;
            public readonly GUIContent SecondaryLabel;
            public readonly string Help;

            public ParameterLabels(bool usesParameter, bool usesSecondary, GUIContent parameterLabel, GUIContent secondaryLabel, string help)
            {
                UsesParameter = usesParameter;
                UsesSecondary = usesSecondary;
                ParameterLabel = parameterLabel;
                SecondaryLabel = secondaryLabel;
                Help = help;
            }
        }
        #endregion
    }
}
