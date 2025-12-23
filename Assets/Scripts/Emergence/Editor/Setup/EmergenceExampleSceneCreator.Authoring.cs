using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emergence
{
    /// <summary>
    /// Authoring setup helpers for the example village subscene.
    /// </summary>
    internal static partial class EmergenceExampleSceneCreator
    {
        #region Authoring Setup
        private static void PopulateSubScene(Scene subScene, EM_MechanicLibrary library, EM_SocietyProfile profile, GameObject npcPrefab)
        {
            GameObject libraryObject = GetOrCreateRootObject(subScene, LibraryObjectName);
            EmergenceLibraryAuthoring libraryAuthoring = GetOrAddComponent<EmergenceLibraryAuthoring>(libraryObject);
            ConfigureLibraryAuthoring(libraryAuthoring, library, profile);

            GameObject societyObject = GetOrCreateRootObject(subScene, SocietyObjectName);
            EmergenceSocietyProfileAuthoring profileAuthoring = GetOrAddComponent<EmergenceSocietyProfileAuthoring>(societyObject);
            ConfigureSocietyProfileAuthoring(profileAuthoring, library, profile);

            EmergenceSocietySimulationAuthoring simulationAuthoring = GetOrAddComponent<EmergenceSocietySimulationAuthoring>(societyObject);
            ConfigureSimulationAuthoring(simulationAuthoring);

            GameObject spawnerObject = GetOrCreateRootObject(subScene, SpawnerObjectName);
            EmergenceNpcSpawnerAuthoring spawnerAuthoring = GetOrAddComponent<EmergenceNpcSpawnerAuthoring>(spawnerObject);
            ConfigureSpawnerAuthoring(spawnerAuthoring, npcPrefab, societyObject);
        }

        private static GameObject GetOrCreateRootObject(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != name)
                    continue;

                return roots[i];
            }

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);

            return created;
        }

        private static TComponent GetOrAddComponent<TComponent>(GameObject target) where TComponent : Component
        {
            TComponent component = target.GetComponent<TComponent>();

            if (component != null)
                return component;

            return target.AddComponent<TComponent>();
        }

        private static void ConfigureLibraryAuthoring(EmergenceLibraryAuthoring authoring, EM_MechanicLibrary library, EM_SocietyProfile profile)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            serialized.FindProperty("library").objectReferenceValue = library;
            serialized.FindProperty("defaultProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureSocietyProfileAuthoring(EmergenceSocietyProfileAuthoring authoring, EM_MechanicLibrary library, EM_SocietyProfile profile)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            serialized.FindProperty("library").objectReferenceValue = library;
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.FindProperty("initialLod").enumValueIndex = (int)EmergenceLodTier.Full;
            serialized.FindProperty("debugName").stringValue = profile.DisplayName;
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureSimulationAuthoring(EmergenceSocietySimulationAuthoring authoring)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            SerializedProperty resources = serialized.FindProperty("societyResources");
            resources.arraySize = 3;
            ApplyResourceEntry(resources.GetArrayElementAtIndex(0), "Resource.Food", 50f);
            ApplyResourceEntry(resources.GetArrayElementAtIndex(1), "Resource.Water", 50f);
            ApplyResourceEntry(resources.GetArrayElementAtIndex(2), "Resource.Sleep", 50f);
            serialized.ApplyModifiedProperties();
        }

        private static void ConfigureSpawnerAuthoring(EmergenceNpcSpawnerAuthoring authoring, GameObject npcPrefab, GameObject societyRoot)
        {
            SerializedObject serialized = new SerializedObject(authoring);
            serialized.FindProperty("npcPrefab").objectReferenceValue = npcPrefab;
            serialized.FindProperty("societyRoot").objectReferenceValue = societyRoot;
            serialized.FindProperty("count").intValue = 12;
            serialized.FindProperty("radius").floatValue = 12f;
            serialized.FindProperty("height").floatValue = 0f;
            serialized.FindProperty("randomSeed").longValue = 0L;
            serialized.ApplyModifiedProperties();
        }

        private static void ApplyResourceEntry(SerializedProperty property, string resourceId, float amount)
        {
            property.FindPropertyRelative("ResourceId").stringValue = resourceId;
            property.FindPropertyRelative("Amount").floatValue = amount;
        }
        #endregion
    }
}
