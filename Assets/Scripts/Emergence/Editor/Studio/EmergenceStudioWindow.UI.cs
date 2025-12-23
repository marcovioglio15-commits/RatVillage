using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Emergence
{
    /// <summary>
    /// UI layout and visual elements for Emergence Studio.
    /// </summary>
    public sealed partial class EmergenceStudioWindow
    {
        #region UI State
        private ListView listView;
        private VisualElement inspectorRoot;
        private ObjectField libraryField;
        private EnumField categoryField;
        private Button createAssetButton;
        private Label statusLabel;
        #endregion

        #region UI
        private Toolbar BuildToolbar()
        {
            Toolbar toolbar = new Toolbar();

            Label titleLabel = new Label(WindowTitle);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginRight = 12f;
            toolbar.Add(titleLabel);

            libraryField = new ObjectField("Library");
            libraryField.objectType = typeof(EM_MechanicLibrary);
            libraryField.value = library;
            libraryField.style.minWidth = 260f;
            libraryField.tooltip = "Library is the master registry for Signals, Rule Sets, Effects, Metrics, Domains, and Profiles. " +
                "Assign one before creating or editing assets in Emergence Studio.";
            libraryField.RegisterValueChangedCallback(OnLibraryChanged);
            toolbar.Add(libraryField);

            ToolbarButton createLibraryButton = new ToolbarButton(CreateLibrary);
            createLibraryButton.text = "Create Library";
            toolbar.Add(createLibraryButton);

            ToolbarButton refreshButton = new ToolbarButton(RefreshLibrary);
            refreshButton.text = "Refresh";
            toolbar.Add(refreshButton);

            return toolbar;
        }

        private TwoPaneSplitView BuildSplitView()
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView(0, LeftPaneWidth, TwoPaneSplitViewOrientation.Horizontal);

            VisualElement leftPane = BuildLeftPane();
            VisualElement rightPane = BuildRightPane();

            splitView.Add(leftPane);
            splitView.Add(rightPane);

            return splitView;
        }

        private VisualElement BuildLeftPane()
        {
            VisualElement leftPane = new VisualElement();
            leftPane.style.flexDirection = FlexDirection.Column;
            leftPane.style.flexGrow = 1f;
            leftPane.style.paddingRight = 6f;

            categoryField = new EnumField("Category", selectedCategory);
            categoryField.style.marginBottom = 6f;
            categoryField.tooltip = GetCategoryTooltip(selectedCategory);
            categoryField.RegisterValueChangedCallback(OnCategoryChanged);

            ScrollView leftScroll = new ScrollView(ScrollViewMode.Vertical);
            leftScroll.style.flexGrow = 1f;
            leftScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            leftScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            leftScroll.Add(categoryField);

            listView = new ListView();
            listView.itemsSource = items;
            listView.makeItem = MakeListItem;
            listView.bindItem = BindListItem;
            listView.selectionType = SelectionType.Single;
            listView.style.flexGrow = 1f;
            listView.style.minHeight = 240f;
            listView.selectionChanged += OnSelectionChanged;
            leftScroll.Add(listView);

            createAssetButton = new Button(CreateAssetForCategory);
            createAssetButton.style.marginTop = 6f;
            leftScroll.Add(createAssetButton);

            leftPane.Add(leftScroll);

            return leftPane;
        }

        private VisualElement BuildRightPane()
        {
            VisualElement rightPane = new VisualElement();
            rightPane.style.flexGrow = 1f;
            rightPane.style.paddingLeft = 6f;

            ScrollView inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.style.flexGrow = 1f;
            inspectorScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            inspectorScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            inspectorRoot = inspectorScroll;
            rightPane.Add(inspectorScroll);

            return rightPane;
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

        private VisualElement MakeListItem()
        {
            Label label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 6f;
            return label;
        }

        private void BindListItem(VisualElement element, int index)
        {
            Label label = element as Label;

            if (label == null)
                return;

            if (index < 0 || index >= items.Count)
            {
                label.text = string.Empty;
                return;
            }

            Object item = items[index];
            label.text = item != null ? item.name : "Missing";
        }
        #endregion
    }
}
