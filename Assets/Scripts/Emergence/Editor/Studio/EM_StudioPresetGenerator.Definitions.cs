using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Definitions
        private static EM_SignalDefinition CreateSignal(string rootFolder, string assetName, EM_IdDefinition idDefinition, string legacyId,
            string description)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Signals, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_SignalDefinition asset = ScriptableObject.CreateInstance<EM_SignalDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("signalIdDefinition").objectReferenceValue = idDefinition;
            serialized.FindProperty("signalId").stringValue = legacyId;
            serialized.FindProperty("description").stringValue = description ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_MetricDefinition CreateMetric(string rootFolder, string assetName, EM_IdDefinition idDefinition, string legacyId,
            EM_SignalDefinition signal, float sampleInterval)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Metrics, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_MetricDefinition asset = ScriptableObject.CreateInstance<EM_MetricDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("metricIdDefinition").objectReferenceValue = idDefinition;
            serialized.FindProperty("metricId").stringValue = legacyId;
            serialized.FindProperty("signal").objectReferenceValue = signal;
            serialized.FindProperty("samplingMode").enumValueIndex = (int)EmergenceMetricSamplingMode.Event;
            serialized.FindProperty("aggregationKind").enumValueIndex = (int)EmergenceMetricAggregation.No_Aggregation;
            serialized.FindProperty("sampleInterval").floatValue = sampleInterval;
            serialized.FindProperty("scope").enumValueIndex = (int)EmergenceMetricScope.Member;
            serialized.FindProperty("normalization").enumValueIndex = (int)EmergenceMetricNormalization.Clamp01;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_EffectDefinition CreateOverrideEffect(string rootFolder, string assetName, EM_IdDefinition effectIdDefinition,
            string legacyId, EM_IdDefinition activityIdDefinition, EM_IdDefinition secondaryIdDefinition, float magnitude)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Effects, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_EffectDefinition asset = ScriptableObject.CreateInstance<EM_EffectDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("effectIdDefinition").objectReferenceValue = effectIdDefinition;
            serialized.FindProperty("effectId").stringValue = legacyId;
            serialized.FindProperty("effectType").enumValueIndex = (int)EmergenceEffectType.OverrideSchedule;
            serialized.FindProperty("target").enumValueIndex = (int)EmergenceEffectTarget.EventTarget;
            serialized.FindProperty("parameterIdDefinition").objectReferenceValue = activityIdDefinition;
            serialized.FindProperty("parameterId").stringValue = activityIdDefinition != null ? activityIdDefinition.Id : string.Empty;
            serialized.FindProperty("secondaryIdDefinition").objectReferenceValue = secondaryIdDefinition;
            serialized.FindProperty("secondaryId").stringValue = secondaryIdDefinition != null ? secondaryIdDefinition.Id : string.Empty;
            serialized.FindProperty("magnitude").floatValue = magnitude;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_EffectDefinition CreateIntentEffect(string rootFolder, string assetName, EM_IdDefinition effectIdDefinition,
            string legacyId, EM_IdDefinition intentIdDefinition, float magnitude)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Effects, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_EffectDefinition asset = ScriptableObject.CreateInstance<EM_EffectDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("effectIdDefinition").objectReferenceValue = effectIdDefinition;
            serialized.FindProperty("effectId").stringValue = legacyId;
            serialized.FindProperty("effectType").enumValueIndex = (int)EmergenceEffectType.AddIntent;
            serialized.FindProperty("target").enumValueIndex = (int)EmergenceEffectTarget.EventTarget;
            serialized.FindProperty("parameterIdDefinition").objectReferenceValue = intentIdDefinition;
            serialized.FindProperty("parameterId").stringValue = intentIdDefinition != null ? intentIdDefinition.Id : string.Empty;
            serialized.FindProperty("secondaryIdDefinition").objectReferenceValue = null;
            serialized.FindProperty("secondaryId").stringValue = string.Empty;
            serialized.FindProperty("magnitude").floatValue = magnitude;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_EffectDefinition CreateRelationshipEffect(string rootFolder, string assetName, EM_IdDefinition effectIdDefinition,
            string legacyId, float magnitude, EmergenceEffectTarget target)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Effects, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_EffectDefinition asset = ScriptableObject.CreateInstance<EM_EffectDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("effectIdDefinition").objectReferenceValue = effectIdDefinition;
            serialized.FindProperty("effectId").stringValue = legacyId;
            serialized.FindProperty("effectType").enumValueIndex = (int)EmergenceEffectType.ModifyRelationship;
            serialized.FindProperty("target").enumValueIndex = (int)target;
            serialized.FindProperty("parameterIdDefinition").objectReferenceValue = null;
            serialized.FindProperty("parameterId").stringValue = string.Empty;
            serialized.FindProperty("secondaryIdDefinition").objectReferenceValue = null;
            serialized.FindProperty("secondaryId").stringValue = string.Empty;
            serialized.FindProperty("magnitude").floatValue = magnitude;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_DomainDefinition CreateDomain(string rootFolder, string assetName, EM_IdDefinition idDefinition, string legacyId,
            EM_RuleSetDefinition[] ruleSets, Color color)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Domains, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_DomainDefinition asset = ScriptableObject.CreateInstance<EM_DomainDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("domainIdDefinition").objectReferenceValue = idDefinition;
            serialized.FindProperty("domainId").stringValue = legacyId;
            serialized.FindProperty("domainColor").colorValue = color;
            SerializedProperty ruleSetsProperty = serialized.FindProperty("ruleSets");
            ruleSetsProperty.arraySize = ruleSets.Length;

            for (int i = 0; i < ruleSets.Length; i++)
                ruleSetsProperty.GetArrayElementAtIndex(i).objectReferenceValue = ruleSets[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_SocietyProfile CreateProfile(string rootFolder, string assetName, EM_IdDefinition idDefinition, string legacyId,
            EM_DomainDefinition[] domains)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.Profiles, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_SocietyProfile asset = ScriptableObject.CreateInstance<EM_SocietyProfile>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("profileIdDefinition").objectReferenceValue = idDefinition;
            serialized.FindProperty("profileId").stringValue = legacyId;
            SerializedProperty domainsProperty = serialized.FindProperty("domains");
            domainsProperty.arraySize = domains.Length;

            for (int i = 0; i < domains.Length; i++)
                domainsProperty.GetArrayElementAtIndex(i).objectReferenceValue = domains[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_MechanicLibrary CreateLibrary(string rootFolder, EM_SignalDefinition[] signals, EM_MetricDefinition[] metrics,
            EM_EffectDefinition[] effects, EM_RuleSetDefinition[] ruleSets, EM_DomainDefinition[] domains, EM_SocietyProfile[] profiles)
        {
            string folder = EM_StudioAssetUtility.GetLibraryFolder(rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + LibraryAssetName + ".asset");
            EM_MechanicLibrary asset = ScriptableObject.CreateInstance<EM_MechanicLibrary>();
            SerializedObject serialized = new SerializedObject(asset);
            SetArray(serialized.FindProperty("signals"), signals);
            SetArray(serialized.FindProperty("metrics"), metrics);
            SetArray(serialized.FindProperty("effects"), effects);
            SetArray(serialized.FindProperty("ruleSets"), ruleSets);
            SetArray(serialized.FindProperty("domains"), domains);
            SetArray(serialized.FindProperty("profiles"), profiles);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        #endregion
    }
}
