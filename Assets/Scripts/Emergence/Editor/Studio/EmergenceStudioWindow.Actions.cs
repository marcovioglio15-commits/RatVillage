using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Emergence
{
    /// <summary>
    /// Interaction handlers and helper methods for Emergence Studio.
    /// </summary>
    public sealed partial class EmergenceStudioWindow
    {
        #region Actions
        private void OnLibraryChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            library = changeEvent.newValue as EM_MechanicLibrary;
            RefreshLibrary();
            ShowInspector(library);
        }

        private void OnCategoryChanged(ChangeEvent<System.Enum> changeEvent)
        {
            selectedCategory = (EmergenceLibraryCategory)changeEvent.newValue;
            if (categoryField != null)
                categoryField.tooltip = GetCategoryTooltip(selectedCategory);
            RefreshLibrary();
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            UnityEngine.Object selected = listView.selectedItem as UnityEngine.Object;
            ShowInspector(selected != null ? selected : library);
        }

        private void CreateLibrary()
        {
            EM_MechanicLibrary createdLibrary = EmergenceLibraryEditorUtility.CreateLibraryAsset();

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

            UnityEngine.Object asset = EmergenceLibraryEditorUtility.CreateDefinitionAsset(selectedCategory);

            if (asset == null)
                return;

            EmergenceLibraryEditorUtility.AddToLibrary(library, asset, selectedCategory);
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
                case EmergenceLibraryCategory.Signals:
                    AddItems(library.Signals);
                    break;

                case EmergenceLibraryCategory.RuleSets:
                    AddItems(library.RuleSets);
                    break;

                case EmergenceLibraryCategory.Effects:
                    AddItems(library.Effects);
                    break;

                case EmergenceLibraryCategory.Metrics:
                    AddItems(library.Metrics);
                    break;

                case EmergenceLibraryCategory.Norms:
                    AddItems(library.Norms);
                    break;

                case EmergenceLibraryCategory.Institutions:
                    AddItems(library.Institutions);
                    break;

                case EmergenceLibraryCategory.Domains:
                    AddItems(library.Domains);
                    break;

                case EmergenceLibraryCategory.Profiles:
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

        private void ShowInspector(UnityEngine.Object target)
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

        private void SelectItem(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            int index = items.IndexOf(asset);

            if (index < 0)
                return;

            listView.SetSelection(index);
            listView.ScrollToItem(index);
        }

        private static string GetCategoryLabel(EmergenceLibraryCategory category)
        {
            switch (category)
            {
                case EmergenceLibraryCategory.Signals:
                    return "Signal";

                case EmergenceLibraryCategory.RuleSets:
                    return "Rule Set";

                case EmergenceLibraryCategory.Effects:
                    return "Effect";

                case EmergenceLibraryCategory.Metrics:
                    return "Metric";

                case EmergenceLibraryCategory.Norms:
                    return "Norm";

                case EmergenceLibraryCategory.Institutions:
                    return "Institution";

                case EmergenceLibraryCategory.Domains:
                    return "Domain";

                case EmergenceLibraryCategory.Profiles:
                    return "Profile";

                default:
                    return "Asset";
            }
        }

        private static string GetCategoryTooltip(EmergenceLibraryCategory category)
        {
            switch (category)
            {
                case EmergenceLibraryCategory.Signals:
                    return "Signals are the events or observations emitted by gameplay or systems. " +
                           "Rules listen to signals to drive emergent behavior.";

                case EmergenceLibraryCategory.RuleSets:
                    return "Rule Sets are curated lists of signal-to-effect rules. " +
                           "Profiles enable or disable them to shape different societies.";

                case EmergenceLibraryCategory.Effects:
                    return "Effects are reusable outcome blocks that modify needs, resources, reputation, or cohesion. " +
                           "Rules reference effects so you can retune behavior without rewriting logic.";

                case EmergenceLibraryCategory.Metrics:
                    return "Metrics sample telemetry like population or average needs. " +
                           "Use them for balancing, dashboards, or to emit alerts via signals.";

                case EmergenceLibraryCategory.Norms:
                    return "Norms describe social rules enforced by institutions. " +
                           "They define what counts as a violation and the effect it triggers.";

                case EmergenceLibraryCategory.Institutions:
                    return "Institutions enforce norms and apply social pressure. " +
                           "They shape how strongly a society reacts to violations.";

                case EmergenceLibraryCategory.Domains:
                    return "Domains group related signals and rule sets under one theme. " +
                           "Use domains to allocate budgets and tune a whole topic at once.";

                case EmergenceLibraryCategory.Profiles:
                    return "Profiles bundle tuning for a society: domains, rule sets, metrics, and tick rates. " +
                           "Different profiles let you scale behavior across factions or regions.";

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
    }
}
