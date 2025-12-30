using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow : EditorWindow
    {
        #region Fields

        #region Constants
        private const string WindowTitle = "Emergence Studio";
        private const float LeftPaneWidth = 320f;
        #endregion

        #region Lookup
        private EM_MechanicLibrary library;
        private EM_Categories selectedCategory = EM_Categories.Signals;
        private readonly List<Object> items = new List<Object>();
        #endregion

        #endregion

        #region Methods

        #region Menu
        [MenuItem("Tools/Emergence/Studio")]
        public static void OpenWindow()
        {
            EM_EditorTool_StudioWindow window = GetWindow<EM_EditorTool_StudioWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }
        #endregion

        #region Unity LyfeCycle Lifecycle
        private void OnEnable()
        {
            if (library != null)
                return;

            library = FindFirstLibrary();
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
            TwoPaneSplitView splitView = BuildSplitView();
            VisualElement statusBar = BuildStatusBar();

            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(splitView);
            rootVisualElement.Add(statusBar);

            RefreshLibrary();
        }
        #endregion

        #endregion
    }
}
