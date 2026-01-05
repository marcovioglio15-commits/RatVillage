using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Refresh
        private void RefreshAll()
        {
            RefreshLibraryItems();
            RefreshIdRegistry();
            RefreshValidation();
            UpdateStatusLabel();
        }

        private void RefreshActiveTab()
        {
            if (selectedTab == EM_StudioTab.Inspector)
                RefreshLibraryItems();
            else if (selectedTab == EM_StudioTab.IdRegistry)
                RefreshIdRegistry();
            else if (selectedTab == EM_StudioTab.Validation)
                RefreshValidation();
        }

        private void RefreshLibraryItems()
        {
            items.Clear();

            if (library == null)
            {
                UpdateCreateButtonLabel();
                UpdateStatusLabel();

                if (listView != null)
                    listView.Rebuild();

                ShowInspector(null);
                return;
            }

            AddItemsForCategory();
            UpdateCreateButtonLabel();
            UpdateStatusLabel();

            if (listView != null)
                listView.Rebuild();

            if (items.Count > 0 && listView != null)
                listView.SetSelection(0);
            else
                ShowInspector(library);
        }

        private void RefreshIdRegistry()
        {
            idItems.Clear();
            List<EM_IdDefinition> definitions = EM_StudioAssetUtility.FindAssets<EM_IdDefinition>(rootFolder);

            for (int i = 0; i < definitions.Count; i++)
            {
                EM_IdDefinition definition = definitions[i];

                if (definition == null)
                    continue;

                if (idFilterCategory != EM_IdCategory.Any && definition.Category != idFilterCategory)
                    continue;

                if (!string.IsNullOrWhiteSpace(idSearchFilter))
                {
                    string filter = idSearchFilter.Trim().ToLowerInvariant();
                    string id = definition.Id != null ? definition.Id.ToLowerInvariant() : string.Empty;

                    if (!id.Contains(filter))
                        continue;
                }

                idItems.Add(definition);
            }

            idItems.Sort((left, right) =>
            {
                string leftKey = left != null ? left.Id : string.Empty;
                string rightKey = right != null ? right.Id : string.Empty;
                return string.Compare(leftKey, rightKey, System.StringComparison.OrdinalIgnoreCase);
            });

            if (idListView != null)
                idListView.Rebuild();

            if (idItems.Count > 0 && idListView != null)
                idListView.SetSelection(0);
            else
                ShowIdInspector(null);
        }

        private void RefreshValidation()
        {
            validationIssues.Clear();

            if (library != null)
            {
                List<EM_StudioValidationIssue> results = EM_StudioValidator.BuildIssues(library, rootFolder);

                for (int i = 0; i < results.Count; i++)
                    validationIssues.Add(results[i]);
            }
            else
            {
                validationIssues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Error,
                    "No Mechanic Library assigned.", null));
            }

            if (validationListView != null)
                validationListView.Rebuild();

            if (validationStatusLabel != null)
                validationStatusLabel.text = "Issues: " + validationIssues.Count;
        }
        #endregion

        #region Inspectors
        private void ShowInspector(Object target)
        {
            if (inspectorRoot == null)
                return;

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

        private void ShowIdInspector(Object target)
        {
            if (idInspectorRoot == null)
                return;

            idInspectorRoot.Clear();

            if (target == null)
            {
                HelpBox help = new HelpBox("Select an id definition to inspect.", HelpBoxMessageType.Info);
                idInspectorRoot.Add(help);
                return;
            }

            InspectorElement inspector = new InspectorElement(target);
            idInspectorRoot.Add(inspector);
        }

        private void ShowValidationInspector(Object target)
        {
            if (validationInspectorRoot == null)
                return;

            validationInspectorRoot.Clear();

            if (target == null)
            {
                HelpBox help = new HelpBox("Select an issue to inspect its target.", HelpBoxMessageType.Info);
                validationInspectorRoot.Add(help);
                return;
            }

            InspectorElement inspector = new InspectorElement(target);
            validationInspectorRoot.Add(inspector);
        }
        #endregion

        #region Tab Visibility
        private void UpdateTabVisibility()
        {
            if (inspectorTabRoot != null)
                inspectorTabRoot.style.display = selectedTab == EM_StudioTab.Inspector ? DisplayStyle.Flex : DisplayStyle.None;

            if (idRegistryTabRoot != null)
                idRegistryTabRoot.style.display = selectedTab == EM_StudioTab.IdRegistry ? DisplayStyle.Flex : DisplayStyle.None;

            if (validationTabRoot != null)
                validationTabRoot.style.display = selectedTab == EM_StudioTab.Validation ? DisplayStyle.Flex : DisplayStyle.None;

            if (presetsTabRoot != null)
                presetsTabRoot.style.display = selectedTab == EM_StudioTab.Presets ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
    }
}
