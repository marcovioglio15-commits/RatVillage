using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Toolbar
        private Toolbar BuildToolbar()
        {
            Toolbar toolbar = new Toolbar();

            Label titleLabel = new Label(WindowTitle);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginRight = 8f;
            toolbar.Add(titleLabel);

            rootFolderField = new TextField("Root Folder");
            rootFolderField.value = rootFolder;
            rootFolderField.style.minWidth = 260f;
            rootFolderField.tooltip = "Root folder for Emergence scriptables. Defaults to Assets/Scriptable Objects.";
            rootFolderField.RegisterValueChangedCallback(OnRootFolderChanged);
            toolbar.Add(rootFolderField);

            rootFolderButton = new Button(SelectRootFolder);
            rootFolderButton.text = "Select";
            rootFolderButton.tooltip = "Pick a root folder inside the project Assets.";
            toolbar.Add(rootFolderButton);

            libraryField = new ObjectField("Library");
            libraryField.objectType = typeof(EM_MechanicLibrary);
            libraryField.value = library;
            libraryField.style.minWidth = 220f;
            libraryField.tooltip = "Library is the master registry for Emergence definitions.";
            libraryField.RegisterValueChangedCallback(OnLibraryChanged);
            toolbar.Add(libraryField);

            ToolbarButton createLibraryButton = new ToolbarButton(CreateLibrary);
            createLibraryButton.text = "Create Library";
            toolbar.Add(createLibraryButton);

            ToolbarButton syncButton = new ToolbarButton(SyncLibraryForSelectedCategory);
            syncButton.text = "Sync Category";
            syncButton.tooltip = "Add assets from the root folder into the selected library category.";
            toolbar.Add(syncButton);

            ToolbarButton refreshButton = new ToolbarButton(RefreshAll);
            refreshButton.text = "Refresh";
            toolbar.Add(refreshButton);

            return toolbar;
        }

        private VisualElement BuildStatusBar()
        {
            VisualElement statusBar = new VisualElement();
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.marginTop = 4f;

            statusLabel = new Label();
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            statusBar.Add(statusLabel);

            return statusBar;
        }
        #endregion
    }
}
