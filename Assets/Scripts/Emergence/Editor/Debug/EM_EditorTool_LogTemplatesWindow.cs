using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    /// <summary>
    /// Editor window for creating and editing debug message templates and log filters.
    /// </summary>
    internal sealed class EM_EditorTool_LogTemplatesWindow : EditorWindow
    {
        #region Fields
        #region Constants
        private const string MenuPath = "Tools/Emergence/Debug Templates";
        private const string DefaultFolder = "Assets/Scriptable Objects/Debug";
        private const string DefaultAssetPath = "Assets/Scriptable Objects/Debug/EM_DebugMessageTemplates.asset";
        private const string DefaultSettingsAssetPath = "Assets/Scriptable Objects/Debug/EM_DebugLogSettings.asset";
        #endregion

        #region Lookup
        private ObjectField templatesField;
        private ObjectField settingsField;
        private VisualElement templatesInspectorRoot;
        private VisualElement settingsInspectorRoot;
        private Label templatesStatusLabel;
        private Label settingsStatusLabel;
        #endregion
        #endregion

        #region Methods

        #region Menu
        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            EM_EditorTool_LogTemplatesWindow window = GetWindow<EM_EditorTool_LogTemplatesWindow>();
            window.titleContent = new GUIContent("Emergent Mechanics Log Tools (for village stress test)");
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

            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1f;
            root.Add(scrollView);

            Label header = new Label("Emergence Debug Log Tools");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            scrollView.Add(header);

            VisualElement templatesSection = new VisualElement();
            templatesSection.style.flexDirection = FlexDirection.Column;
            templatesSection.style.marginTop = 8f;
            scrollView.Add(templatesSection);

            Label templatesHeader = new Label("Templates");
            templatesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            templatesSection.Add(templatesHeader);

            templatesField = new ObjectField("Templates");
            templatesField.objectType = typeof(EM_DebugMessageTemplates);
            templatesField.tooltip = "Assign or create the template asset used to format debug HUD messages.";
            templatesField.RegisterValueChangedCallback(OnTemplatesChanged);
            templatesSection.Add(templatesField);

            Button createTemplatesButton = new Button(CreateTemplatesAsset);
            createTemplatesButton.text = "Create Templates Asset";
            createTemplatesButton.style.marginTop = 4f;
            templatesSection.Add(createTemplatesButton);

            templatesStatusLabel = new Label();
            templatesStatusLabel.style.marginTop = 4f;
            templatesStatusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            templatesSection.Add(templatesStatusLabel);

            templatesInspectorRoot = new VisualElement();
            templatesInspectorRoot.style.marginTop = 6f;
            templatesSection.Add(templatesInspectorRoot);

            VisualElement settingsSection = new VisualElement();
            settingsSection.style.flexDirection = FlexDirection.Column;
            settingsSection.style.marginTop = 10f;
            scrollView.Add(settingsSection);

            Label settingsHeader = new Label("Log Filters");
            settingsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            settingsSection.Add(settingsHeader);

            settingsField = new ObjectField("Settings");
            settingsField.objectType = typeof(EM_DebugLogSettings);
            settingsField.tooltip = "Assign or create the log settings asset used to filter debug HUD messages.";
            settingsField.RegisterValueChangedCallback(OnSettingsChanged);
            settingsSection.Add(settingsField);

            Button createSettingsButton = new Button(CreateSettingsAsset);
            createSettingsButton.text = "Create Log Settings Asset";
            createSettingsButton.style.marginTop = 4f;
            settingsSection.Add(createSettingsButton);

            settingsStatusLabel = new Label();
            settingsStatusLabel.style.marginTop = 4f;
            settingsStatusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            settingsSection.Add(settingsStatusLabel);

            settingsInspectorRoot = new VisualElement();
            settingsInspectorRoot.style.marginTop = 6f;
            settingsSection.Add(settingsInspectorRoot);

            RefreshTemplatesInspector();
            RefreshSettingsInspector();
        }

        private void OnTemplatesChanged(ChangeEvent<Object> changeEvent)
        {
            RefreshTemplatesInspector();
        }

        private void OnSettingsChanged(ChangeEvent<Object> changeEvent)
        {
            RefreshSettingsInspector();
        }

        private void RefreshTemplatesInspector()
        {
            templatesInspectorRoot.Clear();

            EM_DebugMessageTemplates templates = templatesField.value as EM_DebugMessageTemplates;

            if (templates == null)
            {
                templatesStatusLabel.text = "Select a templates asset to edit.";
                return;
            }

            templatesStatusLabel.text = string.Empty;
            InspectorElement inspector = new InspectorElement(templates);
            templatesInspectorRoot.Add(inspector);
        }

        private void RefreshSettingsInspector()
        {
            settingsInspectorRoot.Clear();

            EM_DebugLogSettings settings = settingsField.value as EM_DebugLogSettings;

            if (settings == null)
            {
                settingsStatusLabel.text = "Select a log settings asset to edit.";
                return;
            }

            settingsStatusLabel.text = string.Empty;
            InspectorElement inspector = new InspectorElement(settings);
            settingsInspectorRoot.Add(inspector);
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
            RefreshTemplatesInspector();
        }

        private void CreateSettingsAsset()
        {
            EnsureFolderExists(DefaultFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultSettingsAssetPath);
            EM_DebugLogSettings asset = CreateInstance<EM_DebugLogSettings>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            settingsField.value = asset;
            Selection.activeObject = asset;
            RefreshSettingsInspector();
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
