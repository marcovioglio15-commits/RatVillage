using UnityEditor;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Preferences
        private void LoadPreferences()
        {
            rootFolder = EditorPrefs.GetString(RootFolderPrefKey, EM_StudioAssetUtility.DefaultRootFolder);
            rootFolder = EM_StudioAssetUtility.ResolveRootFolder(rootFolder);
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(RootFolderPrefKey, rootFolder);
        }
        #endregion
    }
}
