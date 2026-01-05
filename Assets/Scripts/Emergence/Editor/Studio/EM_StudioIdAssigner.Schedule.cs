using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_StudioIdAssigner
    {
        #region Schedule
        private static int AssignScheduleIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_NpcSchedulePreset> assets = EM_StudioAssetUtility.FindAssets<EM_NpcSchedulePreset>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_NpcSchedulePreset asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty entriesProperty = serialized.FindProperty("entries");
                bool changed = false;

                if (entriesProperty != null && entriesProperty.isArray)
                {
                    for (int e = 0; e < entriesProperty.arraySize; e++)
                    {
                        SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(e);

                        if (entryProperty == null)
                            continue;

                        SerializedProperty activityDefinitionProperty = entryProperty.FindPropertyRelative("activityIdDefinition");
                        SerializedProperty activityLegacyProperty = entryProperty.FindPropertyRelative("activityId");
                        bool activityChanged = AssignIdDefinition(activityDefinitionProperty, activityLegacyProperty, EM_IdCategory.Activity, rootFolder,
                            lookup, asset.name + " Activity", string.Empty, false);
                        if (activityChanged)
                            changed = true;

                        SerializedProperty signalsProperty = entryProperty.FindPropertyRelative("signalEntries");

                        if (signalsProperty == null || !signalsProperty.isArray)
                            continue;

                        for (int s = 0; s < signalsProperty.arraySize; s++)
                        {
                            SerializedProperty signalProperty = signalsProperty.GetArrayElementAtIndex(s);

                            if (signalProperty == null)
                                continue;

                            SerializedProperty startDefinitionProperty = signalProperty.FindPropertyRelative("startSignalIdDefinition");
                            SerializedProperty startLegacyProperty = signalProperty.FindPropertyRelative("startSignalId");
                            bool startChanged = AssignIdDefinition(startDefinitionProperty, startLegacyProperty, EM_IdCategory.Signal, rootFolder,
                                lookup, asset.name + " Start Signal", string.Empty, false);

                            SerializedProperty tickDefinitionProperty = signalProperty.FindPropertyRelative("tickSignalIdDefinition");
                            SerializedProperty tickLegacyProperty = signalProperty.FindPropertyRelative("tickSignalId");
                            bool tickChanged = AssignIdDefinition(tickDefinitionProperty, tickLegacyProperty, EM_IdCategory.Signal, rootFolder,
                                lookup, asset.name + " Tick Signal", string.Empty, false);

                            if (startChanged || tickChanged)
                                changed = true;
                        }
                    }
                }

                if (!changed)
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
