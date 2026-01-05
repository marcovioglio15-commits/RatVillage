using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow : EditorWindow
    {
        #region Menu
        [MenuItem("Tools/Emergence/Studio")]
        public static void OpenWindow()
        {
            EM_EditorTool_StudioWindow window = GetWindow<EM_EditorTool_StudioWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(960f, 640f);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            LoadPreferences();

            if (library == null)
                library = FindFirstLibrary(rootFolder);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 6f;
            rootVisualElement.style.paddingRight = 6f;
            rootVisualElement.style.paddingTop = 6f;
            rootVisualElement.style.paddingBottom = 6f;

            Toolbar toolbar = BuildToolbar();
            Toolbar tabBar = BuildTabBar();
            VisualElement tabContainer = BuildTabContainer();
            VisualElement statusBar = BuildStatusBar();

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(tabBar);
            rootVisualElement.Add(tabContainer);
            rootVisualElement.Add(statusBar);

            RefreshAll();
        }
        #endregion
    }
}
