using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static class EM_StudioAssetUtility
    {
        #region Constants
        public const string DefaultRootFolder = "Assets/Scriptable Objects";
        private const string IdFolderName = "Ids";
        private const string ScheduleFolder = "Village/NPC/Schedule Preset";
        #endregion

        #region Root Folder
        public static string ResolveRootFolder(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
                return DefaultRootFolder;

            return rootFolder;
        }

        public static bool TryMakeRelativePath(string absolutePath, out string relativePath)
        {
            relativePath = string.Empty;

            if (string.IsNullOrWhiteSpace(absolutePath))
                return false;

            string dataPath = Application.dataPath.Replace("\\", "/");
            string normalizedAbsolute = absolutePath.Replace("\\", "/");

            if (!normalizedAbsolute.StartsWith(dataPath))
                return false;

            string suffix = normalizedAbsolute.Substring(dataPath.Length);

            if (suffix.StartsWith("/"))
                suffix = suffix.Substring(1);

            relativePath = "Assets" + (string.IsNullOrWhiteSpace(suffix) ? string.Empty : "/" + suffix);
            return true;
        }
        #endregion

        #region Folder Helpers
        public static void EnsureFolderExists(string path)
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

        public static string GetIdFolder(string rootFolder, EM_IdCategory category)
        {
            string resolvedRoot = ResolveRootFolder(rootFolder);
            string folder = resolvedRoot + "/" + IdFolderName + "/" + category;
            EnsureFolderExists(folder);
            return folder;
        }

        public static string GetCategoryFolder(EM_Categories category, string rootFolder)
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

        public static string GetLibraryFolder(string rootFolder)
        {
            string resolvedRoot = ResolveRootFolder(rootFolder);
            return resolvedRoot + "/Libraries";
        }

        public static string GetScheduleFolder(string rootFolder)
        {
            string resolvedRoot = ResolveRootFolder(rootFolder);
            return resolvedRoot + "/" + ScheduleFolder;
        }
        #endregion

        #region Asset Queries
        public static List<T> FindAssets<T>(string rootFolder) where T : Object
        {
            List<T> results = new List<T>();
            string resolvedRoot = ResolveRootFolder(rootFolder);
            string filter = "t:" + typeof(T).Name;
            string[] paths = new string[] { resolvedRoot };
            string[] guids = AssetDatabase.FindAssets(filter, paths);

            if (guids == null || guids.Length == 0)
                return results;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset == null)
                    continue;

                results.Add(asset);
            }

            return results;
        }
        #endregion
    }
}
