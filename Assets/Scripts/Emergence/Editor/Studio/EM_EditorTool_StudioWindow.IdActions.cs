using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Id Registry
        private void CreateIdDefinition()
        {
            if (newIdCategory == EM_IdCategory.None || newIdCategory == EM_IdCategory.Any)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Select a valid id category.", "Ok");
                return;
            }

            string idValue = newIdValueField != null ? newIdValueField.value : string.Empty;

            if (string.IsNullOrWhiteSpace(idValue))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Id value cannot be empty.", "Ok");
                return;
            }

            Dictionary<string, EM_IdDefinition> lookup = EM_StudioIdUtility.BuildIdLookup(rootFolder);
            string key = EM_StudioIdUtility.BuildKey(newIdCategory, idValue);

            if (lookup.ContainsKey(key))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Id already exists in this category.", "Ok");
                RefreshIdRegistry();
                SelectIdItem(lookup[key]);
                return;
            }

            string description = newIdDescriptionField != null ? newIdDescriptionField.value : string.Empty;
            EM_IdDefinition created = EM_StudioIdUtility.FindOrCreateId(rootFolder, newIdCategory, idValue, description, lookup);

            if (created == null)
                return;

            RefreshIdRegistry();
            SelectIdItem(created);

            if (newIdValueField != null)
                newIdValueField.value = string.Empty;

            if (newIdDescriptionField != null)
                newIdDescriptionField.value = string.Empty;
        }

        private void AutoAssignMissingIds()
        {
            int updated = EM_StudioIdAssigner.AssignMissingIds(rootFolder);
            RefreshIdRegistry();
            RefreshValidation();
            RefreshLibraryItems();

            if (validationStatusLabel != null)
                validationStatusLabel.text = "Issues: " + validationIssues.Count + " | Auto-assigned: " + updated;
        }
        #endregion
    }
}
