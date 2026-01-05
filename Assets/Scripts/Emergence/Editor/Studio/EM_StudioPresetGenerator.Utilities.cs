using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Utilities
        private static int DeleteAssetsOfType<T>(string rootFolder) where T : Object
        {
            int deleted = 0;
            string filter = "t:" + typeof(T).Name;
            string[] paths = new string[] { rootFolder };
            string[] guids = AssetDatabase.FindAssets(filter, paths);

            if (guids == null || guids.Length == 0)
                return deleted;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (string.IsNullOrWhiteSpace(path))
                    continue;

                bool removed = AssetDatabase.DeleteAsset(path);

                if (!removed)
                    continue;

                deleted++;
            }

            return deleted;
        }

        private static void SetArray<T>(SerializedProperty arrayProperty, T[] items) where T : Object
        {
            if (arrayProperty == null)
                return;

            arrayProperty.arraySize = items.Length;

            for (int i = 0; i < items.Length; i++)
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
        #endregion
    }
}
