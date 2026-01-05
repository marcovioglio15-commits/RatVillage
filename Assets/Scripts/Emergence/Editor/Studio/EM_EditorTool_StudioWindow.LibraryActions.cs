using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Library
        private void OnLibraryChanged(UnityEngine.UIElements.ChangeEvent<Object> changeEvent)
        {
            library = changeEvent.newValue as EM_MechanicLibrary;
            RefreshAll();
            ShowInspector(library);
        }

        private void CreateLibrary()
        {
            EM_MechanicLibrary createdLibrary = EM_EditorTool_Utilities.CreateLibraryAsset(rootFolder);

            if (createdLibrary == null)
                return;

            library = createdLibrary;

            if (libraryField != null)
                libraryField.value = library;

            RefreshAll();
            ShowInspector(library);
        }

        private void CreateAssetForCategory()
        {
            if (library == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Assign or create a library before creating assets.", "Ok");
                return;
            }

            Object asset = EM_EditorTool_Utilities.CreateDefinitionAsset(selectedCategory, rootFolder);

            if (asset == null)
                return;

            EM_EditorTool_Utilities.AddToLibrary(library, asset, selectedCategory);
            RefreshLibraryItems();
            SelectItem(asset);
        }

        private void SyncLibraryForSelectedCategory()
        {
            if (library == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Assign a library before syncing.", "Ok");
                return;
            }

            SyncCategory(selectedCategory);
            RefreshLibraryItems();
        }

        private void SyncLibraryForAllCategories()
        {
            if (library == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Assign a library before syncing.", "Ok");
                return;
            }

            SyncCategory(EM_Categories.Signals);
            SyncCategory(EM_Categories.Metrics);
            SyncCategory(EM_Categories.Effects);
            SyncCategory(EM_Categories.RuleSets);
            SyncCategory(EM_Categories.Domains);
            SyncCategory(EM_Categories.Profiles);
            RefreshAll();
        }
        #endregion
    }
}
