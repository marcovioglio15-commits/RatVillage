using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Sync Helpers
        private void SyncCategory(EM_Categories category)
        {
            if (library == null)
                return;

            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty listProperty = serialized.FindProperty(GetListPropertyName(category));

            if (listProperty == null)
                return;

            List<Object> assets = FindAssetsForCategory(category);
            HashSet<Object> current = new HashSet<Object>();

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                Object existing = listProperty.GetArrayElementAtIndex(i).objectReferenceValue;

                if (existing == null)
                    continue;

                current.Add(existing);
            }

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null)
                    continue;

                if (current.Contains(assets[i]))
                    continue;

                int index = listProperty.arraySize;
                listProperty.InsertArrayElementAtIndex(index);
                listProperty.GetArrayElementAtIndex(index).objectReferenceValue = assets[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private List<Object> FindAssetsForCategory(EM_Categories category)
        {
            List<Object> results = new List<Object>();

            if (category == EM_Categories.Signals)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_SignalDefinition>(rootFolder));
            else if (category == EM_Categories.Metrics)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_MetricDefinition>(rootFolder));
            else if (category == EM_Categories.Effects)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_EffectDefinition>(rootFolder));
            else if (category == EM_Categories.RuleSets)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_RuleSetDefinition>(rootFolder));
            else if (category == EM_Categories.Domains)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_DomainDefinition>(rootFolder));
            else if (category == EM_Categories.Profiles)
                AddAssets(results, EM_StudioAssetUtility.FindAssets<EM_SocietyProfile>(rootFolder));

            return results;
        }

        private void AddAssets<T>(List<Object> results, List<T> assets) where T : Object
        {
            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null)
                    continue;

                results.Add(assets[i]);
            }
        }

        private void SelectItem(Object asset)
        {
            if (listView == null || asset == null)
                return;

            int index = items.IndexOf(asset);

            if (index < 0)
                return;

            listView.SetSelection(index);
            listView.ScrollToItem(index);
        }

        private void SelectIdItem(EM_IdDefinition asset)
        {
            if (idListView == null || asset == null)
                return;

            int index = idItems.IndexOf(asset);

            if (index < 0)
                return;

            idListView.SetSelection(index);
            idListView.ScrollToItem(index);
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
        #endregion
    }
}
