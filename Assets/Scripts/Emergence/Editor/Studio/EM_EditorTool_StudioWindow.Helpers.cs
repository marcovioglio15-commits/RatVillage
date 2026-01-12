using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Rename UI
        private sealed class RenameItemElements
        {
            public Label Label;
            public TextField Field;
            public UnityObject Asset;
            public bool IsRenaming;
        }

        private void BeginRename(RenameItemElements elements)
        {
            if (elements == null || elements.Asset == null || elements.IsRenaming)
                return;

            elements.IsRenaming = true;
            elements.Field.value = elements.Asset.name;
            elements.Field.style.display = DisplayStyle.Flex;
            elements.Label.style.display = DisplayStyle.None;
            elements.Field.Focus();
            elements.Field.SelectAll();
        }

        private void CancelRename(RenameItemElements elements)
        {
            if (elements == null)
                return;

            elements.IsRenaming = false;
            elements.Field.style.display = DisplayStyle.None;
            elements.Label.style.display = DisplayStyle.Flex;
        }

        private void CommitRename(RenameItemElements elements, Action refreshAction)
        {
            if (elements == null || !elements.IsRenaming)
                return;

            if (elements.Asset == null)
            {
                CancelRename(elements);
                return;
            }

            string newName = elements.Field.value != null ? elements.Field.value.Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(newName) || newName == elements.Asset.name)
            {
                CancelRename(elements);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(elements.Asset);

            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                string error = AssetDatabase.RenameAsset(assetPath, newName);

                if (!string.IsNullOrWhiteSpace(error))
                    EditorUtility.DisplayDialog(WindowTitle, error, "Ok");
            }

            CancelRename(elements);

            if (refreshAction != null)
                refreshAction();
        }
        #endregion

        #region Context Menus
        private void PopulateInspectorContextMenu(ContextualMenuPopulateEvent evt, RenameItemElements elements)
        {
            if (elements == null || elements.Asset == null)
                return;

            evt.menu.AppendAction("Rename", _ => BeginRename(elements));
            evt.menu.AppendAction("Delete", _ => DeleteInspectorItem(elements.Asset));
        }

        private void PopulateIdContextMenu(ContextualMenuPopulateEvent evt, RenameItemElements elements)
        {
            if (elements == null || elements.Asset == null)
                return;

            evt.menu.AppendAction("Rename", _ => BeginRename(elements));

            EM_IdDefinition idDefinition = elements.Asset as EM_IdDefinition;

            if (idDefinition != null)
                evt.menu.AppendAction("Delete", _ => DeleteIdItem(idDefinition));
        }
        #endregion

        #region Deletes
        private void DeleteSelectedInspectorItem()
        {
            UnityObject selected = listView != null ? listView.selectedItem as UnityObject : null;
            DeleteInspectorItem(selected);
        }

        private void DeleteSelectedIdItem()
        {
            EM_IdDefinition selected = idListView != null ? idListView.selectedItem as EM_IdDefinition : null;
            DeleteIdItem(selected);
        }

        private void DeleteInspectorItem(UnityObject item)
        {
            if (item == null)
                return;

            bool confirm = EditorUtility.DisplayDialog(WindowTitle, "Delete asset '" + item.name + "'?", "Delete", "Cancel");

            if (!confirm)
                return;

            if (library != null)
                RemoveFromLibrary(library, item, selectedCategory);

            string assetPath = AssetDatabase.GetAssetPath(item);

            if (!string.IsNullOrWhiteSpace(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.SaveAssets();
            RefreshLibraryItems();
        }

        private void DeleteIdItem(EM_IdDefinition item)
        {
            if (item == null)
                return;

            bool confirm = EditorUtility.DisplayDialog(WindowTitle, "Delete id asset '" + item.name + "'?", "Delete", "Cancel");

            if (!confirm)
                return;

            string assetPath = AssetDatabase.GetAssetPath(item);

            if (!string.IsNullOrWhiteSpace(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.SaveAssets();
            RefreshIdRegistry();
        }

        private static void RemoveFromLibrary(EM_MechanicLibrary library, UnityObject asset, EM_Categories category)
        {
            if (library == null || asset == null)
                return;

            SerializedObject serializedLibrary = new SerializedObject(library);
            SerializedProperty listProperty = serializedLibrary.FindProperty(GetLibraryListPropertyName(category));

            if (listProperty == null || !listProperty.isArray)
                return;

            RemoveObjectFromArray(listProperty, asset);
            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static void RemoveObjectFromArray(SerializedProperty listProperty, UnityObject asset)
        {
            for (int i = listProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);

                if (element.objectReferenceValue != asset)
                    continue;

                listProperty.DeleteArrayElementAtIndex(i);

                if (i < listProperty.arraySize && listProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    listProperty.DeleteArrayElementAtIndex(i);

                return;
            }
        }
        #endregion

        #region Library List Names
        private static string GetLibraryListPropertyName(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return "signals";

            if (category == EM_Categories.RuleSets)
                return "ruleSets";

            if (category == EM_Categories.Effects)
                return "effects";

            if (category == EM_Categories.Metrics)
                return "metrics";

            if (category == EM_Categories.Domains)
                return "domains";

            if (category == EM_Categories.Profiles)
                return "profiles";

            return string.Empty;
        }
        #endregion
    }
}
