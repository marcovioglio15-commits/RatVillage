using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Utility helpers for example asset creation.
    /// </summary>
    internal static partial class EmergenceExampleAssetsCreator
    {
        #region Types
        /// <summary>
        /// Editor-only rule entry data used to populate serialized rule sets.
        /// </summary>
        private struct ExampleRuleEntry
        {
            public EM_SignalDefinition Signal;
            public EM_EffectDefinition Effect;
            public int Priority;
            public float Weight;
            public float MinimumSignalValue;
            public float CooldownSeconds;
        }
        #endregion

        #region Helpers
        private static void ApplyRuleEntry(SerializedProperty entryProperty, ExampleRuleEntry entry)
        {
            entryProperty.FindPropertyRelative("signal").objectReferenceValue = entry.Signal;
            entryProperty.FindPropertyRelative("effect").objectReferenceValue = entry.Effect;
            entryProperty.FindPropertyRelative("priority").intValue = entry.Priority;
            entryProperty.FindPropertyRelative("weight").floatValue = entry.Weight;
            entryProperty.FindPropertyRelative("minimumSignalValue").floatValue = entry.MinimumSignalValue;
            entryProperty.FindPropertyRelative("cooldownSeconds").floatValue = entry.CooldownSeconds;
        }

        private static void AddToLibraryIfMissing(EM_MechanicLibrary library, Object asset, string propertyName)
        {
            if (library == null || asset == null)
                return;

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty array = serialized.FindProperty(propertyName);

            if (array == null)
                return;

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                    return;
            }

            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            array.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            serialized.ApplyModifiedProperties();
        }

        private static string GetCategoryFolder(string category)
        {
            return RootFolder + "/" + category;
        }

        private static string GetLibraryFolder()
        {
            return RootFolder + "/Libraries";
        }

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
        #endregion
    }
}
