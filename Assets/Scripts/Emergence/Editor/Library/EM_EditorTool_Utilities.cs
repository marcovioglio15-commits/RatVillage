using System;
using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static class EM_EditorTool_Utilities
    {
        #region Fields

        #region Constants
        private const string RootFolder = "Assets/Scriptable Objects";
        private const string LibraryFolderName = "Libraries";
        #endregion

        #endregion

        #region Methods

        #region Public Properties
        public static EM_MechanicLibrary CreateLibraryAsset()
        {
            return CreateLibraryAsset(RootFolder);
        }

        public static EM_MechanicLibrary CreateLibraryAsset(string rootFolder)
        {
            string libraryFolder = GetLibraryFolderPath(rootFolder);
            EnsureFolderExists(libraryFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(libraryFolder + "/EM_MechanicLibrary.asset");
            EM_MechanicLibrary library = ScriptableObject.CreateInstance<EM_MechanicLibrary>();

            AssetDatabase.CreateAsset(library, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return library;
        }

        public static UnityEngine.Object CreateDefinitionAsset(EM_Categories category)
        {
            return CreateDefinitionAsset(category, RootFolder);
        }

        public static UnityEngine.Object CreateDefinitionAsset(EM_Categories category, string rootFolder)
        {
            string categoryFolder = GetCategoryFolderPath(category, rootFolder);
            EnsureFolderExists(categoryFolder);

            Type assetType = GetAssetType(category);

            if (assetType == null)
                return null;

            string fileName = GetDefaultFileName(category);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(categoryFolder + "/" + fileName);
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType) as ScriptableObject;

            if (asset == null)
                return null;

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        /// <summary>
        /// Adds the provided asset to the specified library category.
        /// </summary>
        public static void AddToLibrary(EM_MechanicLibrary library, UnityEngine.Object asset, EM_Categories category)
        {
            if (library == null || asset == null)
                return;

            SerializedObject serializedLibrary = new SerializedObject(library);
            SerializedProperty listProperty = serializedLibrary.FindProperty(GetListPropertyName(category));

            if (listProperty == null)
                return;

            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            listProperty.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            serializedLibrary.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Helpers
        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string GetLibraryFolderPath(string rootFolder)
        {
            string resolvedRoot = ResolveRootFolder(rootFolder);
            return resolvedRoot + "/" + LibraryFolderName;
        }

        private static string GetCategoryFolderPath(EM_Categories category, string rootFolder)
        {
            string resolvedRoot = ResolveRootFolder(rootFolder);

            if (category == EM_Categories.Signals)
                return resolvedRoot + "/Signals";

            if (category == EM_Categories.RuleSets)
                return resolvedRoot + "/RuleSets";

            if (category == EM_Categories.Effects)
                return resolvedRoot + "/Effects";

            if (category == EM_Categories.Metrics)
                return resolvedRoot + "/Metrics";

            if (category == EM_Categories.Domains)
                return resolvedRoot + "/Domains";

            if (category == EM_Categories.Profiles)
                return resolvedRoot + "/Profiles";

            return resolvedRoot;
        }

        private static Type GetAssetType(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return typeof(EM_SignalDefinition);

            if (category == EM_Categories.RuleSets)
                return typeof(EM_RuleSetDefinition);

            if (category == EM_Categories.Effects)
                return typeof(EM_EffectDefinition);

            if (category == EM_Categories.Metrics)
                return typeof(EM_MetricDefinition);

            if (category == EM_Categories.Domains)
                return typeof(EM_DomainDefinition);

            if (category == EM_Categories.Profiles)
                return typeof(EM_SocietyProfile);

            return null;
        }

        private static string GetDefaultFileName(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return "EM_SignalDefinition.asset";

            if (category == EM_Categories.RuleSets)
                return "EM_RuleSetDefinition.asset";

            if (category == EM_Categories.Effects)
                return "EM_EffectDefinition.asset";

            if (category == EM_Categories.Metrics)
                return "EM_MetricDefinition.asset";

            if (category == EM_Categories.Domains)
                return "EM_DomainDefinition.asset";

            if (category == EM_Categories.Profiles)
                return "EM_SocietyProfile.asset";

            return "EM_Asset.asset";
        }

        private static string GetListPropertyName(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return "signals";

            if (category == EM_Categories.RuleSets)
                return "ruleSets";

            if (category == EM_Categories.Effects)
                return "effects";

            if (category == EM_Categories.Metrics)
                return "metrics";

            if (category == EM_Categories.Domains)
                return "domains";

            if (category == EM_Categories.Profiles)
                return "profiles";

            return string.Empty;
        }

        private static string ResolveRootFolder(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                return RootFolder;

            return rootFolder;
        }
        #endregion

        #endregion
    }
}
