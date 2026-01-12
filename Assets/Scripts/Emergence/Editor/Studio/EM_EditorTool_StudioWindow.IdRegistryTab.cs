using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Id Registry Tab
        private VisualElement BuildIdRegistryTab()
        {
            VisualElement root = new VisualElement();
            root.name = "IdRegistryTab";
            root.style.flexGrow = 1f;

            TwoPaneSplitView splitView = new TwoPaneSplitView(0, LeftPaneWidth, TwoPaneSplitViewOrientation.Horizontal);
            splitView.Add(BuildIdRegistryLeftPane());
            splitView.Add(BuildIdRegistryRightPane());

            root.Add(splitView);
            return root;
        }

        private VisualElement BuildIdRegistryLeftPane()
        {
            VisualElement leftPane = new VisualElement();
            leftPane.style.flexDirection = FlexDirection.Column;
            leftPane.style.flexGrow = 1f;
            leftPane.style.paddingRight = 6f;

            idFilterField = new EnumField("Category", idFilterCategory);
            idFilterField.tooltip = "Filter the registry by id category.";
            idFilterField.RegisterValueChangedCallback(OnIdFilterChanged);
            leftPane.Add(idFilterField);

            idSearchField = new TextField("Search");
            idSearchField.isDelayed = true;
            idSearchField.tooltip = "Filter ids by string.";
            idSearchField.RegisterValueChangedCallback(OnIdSearchChanged);
            leftPane.Add(idSearchField);

            idListView = new ListView();
            idListView.itemsSource = idItems;
            idListView.makeItem = MakeIdListItem;
            idListView.bindItem = BindIdListItem;
            idListView.selectionType = SelectionType.Single;
            idListView.style.flexGrow = 1f;
            idListView.style.minHeight = 260f;
            idListView.selectionChanged += OnIdSelectionChanged;
            leftPane.Add(idListView);

            deleteIdButton = new Button(DeleteSelectedIdItem);
            deleteIdButton.text = "Delete Selected";
            deleteIdButton.style.marginTop = 6f;
            leftPane.Add(deleteIdButton);

            VisualElement createBox = new VisualElement();
            createBox.style.marginTop = 6f;
            createBox.style.flexDirection = FlexDirection.Column;

            Label createLabel = new Label("Create Id");
            createLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            createBox.Add(createLabel);

            newIdCategoryField = new EnumField("Category", newIdCategory);
            newIdCategoryField.tooltip = "Category to scope uniqueness for the id.";
            newIdCategoryField.RegisterValueChangedCallback(OnNewIdCategoryChanged);
            createBox.Add(newIdCategoryField);

            newIdValueField = new TextField("Id");
            newIdValueField.tooltip = "Unique id string within the selected category.";
            createBox.Add(newIdValueField);

            newIdDescriptionField = new TextField("Description");
            newIdDescriptionField.multiline = true;
            newIdDescriptionField.tooltip = "Optional description for designers.";
            createBox.Add(newIdDescriptionField);

            createIdButton = new Button(CreateIdDefinition);
            createIdButton.text = "Create Id Definition";
            createBox.Add(createIdButton);

            leftPane.Add(createBox);

            return leftPane;
        }

        private VisualElement BuildIdRegistryRightPane()
        {
            VisualElement rightPane = new VisualElement();
            rightPane.style.flexGrow = 1f;
            rightPane.style.paddingLeft = 6f;

            ScrollView inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.style.flexGrow = 1f;
            inspectorScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            inspectorScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

            idInspectorRoot = inspectorScroll;
            rightPane.Add(inspectorScroll);

            return rightPane;
        }

        private VisualElement MakeIdListItem()
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

            row.AddManipulator(new ContextualMenuManipulator(evt => PopulateIdContextMenu(evt, elements)));

            renameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitRename(elements, RefreshIdRegistry);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    CancelRename(elements);
                    evt.StopPropagation();
                }
            });

            renameField.RegisterCallback<FocusOutEvent>(evt => CommitRename(elements, RefreshIdRegistry));
            return row;
        }

        private void BindIdListItem(VisualElement element, int index)
        {
            RenameItemElements elements = element.userData as RenameItemElements;

            if (elements == null)
                return;

            if (index < 0 || index >= idItems.Count)
            {
                elements.Asset = null;
                elements.Label.text = string.Empty;
                CancelRename(elements);
                return;
            }

            EM_IdDefinition item = idItems[index];
            elements.Asset = item;

            if (item == null)
            {
                elements.Label.text = "Missing";
                CancelRename(elements);
                return;
            }

            elements.Label.text = item.name + " - " + item.Id + " (" + item.Category + ")";
            CancelRename(elements);
        }
        #endregion

        #region Id Registry Events
        private void OnIdFilterChanged(ChangeEvent<System.Enum> changeEvent)
        {
            idFilterCategory = (EM_IdCategory)changeEvent.newValue;
            RefreshIdRegistry();
        }

        private void OnIdSearchChanged(ChangeEvent<string> changeEvent)
        {
            idSearchFilter = changeEvent.newValue;
            RefreshIdRegistry();
        }

        private void OnNewIdCategoryChanged(ChangeEvent<System.Enum> changeEvent)
        {
            newIdCategory = (EM_IdCategory)changeEvent.newValue;
        }

        private void OnIdSelectionChanged(System.Collections.Generic.IEnumerable<object> selection)
        {
            EM_IdDefinition selected = idListView != null ? idListView.selectedItem as EM_IdDefinition : null;
            ShowIdInspector(selected);
        }
        #endregion
    }
}
