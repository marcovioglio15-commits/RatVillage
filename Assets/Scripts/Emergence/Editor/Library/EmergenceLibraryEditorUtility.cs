using System;
using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Provides helper methods for creating and managing emergence assets.
    /// </summary>
    internal static class EmergenceLibraryEditorUtility
    {
        #region Constants
        private const string RootFolder = "Assets/Scriptable Objects";
        private const string LibraryFolderName = "Libraries";
        #endregion

        #region Public
        /// <summary>
        /// Creates a new mechanic library asset in the default folder.
        /// </summary>
        public static EM_MechanicLibrary CreateLibraryAsset()
        {
            string libraryFolder = GetLibraryFolderPath();
            EnsureFolderExists(libraryFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(libraryFolder + "/EM_MechanicLibrary.asset");
            EM_MechanicLibrary library = ScriptableObject.CreateInstance<EM_MechanicLibrary>();

            AssetDatabase.CreateAsset(library, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return library;
        }

        /// <summary>
        /// Creates a new definition asset for the selected category.
        /// </summary>
        public static UnityEngine.Object CreateDefinitionAsset(EmergenceLibraryCategory category)
        {
            string categoryFolder = GetCategoryFolderPath(category);
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
        public static void AddToLibrary(EM_MechanicLibrary library, UnityEngine.Object asset, EmergenceLibraryCategory category)
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

        private static string GetLibraryFolderPath()
        {
            return RootFolder + "/" + LibraryFolderName;
        }

        private static string GetCategoryFolderPath(EmergenceLibraryCategory category)
        {
            if (category == EmergenceLibraryCategory.Signals)
                return RootFolder + "/Signals";

            if (category == EmergenceLibraryCategory.RuleSets)
                return RootFolder + "/RuleSets";

            if (category == EmergenceLibraryCategory.Effects)
                return RootFolder + "/Effects";

            if (category == EmergenceLibraryCategory.Metrics)
                return RootFolder + "/Metrics";

            if (category == EmergenceLibraryCategory.Norms)
                return RootFolder + "/Norms";

            if (category == EmergenceLibraryCategory.Institutions)
                return RootFolder + "/Institutions";

            if (category == EmergenceLibraryCategory.Domains)
                return RootFolder + "/Domains";

            if (category == EmergenceLibraryCategory.Profiles)
                return RootFolder + "/Profiles";

            return RootFolder;
        }

        private static Type GetAssetType(EmergenceLibraryCategory category)
        {
            if (category == EmergenceLibraryCategory.Signals)
                return typeof(EM_SignalDefinition);

            if (category == EmergenceLibraryCategory.RuleSets)
                return typeof(EM_RuleSetDefinition);

            if (category == EmergenceLibraryCategory.Effects)
                return typeof(EM_EffectDefinition);

            if (category == EmergenceLibraryCategory.Metrics)
                return typeof(EM_MetricDefinition);

            if (category == EmergenceLibraryCategory.Norms)
                return typeof(EM_NormDefinition);

            if (category == EmergenceLibraryCategory.Institutions)
                return typeof(EM_InstitutionDefinition);

            if (category == EmergenceLibraryCategory.Domains)
                return typeof(EM_DomainDefinition);

            if (category == EmergenceLibraryCategory.Profiles)
                return typeof(EM_SocietyProfile);

            return null;
        }

        private static string GetDefaultFileName(EmergenceLibraryCategory category)
        {
            if (category == EmergenceLibraryCategory.Signals)
                return "EM_SignalDefinition.asset";

            if (category == EmergenceLibraryCategory.RuleSets)
                return "EM_RuleSetDefinition.asset";

            if (category == EmergenceLibraryCategory.Effects)
                return "EM_EffectDefinition.asset";

            if (category == EmergenceLibraryCategory.Metrics)
                return "EM_MetricDefinition.asset";

            if (category == EmergenceLibraryCategory.Norms)
                return "EM_NormDefinition.asset";

            if (category == EmergenceLibraryCategory.Institutions)
                return "EM_InstitutionDefinition.asset";

            if (category == EmergenceLibraryCategory.Domains)
                return "EM_DomainDefinition.asset";

            if (category == EmergenceLibraryCategory.Profiles)
                return "EM_SocietyProfile.asset";

            return "EM_Asset.asset";
        }

        private static string GetListPropertyName(EmergenceLibraryCategory category)
        {
            if (category == EmergenceLibraryCategory.Signals)
                return "signals";

            if (category == EmergenceLibraryCategory.RuleSets)
                return "ruleSets";

            if (category == EmergenceLibraryCategory.Effects)
                return "effects";

            if (category == EmergenceLibraryCategory.Metrics)
                return "metrics";

            if (category == EmergenceLibraryCategory.Norms)
                return "norms";

            if (category == EmergenceLibraryCategory.Institutions)
                return "institutions";

            if (category == EmergenceLibraryCategory.Domains)
                return "domains";

            if (category == EmergenceLibraryCategory.Profiles)
                return "profiles";

            return string.Empty;
        }
        #endregion
    }
}
