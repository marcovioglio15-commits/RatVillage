using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Asset lookup helpers for the example village setup.
    /// </summary>
    internal static partial class EmergenceExampleSceneCreator
    {
        #region Asset Lookup
        private static EM_MechanicLibrary FindLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_MechanicLibrary");

            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<EM_MechanicLibrary>(path);
        }

        private static EM_SocietyProfile FindProfile(string profileId)
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_SocietyProfile");

            if (guids == null || guids.Length == 0)
                return null;

            EM_SocietyProfile fallback = null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EM_SocietyProfile profile = AssetDatabase.LoadAssetAtPath<EM_SocietyProfile>(path);

                if (profile == null)
                    continue;

                if (fallback == null)
                    fallback = profile;

                if (profile.ProfileId == profileId)
                    return profile;
            }

            return fallback;
        }

        private static GameObject FindNpcPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExampleNpcPrefabPath);

            if (prefab != null)
                return prefab;

            string[] guids = AssetDatabase.FindAssets("EM_Prefab_NpcExample t:GameObject");

            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static EM_DebugMessageTemplates FindOrCreateDebugTemplates()
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_DebugMessageTemplates");

            if (guids != null && guids.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                EM_DebugMessageTemplates existing = AssetDatabase.LoadAssetAtPath<EM_DebugMessageTemplates>(existingPath);
                EnsureDebugTemplateDefaults(existing);
                return existing;
            }

            string folder = "Assets/Scriptable Objects/Debug";
            EnsureFolderExists(folder);

            string assetPath = folder + "/EM_DebugMessageTemplates.asset";
            EM_DebugMessageTemplates templates = AssetDatabase.LoadAssetAtPath<EM_DebugMessageTemplates>(assetPath);

            if (templates != null)
                return templates;

            templates = ScriptableObject.CreateInstance<EM_DebugMessageTemplates>();
            AssetDatabase.CreateAsset(templates, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureDebugTemplateDefaults(templates);
            return templates;
        }

        private static void EnsureDebugTemplateDefaults(EM_DebugMessageTemplates templates)
        {
            if (templates == null)
                return;

            SerializedObject serialized = new SerializedObject(templates);
            UpdateTemplateField(serialized.FindProperty("timeLabelTemplate"), "Time of Day: {time}", "Time of Day: {time}");
            UpdateTemplateField(serialized.FindProperty("scheduleWindowTemplate"),
                "[{time}] Society {society} entered {window} window.",
                "[{time}] Society {society} entered {window} schedule window; window-driven rules can now activate.");
            UpdateTemplateField(serialized.FindProperty("scheduleTickTemplate"),
                "[{time}] Society {society} {window} tick value {value}.",
                "[{time}] Society {society} emitted {window} schedule tick (value {value}).");
            UpdateTemplateField(serialized.FindProperty("tradeAttemptTemplate"),
                "[{time}] Villager {subject} seeks {resource} for {need} from Villager {target}.",
                "[{time}] {subject} paused current activity to seek {resource} for {need} from {target} (chance {value}).");
            UpdateTemplateField(serialized.FindProperty("tradeAttemptTemplate"),
                "[{time}] Villager {subject} paused current activity to seek {resource} for {need} from Villager {target} (chance {value}).",
                "[{time}] {subject} paused current activity to seek {resource} for {need} from {target} (chance {value}).");
            UpdateTemplateField(serialized.FindProperty("tradeSuccessTemplate"),
                "[{time}] Villager {subject} obtained {resource} from Villager {target} for {need} (amount {value}).",
                "[{time}] {subject} obtained {resource} from {target} to satisfy {need} (amount {value}).");
            UpdateTemplateField(serialized.FindProperty("tradeSuccessTemplate"),
                "[{time}] Villager {subject} obtained {resource} from Villager {target} to satisfy {need} (amount {value}).",
                "[{time}] {subject} obtained {resource} from {target} to satisfy {need} (amount {value}).");
            UpdateTemplateField(serialized.FindProperty("tradeFailTemplate"),
                "[{time}] Villager {subject} failed to obtain {resource} for {need} (reason: {reason}).",
                "[{time}] {subject} could not obtain {resource} for {need} (reason: {reason}).");
            UpdateTemplateField(serialized.FindProperty("tradeFailTemplate"),
                "[{time}] Villager {subject} could not obtain {resource} for {need} (reason: {reason}).",
                "[{time}] {subject} could not obtain {resource} for {need} (reason: {reason}).");
            UpdateTemplateField(serialized.FindProperty("distributionTemplate"),
                "[{time}] Society {society} distributed {resource} to Villager {subject} for {need} (amount {value}).",
                "[{time}] Society {society} distributed {resource} to {subject} to satisfy {need} (amount {value}).");
            UpdateTemplateField(serialized.FindProperty("distributionTemplate"),
                "[{time}] Society {society} distributed {resource} to Villager {subject} to satisfy {need} (amount {value}).",
                "[{time}] Society {society} distributed {resource} to {subject} to satisfy {need} (amount {value}).");
            serialized.ApplyModifiedProperties();
        }

        private static void UpdateTemplateField(SerializedProperty property, string oldValue, string newValue)
        {
            if (property == null)
                return;

            string current = property.stringValue;

            if (string.IsNullOrEmpty(current))
            {
                property.stringValue = newValue;
                return;
            }

            if (current == oldValue)
                property.stringValue = newValue;
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
