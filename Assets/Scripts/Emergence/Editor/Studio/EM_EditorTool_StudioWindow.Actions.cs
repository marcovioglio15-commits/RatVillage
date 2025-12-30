using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Methods
        #region Events
        private void OnLibraryChanged(ChangeEvent<Object> changeEvent)
        {
            library = changeEvent.newValue as EM_MechanicLibrary;
            RefreshLibrary();
            ShowInspector(library);
        }

        private void OnCategoryChanged(ChangeEvent<System.Enum> changeEvent)
        {
            selectedCategory = (EM_Categories)changeEvent.newValue;
            if (categoryField != null)
                categoryField.tooltip = GetCategoryTooltip(selectedCategory);
            RefreshLibrary();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            UnityEngine.Object selected = listView.selectedItem as UnityEngine.Object;
            ShowInspector(selected != null ? selected : library);
        }
        #endregion

        #region Creation
        private void CreateLibrary()
        {
            EM_MechanicLibrary createdLibrary = EM_EditorTool_Utilities.CreateLibraryAsset();

            if (createdLibrary == null)
                return;

            library = createdLibrary;
            libraryField.value = library;
            RefreshLibrary();
            ShowInspector(library);
        }

        private void CreateAssetForCategory()
        {
            if (library == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Assign or create a library before creating assets.", "Ok");
                return;
            }

            UnityEngine.Object asset = EM_EditorTool_Utilities.CreateDefinitionAsset(selectedCategory);

            if (asset == null)
                return;

            EM_EditorTool_Utilities.AddToLibrary(library, asset, selectedCategory);
            RefreshLibrary();
            SelectItem(asset);
        }
        #endregion

        #region Helpers
        private void RefreshLibrary()
        {
            items.Clear();

            if (library == null)
            {
                UpdateCreateButtonLabel();
                UpdateStatusLabel();
                listView.Rebuild();
                ShowInspector(null);
                return;
            }

            AddItemsForCategory();
            UpdateCreateButtonLabel();
            UpdateStatusLabel();
            listView.Rebuild();

            if (items.Count > 0)
                listView.SetSelection(0);
            else
                ShowInspector(library);
        }

        private void AddItemsForCategory()
        {
            switch (selectedCategory)
            {
                case EM_Categories.Signals:
                    AddItems(library.Signals);
                    break;

                case EM_Categories.RuleSets:
                    AddItems(library.RuleSets);
                    break;

                case EM_Categories.Effects:
                    AddItems(library.Effects);
                    break;

                case EM_Categories.Metrics:
                    AddItems(library.Metrics);
                    break;

                case EM_Categories.Domains:
                    AddItems(library.Domains);
                    break;

                case EM_Categories.Profiles:
                    AddItems(library.Profiles);
                    break;
            }
        }

        private void AddItems<T>(T[] source) where T : UnityEngine.Object
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                items.Add(source[i]);
            }
        }

        private void UpdateCreateButtonLabel()
        {
            if (createAssetButton == null)
                return;

            createAssetButton.text = "Create " + GetCategoryLabel(selectedCategory);
        }

        private void UpdateStatusLabel()
        {
            if (statusLabel == null)
                return;

            if (library == null)
            {
                statusLabel.text = "No library assigned.";
                return;
            }

            statusLabel.text = "Items: " + items.Count + " | Category: " + GetCategoryLabel(selectedCategory);
        }

        private void ShowInspector(Object target)
        {
            inspectorRoot.Clear();

            if (target == null)
            {
                HelpBox help = new HelpBox("Select or create a library to start.", HelpBoxMessageType.Info);
                inspectorRoot.Add(help);
                return;
            }

            InspectorElement inspector = new InspectorElement(target);
            inspectorRoot.Add(inspector);
        }

        private void SelectItem(Object asset)
        {
            if (asset == null)
                return;

            int index = items.IndexOf(asset);

            if (index < 0)
                return;

            listView.SetSelection(index);
            listView.ScrollToItem(index);
        }

        private static string GetCategoryLabel(EM_Categories category)
        {
            switch (category)
            {
                case EM_Categories.Signals:
                    return "Signal";

                case EM_Categories.RuleSets:
                    return "Rule Set";

                case EM_Categories.Effects:
                    return "Effect";

                case EM_Categories.Metrics:
                    return "Metric";

                case EM_Categories.Domains:
                    return "Domain";

                case EM_Categories.Profiles:
                    return "Profile";

                default:
                    return "Asset";
            }
        }

        private static string GetCategoryTooltip(EM_Categories category)
        {
            switch (category)
            {
                case EM_Categories.Signals:
                    return "Signals are observable values emitted by gameplay or systems. " +
                           "Metrics sample signals to feed rule evaluation.";

                case EM_Categories.RuleSets:
                    return "Rule Sets bind metric samples to effects using probability curves. " +
                           "Profiles activate rule sets through their domains.";

                case EM_Categories.Effects:
                    return "Effects are reusable outcome blocks triggered by rules. " +
                           "They can modify needs, resources, reputation, or cohesion.";

                case EM_Categories.Metrics:
                    return "Metrics sample a single signal on a fixed interval. " +
                           "They normalize values into 0-1 space for rule evaluation.";

                case EM_Categories.Domains:
                    return "Domains group related rule sets for readability and profiling. " +
                           "Profiles enable domains to activate their rule sets.";

                case EM_Categories.Profiles:
                    return "Profiles select the active domains for a society. " +
                           "Rule sets and metrics are derived from those domains.";

                default:
                    return "Emergence asset category.";
            }
        }

        private EM_MechanicLibrary FindFirstLibrary()
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_MechanicLibrary");

            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<EM_MechanicLibrary>(path);
        }
        #endregion

        #endregion
    }
}
