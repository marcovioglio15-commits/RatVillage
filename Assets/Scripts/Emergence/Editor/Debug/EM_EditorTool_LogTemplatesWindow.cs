using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    /// <summary>
    /// Editor window for creating and editing debug message templates.
    /// </summary>
    internal sealed class EM_EditorTool_LogTemplatesWindow : EditorWindow
    {
        #region Fields
        #region Constants
        private const string MenuPath = "Tools/Emergence/Debug Templates";
        private const string DefaultFolder = "Assets/Scriptable Objects/Debug";
        private const string DefaultAssetPath = "Assets/Scriptable Objects/Debug/EM_DebugMessageTemplates.asset";
        #endregion

        #region Lookup
        private ObjectField templatesField;
        private VisualElement inspectorRoot;
        private Label statusLabel;
        #endregion
        #endregion

        #region Methods

        #region Menu
        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            EM_EditorTool_LogTemplatesWindow window = GetWindow<EM_EditorTool_LogTemplatesWindow>();
            window.titleContent = new GUIContent("Emergent Mechanics Log Templates (for village stress test)");
            window.minSize = new Vector2(420f, 320f);
        }
        #endregion

        #region UI
        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 8f;
            root.style.paddingRight = 8f;
            root.style.paddingTop = 8f;
            root.style.paddingBottom = 8f;

            Label header = new Label("Emergence Debug Templates");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            templatesField = new ObjectField("Templates");
            templatesField.objectType = typeof(EM_DebugMessageTemplates);
            templatesField.tooltip = "Assign or create the template asset used to format debug HUD messages.";
            templatesField.RegisterValueChangedCallback(OnTemplatesChanged);
            root.Add(templatesField);

            Button createButton = new Button(CreateTemplatesAsset);
            createButton.text = "Create Templates Asset";
            createButton.style.marginTop = 4f;
            root.Add(createButton);

            statusLabel = new Label();
            statusLabel.style.marginTop = 4f;
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            root.Add(statusLabel);

            inspectorRoot = new VisualElement();
            inspectorRoot.style.flexGrow = 1f;
            inspectorRoot.style.marginTop = 6f;
            root.Add(inspectorRoot);

            RefreshInspector();
        }

        private void OnTemplatesChanged(ChangeEvent<Object> changeEvent)
        {
            RefreshInspector();
        }

        private void RefreshInspector()
        {
            inspectorRoot.Clear();

            EM_DebugMessageTemplates templates = templatesField.value as EM_DebugMessageTemplates;

            if (templates == null)
            {
                statusLabel.text = "Select a templates asset to edit.";
                return;
            }

            statusLabel.text = string.Empty;
            InspectorElement inspector = new InspectorElement(templates);
            inspectorRoot.Add(inspector);
        }
        #endregion

        #region Asset Creation
        private void CreateTemplatesAsset()
        {
            EnsureFolderExists(DefaultFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultAssetPath);
            EM_DebugMessageTemplates asset = CreateInstance<EM_DebugMessageTemplates>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            templatesField.value = asset;
            Selection.activeObject = asset;
            RefreshInspector();
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
        #endregion

        #endregion
    }
}
