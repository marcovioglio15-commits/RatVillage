using UnityEditor;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Asset Lookup
        private EM_MechanicLibrary FindFirstLibrary(string rootFolderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:EM_MechanicLibrary", new[] { rootFolderPath });

            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<EM_MechanicLibrary>(path);
        }
        #endregion
    }
}
