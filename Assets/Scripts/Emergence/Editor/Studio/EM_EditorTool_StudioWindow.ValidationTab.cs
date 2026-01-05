using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Validation Tab
        private VisualElement BuildValidationTab()
        {
            VisualElement root = new VisualElement();
            root.name = "ValidationTab";
            root.style.flexGrow = 1f;

            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 6f;

            Button refreshButton = new Button(RefreshValidation);
            refreshButton.text = "Run Validation";
            header.Add(refreshButton);

            Button autoFixButton = new Button(AutoAssignMissingIds);
            autoFixButton.text = "Auto-Create Missing Ids";
            autoFixButton.tooltip = "Creates id definitions for missing references using legacy ids.";
            header.Add(autoFixButton);

            Button syncButton = new Button(SyncLibraryForAllCategories);
            syncButton.text = "Sync Library";
            syncButton.tooltip = "Add assets from the root folder into all library categories.";
            header.Add(syncButton);

            root.Add(header);

            TwoPaneSplitView splitView = new TwoPaneSplitView(0, LeftPaneWidth, TwoPaneSplitViewOrientation.Horizontal);
            splitView.Add(BuildValidationLeftPane());
            splitView.Add(BuildValidationRightPane());
            root.Add(splitView);

            return root;
        }

        private VisualElement BuildValidationLeftPane()
        {
            VisualElement leftPane = new VisualElement();
            leftPane.style.flexDirection = FlexDirection.Column;
            leftPane.style.flexGrow = 1f;
            leftPane.style.paddingRight = 6f;

            validationStatusLabel = new Label();
            validationStatusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            validationStatusLabel.style.marginBottom = 4f;
            leftPane.Add(validationStatusLabel);

            validationListView = new ListView();
            validationListView.itemsSource = validationIssues;
            validationListView.makeItem = MakeValidationListItem;
            validationListView.bindItem = BindValidationListItem;
            validationListView.selectionType = SelectionType.Single;
            validationListView.style.flexGrow = 1f;
            validationListView.selectionChanged += OnValidationSelectionChanged;
            leftPane.Add(validationListView);

            return leftPane;
        }

        private VisualElement BuildValidationRightPane()
        {
            VisualElement rightPane = new VisualElement();
            rightPane.style.flexGrow = 1f;
            rightPane.style.paddingLeft = 6f;

            ScrollView inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.style.flexGrow = 1f;
            inspectorScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            inspectorScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            validationInspectorRoot = inspectorScroll;
            rightPane.Add(inspectorScroll);

            return rightPane;
        }

        private VisualElement MakeValidationListItem()
        {
            Label label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 6f;
            return label;
        }

        private void BindValidationListItem(VisualElement element, int index)
        {
            Label label = element as Label;

            if (label == null)
                return;

            if (index < 0 || index >= validationIssues.Count)
            {
                label.text = string.Empty;
                return;
            }

            EM_StudioValidationIssue issue = validationIssues[index];
            label.text = "[" + issue.Severity + "] " + issue.Message;
        }
        #endregion

        #region Validation Events
        private void OnValidationSelectionChanged(System.Collections.Generic.IEnumerable<object> selection)
        {
            EM_StudioValidationIssue issue = validationListView != null && validationListView.selectedIndex >= 0
                ? validationIssues[validationListView.selectedIndex]
                : new EM_StudioValidationIssue();

            ShowValidationInspector(issue.Target);
        }
        #endregion
    }
}
