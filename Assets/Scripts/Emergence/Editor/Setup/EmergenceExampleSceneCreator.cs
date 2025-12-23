using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;

namespace Emergence
{
    /// <summary>
    /// Creates an example scene and subscene for the Emergence village setup.
    /// </summary>
    internal static partial class EmergenceExampleSceneCreator
    {
        #region Constants
        private const string MenuPath = "Tools/Emergence/Create Example Village Scene";
        private const string SceneFolder = "Assets/Scenes/Emergence";
        private const string MainScenePath = "Assets/Scenes/Emergence/EM_ExampleVillage.unity";
        private const string SubScenePath = "Assets/Scenes/Emergence/EM_ExampleVillage_SubScene.unity";
        private const string SubSceneObjectName = "EM_ExampleVillage_SubScene";
        private const string LibraryObjectName = "EM_Library";
        private const string SocietyObjectName = "EM_SocietyRoot";
        private const string SpawnerObjectName = "EM_NpcSpawner";
        private const string DebugUiObjectName = "EM_DebugUI";
        private const string DebugTimeLabelName = "EM_DebugTimeLabel";
        private const string DebugLogLabelName = "EM_DebugLogLabel";
        private const string ExampleProfileId = "Society.ExampleVillage";
        private const string ExampleNpcPrefabPath = "Assets/Prefabs/Emergence/EM_Prefab_NpcExample.prefab";
        #endregion

        #region Menu
        /// <summary>
        /// Creates an example main scene and subscene with configured authoring objects.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void CreateExampleVillageScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EmergenceExampleAssetsCreator.CreateExampleAssets();
            EnsureFolderExists(SceneFolder);

            EM_MechanicLibrary library = FindLibrary();
            EM_SocietyProfile profile = FindProfile(ExampleProfileId);
            GameObject npcPrefab = FindNpcPrefab();
            EM_DebugMessageTemplates templates = FindOrCreateDebugTemplates();

            if (library == null || profile == null || npcPrefab == null)
            {
                EditorUtility.DisplayDialog("Emergence Studio",
                    "Example assets were not found. Run 'Create Example Village Assets' first.",
                    "Ok");
                return;
            }

            Scene mainScene = OpenOrCreateScene(MainScenePath, NewSceneMode.Single, NewSceneSetup.DefaultGameObjects);
            Scene subScene = OpenOrCreateScene(SubScenePath, NewSceneMode.Additive, NewSceneSetup.EmptyScene);

            SceneAsset subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);

            if (subSceneAsset == null)
            {
                EditorUtility.DisplayDialog("Emergence Studio", "Failed to create the SubScene asset.", "Ok");
                return;
            }

            EnsureMainSceneView(mainScene);
            EnsureDebugUi(mainScene, templates);
            SubScene subSceneComponent = GetOrCreateSubSceneComponent(mainScene, subSceneAsset);

            if (subSceneComponent == null)
                return;

            PopulateSubScene(subScene, library, profile, npcPrefab);

            EditorSceneManager.SaveScene(subScene);
            EditorSceneManager.SaveScene(mainScene);
            EditorSceneManager.SetActiveScene(mainScene);
            Selection.activeObject = subSceneComponent.gameObject;
        }
        #endregion
    }
}
