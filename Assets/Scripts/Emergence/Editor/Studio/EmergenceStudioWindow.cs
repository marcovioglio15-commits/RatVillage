using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Emergence
{
    /// <summary>
    /// Editor window for managing emergence libraries and definitions.
    /// </summary>
    public sealed partial class EmergenceStudioWindow : EditorWindow
    {
        #region Constants
        private const string WindowTitle = "Emergence Studio";
        private const float LeftPaneWidth = 320f;
        #endregion

        #region State
        private EM_MechanicLibrary library;
        private EmergenceLibraryCategory selectedCategory = EmergenceLibraryCategory.Signals;
        private readonly List<Object> items = new List<Object>();
        #endregion

        #region Menu
        /// <summary>
        /// Opens the Emergence Studio window.
        /// </summary>
        [MenuItem("Tools/Emergence/Studio")]
        public static void OpenWindow()
        {
            EmergenceStudioWindow window = GetWindow<EmergenceStudioWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 600f);
            window.Show();
        }
        #endregion

        #region Unity
        /// <summary>
        /// Initializes the editor window.
        /// </summary>
        private void OnEnable()
        {
            if (library != null)
                return;

            library = FindFirstLibrary();
        }

        /// <summary>
        /// Builds the UI layout.
        /// </summary>
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
    }
}
