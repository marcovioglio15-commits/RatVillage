using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Fields

        #region Constants
        private const string WindowTitle = "Emergence Studio";
        private const string RootFolderPrefKey = "Emergence.Studio.RootFolder";
        private const float LeftPaneWidth = 320f;
        #endregion

        #region Data
        private EM_MechanicLibrary library;
        private EM_Categories selectedCategory = EM_Categories.Signals;
        private EM_StudioTab selectedTab = EM_StudioTab.Inspector;
        private EM_IdCategory idFilterCategory = EM_IdCategory.Any;
        private EM_IdCategory newIdCategory = EM_IdCategory.Signal;
        private EM_StudioPresetType selectedPreset = EM_StudioPresetType.Neutral;
        private string rootFolder = EM_StudioAssetUtility.DefaultRootFolder;
        private string itemSearchFilter = string.Empty;
        private string idSearchFilter = string.Empty;
        private readonly List<Object> items = new List<Object>();
        private readonly List<EM_IdDefinition> idItems = new List<EM_IdDefinition>();
        private readonly List<EM_StudioValidationIssue> validationIssues = new List<EM_StudioValidationIssue>();
        #endregion

        #region Toolbar UI
        private TextField rootFolderField;
        private Button rootFolderButton;
        private ObjectField libraryField;
        private ToolbarToggle inspectorTabToggle;
        private ToolbarToggle idRegistryTabToggle;
        private ToolbarToggle validationTabToggle;
        private ToolbarToggle presetsTabToggle;
        #endregion

        #region Inspector UI
        private ListView listView;
        private VisualElement inspectorRoot;
        private EnumField categoryField;
        private TextField itemSearchField;
        private Button createAssetButton;
        private Label statusLabel;
        #endregion

        #region Tab Roots
        private VisualElement inspectorTabRoot;
        private VisualElement idRegistryTabRoot;
        private VisualElement validationTabRoot;
        private VisualElement presetsTabRoot;
        #endregion

        #region Id Registry UI
        private ListView idListView;
        private VisualElement idInspectorRoot;
        private EnumField idFilterField;
        private TextField idSearchField;
        private EnumField newIdCategoryField;
        private TextField newIdValueField;
        private TextField newIdDescriptionField;
        private Button createIdButton;
        #endregion

        #region Validation UI
        private ListView validationListView;
        private VisualElement validationInspectorRoot;
        private Label validationStatusLabel;
        #endregion

        #region Preset UI
        private EnumField presetField;
        private Label presetStatusLabel;
        #endregion

        #endregion
    }
}
