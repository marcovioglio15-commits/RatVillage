using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Inspector Tab
        private VisualElement BuildInspectorTab()
        {
            VisualElement root = new VisualElement();
            root.name = "InspectorTab";
            root.style.flexGrow = 1f;

            TwoPaneSplitView splitView = new TwoPaneSplitView(0, LeftPaneWidth, TwoPaneSplitViewOrientation.Horizontal);
            splitView.Add(BuildInspectorLeftPane());
            splitView.Add(BuildInspectorRightPane());

            root.Add(splitView);
            return root;
        }

        private VisualElement BuildInspectorLeftPane()
        {
            VisualElement leftPane = new VisualElement();
            leftPane.style.flexDirection = FlexDirection.Column;
            leftPane.style.flexGrow = 1f;
            leftPane.style.paddingRight = 6f;

            categoryField = new EnumField("Category", selectedCategory);
            categoryField.style.marginBottom = 4f;
            categoryField.tooltip = GetCategoryTooltip(selectedCategory);
            categoryField.RegisterValueChangedCallback(OnCategoryChanged);
            leftPane.Add(categoryField);

            itemSearchField = new TextField("Search");
            itemSearchField.isDelayed = true;
            itemSearchField.tooltip = "Filter assets by name.";
            itemSearchField.RegisterValueChangedCallback(OnItemSearchChanged);
            leftPane.Add(itemSearchField);

            listView = new ListView();
            listView.itemsSource = items;
            listView.makeItem = MakeListItem;
            listView.bindItem = BindListItem;
            listView.selectionType = SelectionType.Single;
            listView.style.flexGrow = 1f;
            listView.style.minHeight = 260f;
            listView.selectionChanged += OnSelectionChanged;
            leftPane.Add(listView);

            createAssetButton = new Button(CreateAssetForCategory);
            createAssetButton.style.marginTop = 6f;
            leftPane.Add(createAssetButton);

            deleteAssetButton = new Button(DeleteSelectedInspectorItem);
            deleteAssetButton.text = "Delete Selected";
            deleteAssetButton.style.marginTop = 4f;
            leftPane.Add(deleteAssetButton);

            return leftPane;
        }

        private VisualElement BuildInspectorRightPane()
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

        private VisualElement MakeListItem()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            Label label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 6f;
            label.style.flexGrow = 1f;
            row.Add(label);

            TextField renameField = new TextField();
            renameField.isDelayed = true;
            renameField.style.flexGrow = 1f;
            renameField.style.display = DisplayStyle.None;
            row.Add(renameField);

            RenameItemElements elements = new RenameItemElements
            {
                Label = label,
                Field = renameField
            };
            row.userData = elements;

            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.clickCount != 2)
                    return;

                BeginRename(elements);
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            row.AddManipulator(new ContextualMenuManipulator(evt => PopulateInspectorContextMenu(evt, elements)));

            renameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitRename(elements, RefreshLibraryItems);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    CancelRename(elements);
                    evt.StopPropagation();
                }
            });

            renameField.RegisterCallback<FocusOutEvent>(evt => CommitRename(elements, RefreshLibraryItems));
            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            RenameItemElements elements = element.userData as RenameItemElements;

            if (elements == null)
                return;

            if (index < 0 || index >= items.Count)
            {
                elements.Asset = null;
                elements.Label.text = string.Empty;
                CancelRename(elements);
                return;
            }

            Object item = items[index];
            elements.Asset = item;

            if (item == null)
            {
                elements.Label.text = "Missing";
                CancelRename(elements);
                return;
            }

            bool missingId = IsMissingIdDefinition(item);
            elements.Label.text = missingId ? item.name + " [! Id]" : item.name;
            CancelRename(elements);
        }
        #endregion

        #region Inspector Events
        private void OnCategoryChanged(ChangeEvent<System.Enum> changeEvent)
        {
            selectedCategory = (EM_Categories)changeEvent.newValue;

            if (categoryField != null)
                categoryField.tooltip = GetCategoryTooltip(selectedCategory);

            RefreshLibraryItems();
        }

        private void OnItemSearchChanged(ChangeEvent<string> changeEvent)
        {
            itemSearchFilter = changeEvent.newValue;
            RefreshLibraryItems();
        }

        private void OnSelectionChanged(System.Collections.Generic.IEnumerable<object> selection)
        {
            Object selected = listView != null ? listView.selectedItem as Object : null;
            ShowInspector(selected != null ? selected : library);
        }
        #endregion
    }
}
