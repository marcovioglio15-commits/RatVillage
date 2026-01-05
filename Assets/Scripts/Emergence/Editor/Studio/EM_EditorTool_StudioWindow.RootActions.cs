using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Root Folder
        private void OnRootFolderChanged(UnityEngine.UIElements.ChangeEvent<string> changeEvent)
        {
            string newValue = changeEvent.newValue;
            string resolved = EM_StudioAssetUtility.ResolveRootFolder(newValue);

            if (!resolved.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog(WindowTitle, "Root folder must be inside Assets.", "Ok");

                if (rootFolderField != null)
                    rootFolderField.SetValueWithoutNotify(rootFolder);

                return;
            }

            rootFolder = resolved;
            SavePreferences();

            if (rootFolderField != null)
                rootFolderField.SetValueWithoutNotify(rootFolder);

            if (library == null)
                library = FindFirstLibrary(rootFolder);

            if (libraryField != null)
                libraryField.SetValueWithoutNotify(library);

            RefreshAll();
        }

        private void SelectRootFolder()
        {
            string selected = EditorUtility.OpenFolderPanel("Select Root Folder", Application.dataPath, string.Empty);

            if (string.IsNullOrWhiteSpace(selected))
                return;

            string relative;
            bool valid = EM_StudioAssetUtility.TryMakeRelativePath(selected, out relative);

            if (!valid)
            {
                EditorUtility.DisplayDialog(WindowTitle, "Folder must be inside the Assets directory.", "Ok");
                return;
            }

            rootFolder = EM_StudioAssetUtility.ResolveRootFolder(relative);
            SavePreferences();

            if (rootFolderField != null)
                rootFolderField.SetValueWithoutNotify(rootFolder);

            if (library == null)
                library = FindFirstLibrary(rootFolder);

            if (libraryField != null)
                libraryField.SetValueWithoutNotify(library);

            RefreshAll();
        }
        #endregion
    }
}
