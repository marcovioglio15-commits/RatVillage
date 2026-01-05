using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_StudioIdAssigner
    {
        #region Rule Sets
        private static int AssignRuleSetIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_RuleSetDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_RuleSetDefinition>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_RuleSetDefinition asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("ruleSetIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("ruleSetId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.RuleSet, rootFolder,
                    lookup, asset.name, "RuleSet", true);

                SerializedProperty rulesProperty = serialized.FindProperty("rules");
                bool rulesChanged = false;

                if (rulesProperty != null && rulesProperty.isArray)
                {
                    for (int r = 0; r < rulesProperty.arraySize; r++)
                    {
                        SerializedProperty ruleProperty = rulesProperty.GetArrayElementAtIndex(r);

                        if (ruleProperty == null)
                            continue;

                        SerializedProperty contextDefinitionProperty = ruleProperty.FindPropertyRelative("contextIdDefinition");
                        SerializedProperty contextLegacyProperty = ruleProperty.FindPropertyRelative("contextIdFilter");
                        bool entryChanged = AssignIdDefinition(contextDefinitionProperty, contextLegacyProperty, EM_IdCategory.Context, rootFolder,
                            lookup, asset.name + " Context", string.Empty, false);

                        if (entryChanged)
                            rulesChanged = true;
                    }
                }

                if (!changed && !rulesChanged)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
        #endregion
    }
}
