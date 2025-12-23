using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Prefab creation helpers for the example setup.
    /// </summary>
    internal static partial class EmergenceExampleAssetsCreator
    {
        #region Prefabs
        private const string PrefabFolder = "Assets/Prefabs/Emergence";
        private const string NpcPrefabName = "EM_Prefab_NpcExample.prefab";
        private const string SpawnerPrefabName = "EM_Prefab_NpcSpawner.prefab";

        private static void CreateExamplePrefabs()
        {
            EnsureFolderExists(PrefabFolder);

            string npcPath = PrefabFolder + "/" + NpcPrefabName;
            string spawnerPath = PrefabFolder + "/" + SpawnerPrefabName;

            GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(npcPath);

            if (npcPrefab == null)
                npcPrefab = CreateNpcPrefab(npcPath);

            EnsureNpcDebugName(npcPrefab);

            GameObject spawnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spawnerPath);

            if (spawnerPrefab == null)
                CreateSpawnerPrefab(spawnerPath, npcPrefab);
        }

        private static GameObject CreateNpcPrefab(string path)
        {
            GameObject temp = new GameObject("EM_NpcExample");
            EmergenceNpcAuthoring authoring = temp.AddComponent<EmergenceNpcAuthoring>();
            ConfigureNpcAuthoring(authoring);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);

            return prefab;
        }

        private static void CreateSpawnerPrefab(string path, GameObject npcPrefab)
        {
            GameObject temp = new GameObject("EM_NpcSpawner");
            EmergenceNpcSpawnerAuthoring authoring = temp.AddComponent<EmergenceNpcSpawnerAuthoring>();
            ConfigureSpawnerAuthoring(authoring, npcPrefab);

            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
        }

        private static void ConfigureNpcAuthoring(EmergenceNpcAuthoring authoring)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            serialized.FindProperty("societyRoot").objectReferenceValue = null;
            serialized.FindProperty("lodTier").enumValueIndex = (int)EmergenceLodTier.Full;
            serialized.FindProperty("initialReputation").floatValue = 0f;
            serialized.FindProperty("randomSeed").longValue = 0L;
            serialized.FindProperty("debugName").stringValue = "Villager";

            SerializedProperty needs = serialized.FindProperty("needs");
            needs.arraySize = 3;
            ApplyNeedEntry(needs.GetArrayElementAtIndex(0), "Need.Hunger", 0.2f);
            ApplyNeedEntry(needs.GetArrayElementAtIndex(1), "Need.Thirst", 0.1f);
            ApplyNeedEntry(needs.GetArrayElementAtIndex(2), "Need.Sleep", 0.3f);

            SerializedProperty rules = serialized.FindProperty("needRules");
            rules.arraySize = 3;
            ApplyNeedRuleEntry(rules.GetArrayElementAtIndex(0), "Need.Hunger", "Resource.Food", 0.06f, 0.5f, 0.9f, 15f, 0.4f);
            ApplyNeedRuleEntry(rules.GetArrayElementAtIndex(1), "Need.Thirst", "Resource.Water", 0.08f, 0.4f, 0.95f, 12f, 0.5f);
            ApplyNeedRuleEntry(rules.GetArrayElementAtIndex(2), "Need.Sleep", "Resource.Sleep", 0.05f, 0.6f, 0.8f, 20f, 0.6f);

            SerializedProperty resources = serialized.FindProperty("resources");
            resources.arraySize = 3;
            ApplyResourceEntry(resources.GetArrayElementAtIndex(0), "Resource.Food", 0f);
            ApplyResourceEntry(resources.GetArrayElementAtIndex(1), "Resource.Water", 0f);
            ApplyResourceEntry(resources.GetArrayElementAtIndex(2), "Resource.Sleep", 0f);

            SerializedProperty relationships = serialized.FindProperty("relationships");
            relationships.arraySize = 0;
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureSpawnerAuthoring(EmergenceNpcSpawnerAuthoring authoring, GameObject npcPrefab)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            serialized.FindProperty("npcPrefab").objectReferenceValue = npcPrefab;
            serialized.FindProperty("societyRoot").objectReferenceValue = null;
            serialized.FindProperty("count").intValue = 12;
            serialized.FindProperty("radius").floatValue = 12f;
            serialized.FindProperty("height").floatValue = 0f;
            serialized.FindProperty("randomSeed").longValue = 0L;
            serialized.ApplyModifiedProperties();
        }

        private static void EnsureNpcDebugName(GameObject npcPrefab)
        {
            if (npcPrefab == null)
                return;

            EmergenceNpcAuthoring authoring = npcPrefab.GetComponent<EmergenceNpcAuthoring>();

            if (authoring == null)
                return;

            SerializedObject serialized = new SerializedObject(authoring);
            SerializedProperty debugNameProperty = serialized.FindProperty("debugName");

            if (debugNameProperty == null)
                return;

            string current = debugNameProperty.stringValue;

            if (!string.IsNullOrEmpty(current))
                return;

            debugNameProperty.stringValue = "Villager";
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(authoring);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyNeedEntry(SerializedProperty property, string needId, float value)
        {
            property.FindPropertyRelative("NeedId").stringValue = needId;
            property.FindPropertyRelative("Value").floatValue = value;
        }

        private static void ApplyResourceEntry(SerializedProperty property, string resourceId, float amount)
        {
            property.FindPropertyRelative("ResourceId").stringValue = resourceId;
            property.FindPropertyRelative("Amount").floatValue = amount;
        }

        private static void ApplyNeedRuleEntry(SerializedProperty property, string needId, string resourceId, float ratePerHour,
            float startThreshold, float maxProbability, float cooldownSeconds, float satisfactionAmount)
        {
            property.FindPropertyRelative("NeedId").stringValue = needId;
            property.FindPropertyRelative("ResourceId").stringValue = resourceId;
            property.FindPropertyRelative("RatePerHour").floatValue = ratePerHour;
            property.FindPropertyRelative("MinValue").floatValue = 0f;
            property.FindPropertyRelative("MaxValue").floatValue = 1f;
            property.FindPropertyRelative("StartThreshold").floatValue = startThreshold;
            property.FindPropertyRelative("MaxProbability").floatValue = maxProbability;
            property.FindPropertyRelative("ProbabilityExponent").floatValue = 2f;
            property.FindPropertyRelative("CooldownSeconds").floatValue = cooldownSeconds;
            property.FindPropertyRelative("ResourceTransferAmount").floatValue = 1f;
            property.FindPropertyRelative("NeedSatisfactionAmount").floatValue = satisfactionAmount;
        }
        #endregion
    }
}
