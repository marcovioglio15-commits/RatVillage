using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Schedule
        private static EM_NpcSchedulePreset CreateSchedule(string rootFolder, string assetName, EM_StudioPresetTuning tuning,
            EM_IdDefinition seekFoodId, EM_IdDefinition seekWaterId, EM_IdDefinition sleepId, EM_IdDefinition workId,
            EM_IdDefinition workProduceFoodSignalId, EM_IdDefinition workProduceWaterSignalId)
        {
            string folder = EM_StudioAssetUtility.GetScheduleFolder(rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_NpcSchedulePreset asset = ScriptableObject.CreateInstance<EM_NpcSchedulePreset>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("curveSamples").intValue = 32;

            SerializedProperty entriesProperty = serialized.FindProperty("entries");
            entriesProperty.arraySize = 4;

            ConfigureScheduleEntry(entriesProperty, 0, seekFoodId, 0f, 0f, 0f, BuildFlatCurve(1f), false, null);
            ConfigureScheduleEntry(entriesProperty, 1, seekWaterId, 0f, 0f, 0f, BuildFlatCurve(1f), false, null);
            ConfigureScheduleEntry(entriesProperty, 2, sleepId, 0f, 0f, 0f, BuildFlatCurve(1f), false, null);

            float workTickInterval = 1f * tuning.ScheduleTickMultiplier;
            EM_IdDefinition[] workSignals = new EM_IdDefinition[] { workProduceFoodSignalId, workProduceWaterSignalId };
            ConfigureScheduleEntry(entriesProperty, 3, workId, 6f, 24f, workTickInterval, BuildFlatCurve(1f), true, workSignals);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ConfigureScheduleEntry(SerializedProperty entriesProperty, int index, EM_IdDefinition activityId,
            float startHour, float endHour, float tickInterval, AnimationCurve curve, bool tradeCapable, EM_IdDefinition[] tickSignalIds)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
            entryProperty.FindPropertyRelative("activityIdDefinition").objectReferenceValue = activityId;
            entryProperty.FindPropertyRelative("activityId").stringValue = activityId != null ? activityId.Id : string.Empty;
            entryProperty.FindPropertyRelative("locationDefinition").objectReferenceValue = null;
            entryProperty.FindPropertyRelative("tradeCapable").boolValue = tradeCapable;
            entryProperty.FindPropertyRelative("startHour").floatValue = startHour;
            entryProperty.FindPropertyRelative("endHour").floatValue = endHour;
            entryProperty.FindPropertyRelative("useDuration").boolValue = false;
            entryProperty.FindPropertyRelative("minDurationHours").floatValue = 0f;
            entryProperty.FindPropertyRelative("maxDurationHours").floatValue = 0f;
            SerializedProperty signalsProperty = entryProperty.FindPropertyRelative("signalEntries");

            if (signalsProperty != null && signalsProperty.isArray)
            {
                int signalCount = tickSignalIds != null ? tickSignalIds.Length : 0;
                signalsProperty.arraySize = signalCount;

                for (int i = 0; i < signalCount; i++)
                {
                    SerializedProperty signalProperty = signalsProperty.GetArrayElementAtIndex(i);
                    signalProperty.FindPropertyRelative("startSignalIdDefinition").objectReferenceValue = null;
                    signalProperty.FindPropertyRelative("startSignalId").stringValue = string.Empty;

                    EM_IdDefinition tickSignalId = tickSignalIds[i];
                    signalProperty.FindPropertyRelative("tickSignalIdDefinition").objectReferenceValue = tickSignalId;
                    signalProperty.FindPropertyRelative("tickSignalId").stringValue = tickSignalId != null ? tickSignalId.Id : string.Empty;
                    signalProperty.FindPropertyRelative("tickIntervalHours").floatValue = tickInterval;
                    signalProperty.FindPropertyRelative("tickSignalCurve").animationCurveValue = curve;
                }
            }
        }
        #endregion
    }
}
