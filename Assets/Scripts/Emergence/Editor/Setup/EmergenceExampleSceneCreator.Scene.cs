using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;

namespace Emergence
{
    /// <summary>
    /// Scene creation helpers for the example village setup.
    /// </summary>
    internal static partial class EmergenceExampleSceneCreator
    {
        #region Scene Helpers
        private static Scene OpenOrCreateScene(string path, NewSceneMode mode, NewSceneSetup setup)
        {
            Scene existing = SceneManager.GetSceneByPath(path);

            if (existing.IsValid())
                return existing;

            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            if (asset != null)
                return EditorSceneManager.OpenScene(path, GetOpenMode(mode));

            Scene scene = EditorSceneManager.NewScene(setup, mode);
            EditorSceneManager.SaveScene(scene, path);

            return scene;
        }

        private static SubScene GetOrCreateSubSceneComponent(Scene mainScene, SceneAsset subSceneAsset)
        {
            SubScene[] subScenes = Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None);

            for (int i = 0; i < subScenes.Length; i++)
            {
                if (subScenes[i].gameObject.scene != mainScene)
                    continue;

                if (subScenes[i].SceneAsset != subSceneAsset)
                    continue;

                return subScenes[i];
            }

            GameObject subSceneObject = new GameObject(SubSceneObjectName);
            SceneManager.MoveGameObjectToScene(subSceneObject, mainScene);

            SubScene subScene = subSceneObject.AddComponent<SubScene>();
            subScene.SceneAsset = subSceneAsset;

            return subScene;
        }

        private static void EnsureMainSceneView(Scene mainScene)
        {
            if (!HasComponentInScene<Camera>(mainScene))
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 12f, -12f);
                cameraObject.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
                SceneManager.MoveGameObjectToScene(cameraObject, mainScene);
            }

            if (!HasComponentInScene<Light>(mainScene))
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                SceneManager.MoveGameObjectToScene(lightObject, mainScene);
            }
        }

        private static bool HasComponentInScene<TComponent>(Scene scene) where TComponent : Component
        {
            TComponent[] components = Object.FindObjectsByType<TComponent>(FindObjectsSortMode.None);

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.scene == scene)
                    return true;
            }

            return false;
        }

        private static OpenSceneMode GetOpenMode(NewSceneMode mode)
        {
            if (mode == NewSceneMode.Additive)
                return OpenSceneMode.Additive;

            return OpenSceneMode.Single;
        }
        #endregion
    }
}
