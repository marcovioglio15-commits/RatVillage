using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_StudioIdAssigner
    {
        #region Signal
        private static int AssignSignalIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_SignalDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_SignalDefinition>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_SignalDefinition asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("signalIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("signalId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.Signal, rootFolder,
                    lookup, asset.name, "Signal", true);

                if (!changed)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
        #endregion

        #region Metric
        private static int AssignMetricIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_MetricDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_MetricDefinition>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_MetricDefinition asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("metricIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("metricId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.Metric, rootFolder,
                    lookup, asset.name, "Metric", true);

                if (!changed)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
        #endregion

        #region Effect
        private static int AssignEffectIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_EffectDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_EffectDefinition>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_EffectDefinition asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("effectIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("effectId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.Effect, rootFolder,
                    lookup, asset.name, "Effect", true);

                EmergenceEffectType effectType = GetEffectType(serialized);
                SerializedProperty parameterDefinitionProperty = serialized.FindProperty("parameterIdDefinition");
                SerializedProperty parameterLegacyProperty = serialized.FindProperty("parameterId");
                EM_IdCategory parameterCategory = ResolveEffectParameterCategory(effectType);
                bool parameterChanged = AssignIdDefinition(parameterDefinitionProperty, parameterLegacyProperty, parameterCategory, rootFolder,
                    lookup, asset.name + " Parameter", string.Empty, false);

                SerializedProperty secondaryDefinitionProperty = serialized.FindProperty("secondaryIdDefinition");
                SerializedProperty secondaryLegacyProperty = serialized.FindProperty("secondaryId");
                EM_IdCategory secondaryCategory = ResolveEffectSecondaryCategory(effectType);
                bool secondaryChanged = AssignIdDefinition(secondaryDefinitionProperty, secondaryLegacyProperty, secondaryCategory, rootFolder,
                    lookup, asset.name + " Secondary", string.Empty, false);

                if (!changed && !parameterChanged && !secondaryChanged)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
        #endregion

        #region Domains
        private static int AssignDomainIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_DomainDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_DomainDefinition>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_DomainDefinition asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("domainIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("domainId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.Domain, rootFolder,
                    lookup, asset.name, "Domain", true);

                if (!changed)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updated++;
            }

            return updated;
        }
        #endregion

        #region Profiles
        private static int AssignProfileIds(string rootFolder, Dictionary<string, EM_IdDefinition> lookup)
        {
            List<EM_SocietyProfile> assets = EM_StudioAssetUtility.FindAssets<EM_SocietyProfile>(rootFolder);
            int updated = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                EM_SocietyProfile asset = assets[i];

                if (asset == null)
                    continue;

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty idDefinitionProperty = serialized.FindProperty("profileIdDefinition");
                SerializedProperty legacyProperty = serialized.FindProperty("profileId");
                bool changed = AssignIdDefinition(idDefinitionProperty, legacyProperty, EM_IdCategory.Profile, rootFolder,
                    lookup, asset.name, "Society", true);

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
