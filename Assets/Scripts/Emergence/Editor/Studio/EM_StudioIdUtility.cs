using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static class EM_StudioIdUtility
    {
        #region Constants
        private const string AssetPrefix = "EM_Id_";
        #endregion

        #region Lookup
        public static Dictionary<string, EM_IdDefinition> BuildIdLookup(string rootFolder)
        {
            List<EM_IdDefinition> definitions = EM_StudioAssetUtility.FindAssets<EM_IdDefinition>(rootFolder);
            Dictionary<string, EM_IdDefinition> lookup = new Dictionary<string, EM_IdDefinition>();

            for (int i = 0; i < definitions.Count; i++)
            {
                EM_IdDefinition definition = definitions[i];

                if (definition == null)
                    continue;

                string key = BuildKey(definition.Category, definition.Id);

                if (lookup.ContainsKey(key))
                    continue;

                lookup.Add(key, definition);
            }

            return lookup;
        }

        public static EM_IdDefinition FindOrCreateId(string rootFolder, EM_IdCategory category, string id, string description,
            Dictionary<string, EM_IdDefinition> lookup)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            string key = BuildKey(category, id);

            if (lookup != null && lookup.TryGetValue(key, out EM_IdDefinition existing))
                return existing;

            EM_IdDefinition created = CreateIdAsset(rootFolder, category, id, description);

            if (created != null && lookup != null)
                lookup[key] = created;

            return created;
        }
        #endregion

        #region Helpers
        public static string BuildKey(EM_IdCategory category, string id)
        {
            return category + "|" + id;
        }

        public static string SanitizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            char[] buffer = id.Trim().ToCharArray();

            for (int i = 0; i < buffer.Length; i++)
            {
                char value = buffer[i];

                if (char.IsLetterOrDigit(value) || value == '.' || value == '_' || value == '-')
                    continue;

                buffer[i] = '_';
            }

            return new string(buffer);
        }

        private static EM_IdDefinition CreateIdAsset(string rootFolder, EM_IdCategory category, string id, string description)
        {
            string folder = EM_StudioAssetUtility.GetIdFolder(rootFolder, category);
            string safeId = SanitizeId(id);
            string assetName = AssetPrefix + category + "_" + (string.IsNullOrWhiteSpace(safeId) ? "Id" : safeId);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            EM_IdDefinition asset = ScriptableObject.CreateInstance<EM_IdDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty categoryProperty = serialized.FindProperty("category");
            SerializedProperty idProperty = serialized.FindProperty("id");
            SerializedProperty descriptionProperty = serialized.FindProperty("description");

            if (categoryProperty != null)
                categoryProperty.enumValueIndex = (int)category;

            if (idProperty != null)
                idProperty.stringValue = id;

            if (descriptionProperty != null)
                descriptionProperty.stringValue = description ?? string.Empty;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            return asset;
        }
        #endregion
    }
}
