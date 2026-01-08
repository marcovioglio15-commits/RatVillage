using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    [CustomPropertyDrawer(typeof(EM_NpcSchedulePreset.ScheduleEntry))]
    public sealed class EM_NpcScheduleEntryDrawer : PropertyDrawer
    {
        #region Overrides
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = lineHeight;

            if (!property.isExpanded)
                return height;

            height += spacing;
            height += GetChildHeight(property, "activityIdDefinition", false);
            height += spacing;
            height += GetChildHeight(property, "startHour", false);
            height += spacing;
            height += GetChildHeight(property, "endHour", false);
            height += spacing;

            SerializedProperty useDurationProperty = property.FindPropertyRelative("useDuration");

            if (useDurationProperty != null)
                height += EditorGUI.GetPropertyHeight(useDurationProperty, false);

            if (useDurationProperty != null && useDurationProperty.boolValue)
            {
                height += spacing;
                height += GetChildHeight(property, "minDurationHours", false);
                height += spacing;
                height += GetChildHeight(property, "maxDurationHours", false);
            }

            SerializedProperty tradePolicyProperty = property.FindPropertyRelative("tradePolicy");

            if (tradePolicyProperty != null)
            {
                height += spacing;
                height += EditorGUI.GetPropertyHeight(tradePolicyProperty, false);

                if (tradePolicyProperty.enumValueIndex == (int)EM_ScheduleTradePolicy.AllowOnlyListed)
                {
                    height += spacing;
                    height += GetChildHeight(property, "allowedTradeNeeds", true);
                }
            }

            height += spacing;
            height += GetChildHeight(property, "signalEntries", true);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            Rect contentRect = new Rect(position.x, foldoutRect.yMax + spacing, position.width, position.height - lineHeight);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent + 1;

            DrawChildProperty(ref contentRect, property, "activityIdDefinition", false);
            DrawChildProperty(ref contentRect, property, "startHour", false);
            DrawChildProperty(ref contentRect, property, "endHour", false);

            SerializedProperty useDurationProperty = property.FindPropertyRelative("useDuration");

            if (useDurationProperty != null)
            {
                DrawProperty(ref contentRect, useDurationProperty, false);

                if (useDurationProperty.boolValue)
                {
                    DrawChildProperty(ref contentRect, property, "minDurationHours", false);
                    DrawChildProperty(ref contentRect, property, "maxDurationHours", false);
                }
            }

            SerializedProperty tradePolicyProperty = property.FindPropertyRelative("tradePolicy");

            if (tradePolicyProperty != null)
            {
                DrawProperty(ref contentRect, tradePolicyProperty, false);

                if (tradePolicyProperty.enumValueIndex == (int)EM_ScheduleTradePolicy.AllowOnlyListed)
                    DrawChildProperty(ref contentRect, property, "allowedTradeNeeds", true);
            }

            DrawChildProperty(ref contentRect, property, "signalEntries", true);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
        #endregion

        #region Helpers
        private static float GetChildHeight(SerializedProperty property, string name, bool includeChildren)
        {
            SerializedProperty child = property.FindPropertyRelative(name);

            if (child == null)
                return 0f;

            return EditorGUI.GetPropertyHeight(child, includeChildren);
        }

        private static void DrawChildProperty(ref Rect rect, SerializedProperty parent, string name, bool includeChildren)
        {
            SerializedProperty child = parent.FindPropertyRelative(name);

            if (child == null)
                return;

            DrawProperty(ref rect, child, includeChildren);
        }

        private static void DrawProperty(ref Rect rect, SerializedProperty property, bool includeChildren)
        {
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            Rect fieldRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.PropertyField(fieldRect, property, includeChildren);
            rect.y += height + EditorGUIUtility.standardVerticalSpacing;
        }
        #endregion
    }
}
