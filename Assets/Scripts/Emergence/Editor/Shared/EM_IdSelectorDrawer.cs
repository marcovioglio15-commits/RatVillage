using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    [CustomPropertyDrawer(typeof(EM_IdSelectorAttribute))]
    public sealed class EM_IdSelectorDrawer : PropertyDrawer
    {
        #region Constants
        private const float HelpBoxHeight = 34f;
        #endregion

        #region Overrides
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float height = lineHeight;
            EM_IdDefinition definition = property.objectReferenceValue as EM_IdDefinition;

            if (definition != null)
                height += EditorGUIUtility.standardVerticalSpacing + lineHeight;

            if (definition != null && !IsCategoryAllowed(definition.Category, GetAllowedCategories()))
                height += EditorGUIUtility.standardVerticalSpacing + HelpBoxHeight;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect fieldRect = new Rect(position.x, position.y, position.width, lineHeight);
            EditorGUI.ObjectField(fieldRect, property, typeof(EM_IdDefinition), label);

            EM_IdDefinition definition = property.objectReferenceValue as EM_IdDefinition;

            if (definition != null)
            {
                Rect infoRect = new Rect(position.x, fieldRect.yMax + spacing, position.width, lineHeight);
                string info = string.Format("Id: {0} | Category: {1}", definition.Id, definition.Category);
                EditorGUI.LabelField(infoRect, info);

                if (!IsCategoryAllowed(definition.Category, GetAllowedCategories()))
                {
                    Rect helpRect = new Rect(position.x, infoRect.yMax + spacing, position.width, HelpBoxHeight);
                    EditorGUI.HelpBox(helpRect, "Assigned id category does not match the expected category.", MessageType.Warning);
                }
            }

            EditorGUI.EndProperty();
        }
        #endregion

        #region Helpers
        private EM_IdCategory[] GetAllowedCategories()
        {
            EM_IdSelectorAttribute selector = attribute as EM_IdSelectorAttribute;

            if (selector == null)
                return null;

            return selector.Categories;
        }

        private static bool IsCategoryAllowed(EM_IdCategory category, EM_IdCategory[] allowed)
        {
            if (allowed == null || allowed.Length == 0)
                return true;

            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == EM_IdCategory.Any)
                    return true;

                if (allowed[i] == category)
                    return true;
            }

            return false;
        }
        #endregion
    }
}
