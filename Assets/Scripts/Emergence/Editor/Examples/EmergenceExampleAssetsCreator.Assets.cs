using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Asset creation helpers for the example setup.
    /// </summary>
    internal static partial class EmergenceExampleAssetsCreator
    {
        #region Asset Creation
        private static EM_MechanicLibrary FindOrCreateLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_MechanicLibrary");

            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<EM_MechanicLibrary>(path);
            }

            string folder = GetLibraryFolder();
            EnsureFolderExists(folder);

            string libraryPath = folder + "/EM_MechanicLibrary.asset";
            EM_MechanicLibrary library = AssetDatabase.LoadAssetAtPath<EM_MechanicLibrary>(libraryPath);

            if (library != null)
                return library;

            library = ScriptableObject.CreateInstance<EM_MechanicLibrary>();
            AssetDatabase.CreateAsset(library, libraryPath);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static EM_DomainDefinition CreateOrLoadDomain(string fileName, string id, string displayName, Color color)
        {
            string folder = GetCategoryFolder("Domains");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_DomainDefinition domain = AssetDatabase.LoadAssetAtPath<EM_DomainDefinition>(path);

            if (domain == null)
            {
                domain = ScriptableObject.CreateInstance<EM_DomainDefinition>();
                AssetDatabase.CreateAsset(domain, path);
            }

            SerializedObject serialized = new SerializedObject(domain);
            serialized.FindProperty("domainId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("domainColor").colorValue = color;
            serialized.FindProperty("isEnabled").boolValue = true;
            serialized.FindProperty("updateWeight").floatValue = 1f;
            serialized.ApplyModifiedProperties();

            return domain;
        }

        private static EM_SignalDefinition CreateOrLoadSignal(string fileName, string id, string displayName, EM_DomainDefinition domain)
        {
            string folder = GetCategoryFolder("Signals");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_SignalDefinition signal = AssetDatabase.LoadAssetAtPath<EM_SignalDefinition>(path);

            if (signal == null)
            {
                signal = ScriptableObject.CreateInstance<EM_SignalDefinition>();
                AssetDatabase.CreateAsset(signal, path);
            }

            SerializedObject serialized = new SerializedObject(signal);
            serialized.FindProperty("signalId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("domain").objectReferenceValue = domain;
            serialized.FindProperty("minimumLod").enumValueIndex = (int)EmergenceLodTier.Full;
            serialized.FindProperty("defaultWeight").floatValue = 1f;
            serialized.ApplyModifiedProperties();

            return signal;
        }

        private static EM_EffectDefinition CreateOrLoadEffect(string fileName, string id, string displayName, EmergenceEffectType effectType,
            EmergenceEffectTarget target, string parameterId, float magnitude)
        {
            string folder = GetCategoryFolder("Effects");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_EffectDefinition effect = AssetDatabase.LoadAssetAtPath<EM_EffectDefinition>(path);

            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<EM_EffectDefinition>();
                AssetDatabase.CreateAsset(effect, path);
            }

            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("effectId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("effectType").enumValueIndex = (int)effectType;
            serialized.FindProperty("target").enumValueIndex = (int)target;
            serialized.FindProperty("parameterId").stringValue = parameterId;
            serialized.FindProperty("magnitude").floatValue = magnitude;
            serialized.FindProperty("useClamp").boolValue = false;
            serialized.ApplyModifiedProperties();

            return effect;
        }

        private static EM_RuleSetDefinition CreateOrLoadRuleSet(string fileName, string id, string displayName, EM_DomainDefinition domain,
            ExampleRuleEntry entryA, ExampleRuleEntry entryB, ExampleRuleEntry entryC)
        {
            string folder = GetCategoryFolder("RuleSets");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_RuleSetDefinition ruleSet = AssetDatabase.LoadAssetAtPath<EM_RuleSetDefinition>(path);

            if (ruleSet == null)
            {
                ruleSet = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
                AssetDatabase.CreateAsset(ruleSet, path);
            }

            SerializedObject serialized = new SerializedObject(ruleSet);
            serialized.FindProperty("ruleSetId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("isEnabled").boolValue = true;
            serialized.FindProperty("domain").objectReferenceValue = domain;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 3;
            ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(0), entryA);
            ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(1), entryB);
            ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(2), entryC);
            serialized.ApplyModifiedProperties();

            return ruleSet;
        }

        private static EM_RuleSetDefinition CreateOrLoadRuleSet(string fileName, string id, string displayName, EM_DomainDefinition domain,
            ExampleRuleEntry entryA, ExampleRuleEntry entryB)
        {
            string folder = GetCategoryFolder("RuleSets");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_RuleSetDefinition ruleSet = AssetDatabase.LoadAssetAtPath<EM_RuleSetDefinition>(path);

            if (ruleSet == null)
            {
                ruleSet = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
                AssetDatabase.CreateAsset(ruleSet, path);
            }

            SerializedObject serialized = new SerializedObject(ruleSet);
            serialized.FindProperty("ruleSetId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("isEnabled").boolValue = true;
            serialized.FindProperty("domain").objectReferenceValue = domain;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 2;
            ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(0), entryA);
            ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(1), entryB);
            serialized.ApplyModifiedProperties();

            return ruleSet;
        }

        private static EM_RuleSetDefinition CreateOrLoadRuleSet(string fileName, string id, string displayName, EM_DomainDefinition domain,
            ExampleRuleEntry[] entries)
        {
            string folder = GetCategoryFolder("RuleSets");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_RuleSetDefinition ruleSet = AssetDatabase.LoadAssetAtPath<EM_RuleSetDefinition>(path);

            if (ruleSet == null)
            {
                ruleSet = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
                AssetDatabase.CreateAsset(ruleSet, path);
            }

            SerializedObject serialized = new SerializedObject(ruleSet);
            serialized.FindProperty("ruleSetId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("isEnabled").boolValue = true;
            serialized.FindProperty("domain").objectReferenceValue = domain;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            int entryCount = entries == null ? 0 : entries.Length;
            rulesProperty.arraySize = entryCount;

            for (int i = 0; i < entryCount; i++)
            {
                ApplyRuleEntry(rulesProperty.GetArrayElementAtIndex(i), entries[i]);
            }

            serialized.ApplyModifiedProperties();

            return ruleSet;
        }

        private static EM_MetricDefinition CreateOrLoadMetric(string fileName, string id, string displayName, EmergenceMetricType metricType, float interval)
        {
            string folder = GetCategoryFolder("Metrics");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_MetricDefinition metric = AssetDatabase.LoadAssetAtPath<EM_MetricDefinition>(path);

            if (metric == null)
            {
                metric = ScriptableObject.CreateInstance<EM_MetricDefinition>();
                AssetDatabase.CreateAsset(metric, path);
            }

            SerializedObject serialized = new SerializedObject(metric);
            serialized.FindProperty("metricId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("metricType").enumValueIndex = (int)metricType;
            serialized.FindProperty("sampleInterval").floatValue = interval;
            serialized.ApplyModifiedProperties();

            return metric;
        }

        private static EM_SocietyProfile CreateOrLoadProfile(string fileName, string id, string displayName, EM_DomainDefinition[] domains,
            EM_RuleSetDefinition[] ruleSets, EM_MetricDefinition[] metrics)
        {
            string folder = GetCategoryFolder("Profiles");
            EnsureFolderExists(folder);

            string path = folder + "/" + fileName;
            EM_SocietyProfile profile = AssetDatabase.LoadAssetAtPath<EM_SocietyProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<EM_SocietyProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("volatility").floatValue = 0.5f;
            serialized.FindProperty("shockAbsorption").floatValue = 0.5f;
            serialized.FindProperty("noiseAmplitude").floatValue = 0.1f;
            serialized.FindProperty("crisisThreshold").floatValue = 0.8f;
            serialized.FindProperty("fullSimTickRate").floatValue = 20f;
            serialized.FindProperty("simplifiedSimTickRate").floatValue = 5f;
            serialized.FindProperty("aggregatedSimTickRate").floatValue = 1f;
            serialized.FindProperty("maxSignalQueue").intValue = 2048;
            serialized.FindProperty("regionSize").vector2Value = new Vector2(250f, 250f);

            SerializedProperty domainProperty = serialized.FindProperty("domains");
            domainProperty.arraySize = domains.Length;

            for (int i = 0; i < domains.Length; i++)
            {
                domainProperty.GetArrayElementAtIndex(i).objectReferenceValue = domains[i];
            }

            SerializedProperty ruleProperty = serialized.FindProperty("ruleSets");
            ruleProperty.arraySize = ruleSets.Length;

            for (int i = 0; i < ruleSets.Length; i++)
            {
                ruleProperty.GetArrayElementAtIndex(i).objectReferenceValue = ruleSets[i];
            }

            SerializedProperty metricProperty = serialized.FindProperty("metrics");
            metricProperty.arraySize = metrics.Length;

            for (int i = 0; i < metrics.Length; i++)
            {
                metricProperty.GetArrayElementAtIndex(i).objectReferenceValue = metrics[i];
            }

            serialized.ApplyModifiedProperties();

            return profile;
        }

        private static void ConfigureDomainRuleSets(EM_DomainDefinition domain, EM_RuleSetDefinition ruleSet)
        {
            if (domain == null)
                return;

            SerializedObject serialized = new SerializedObject(domain);
            SerializedProperty ruleSetsProperty = serialized.FindProperty("ruleSets");
            ruleSetsProperty.arraySize = 1;
            ruleSetsProperty.GetArrayElementAtIndex(0).objectReferenceValue = ruleSet;
            serialized.ApplyModifiedProperties();
        }
        #endregion
    }
}
