using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    internal sealed class EM_EditorTool_NarrativeLogWindow : EditorWindow
    {
        #region Constants
        private const string MenuPath = "Tools/Emergence/Narrative Log";
        private const string DefaultFolder = "Assets/Scriptable Objects/Log";
        private const string DefaultTemplatesPath = "Assets/Scriptable Objects/Log/EM_NarrativeLogTemplates.asset";
        private const string DefaultSettingsPath = "Assets/Scriptable Objects/Log/EM_NarrativeLogSettings.asset";
        #endregion

        #region State
        private ObjectField templatesField;
        private ObjectField settingsField;
        private VisualElement templatesInspectorRoot;
        private VisualElement settingsInspectorRoot;
        private Label templatesStatusLabel;
        private Label settingsStatusLabel;
        #endregion

        #region Menu
        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            EM_EditorTool_NarrativeLogWindow window = GetWindow<EM_EditorTool_NarrativeLogWindow>();
            window.titleContent = new GUIContent("Emergence Narrative Log Tools");
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

            Label header = new Label("Narrative Log Assets");
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
            templatesField.objectType = typeof(EM_NarrativeLogTemplates);
            templatesField.tooltip = "Assign or create the template asset used to format narrative log messages.";
            templatesField.RegisterValueChangedCallback(OnTemplatesChanged);
            templatesSection.Add(templatesField);

            Button createTemplatesButton = new Button(CreateTemplatesAsset);
            createTemplatesButton.text = "Create Templates Asset";
            createTemplatesButton.style.marginTop = 4f;
            templatesSection.Add(createTemplatesButton);

            Button populateTemplatesButton = new Button(PopulateSampleTemplates);
            populateTemplatesButton.text = "Populate Sample Templates";
            populateTemplatesButton.tooltip = "Overwrite the template list with curated sample entries.";
            populateTemplatesButton.style.marginTop = 4f;
            templatesSection.Add(populateTemplatesButton);

            Button populateFilteredButton = new Button(PopulateFilteredTemplates);
            populateFilteredButton.text = "Populate Filtered Examples";
            populateFilteredButton.tooltip = "Overwrite the template list with filtered examples using Hunger/Water/Sleep ids.";
            populateFilteredButton.style.marginTop = 4f;
            templatesSection.Add(populateFilteredButton);

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

            Label settingsHeader = new Label("Settings");
            settingsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            settingsSection.Add(settingsHeader);

            settingsField = new ObjectField("Settings");
            settingsField.objectType = typeof(EM_NarrativeLogSettings);
            settingsField.tooltip = "Assign or create the narrative log settings asset.";
            settingsField.RegisterValueChangedCallback(OnSettingsChanged);
            settingsSection.Add(settingsField);

            Button createSettingsButton = new Button(CreateSettingsAsset);
            createSettingsButton.text = "Create Settings Asset";
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

            EM_NarrativeLogTemplates templates = templatesField.value as EM_NarrativeLogTemplates;

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

            EM_NarrativeLogSettings settings = settingsField.value as EM_NarrativeLogSettings;

            if (settings == null)
            {
                settingsStatusLabel.text = "Select a settings asset to edit.";
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

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultTemplatesPath);
            EM_NarrativeLogTemplates asset = CreateInstance<EM_NarrativeLogTemplates>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            templatesField.value = asset;
            Selection.activeObject = asset;
            RefreshTemplatesInspector();
        }

        private void PopulateSampleTemplates()
        {
            EM_NarrativeLogTemplates templates = templatesField.value as EM_NarrativeLogTemplates;

            if (templates == null)
            {
                templatesStatusLabel.text = "Select a templates asset before populating.";
                return;
            }

            bool hasExisting = templates.Templates != null && templates.Templates.Length > 0;

            if (hasExisting)
            {
                bool confirmed = EditorUtility.DisplayDialog("Overwrite Templates",
                    "This will replace the current template list with the sample templates. Continue?", "Overwrite", "Cancel");

                if (!confirmed)
                    return;
            }

            EM_NarrativeLogTemplatePresets.ApplySampleTemplates(templates);
            templatesStatusLabel.text = "Sample templates applied.";
            RefreshTemplatesInspector();
        }

        private void PopulateFilteredTemplates()
        {
            EM_NarrativeLogTemplates templates = templatesField.value as EM_NarrativeLogTemplates;

            if (templates == null)
            {
                templatesStatusLabel.text = "Select a templates asset before populating.";
                return;
            }

            bool hasExisting = templates.Templates != null && templates.Templates.Length > 0;

            if (hasExisting)
            {
                bool confirmed = EditorUtility.DisplayDialog("Overwrite Templates",
                    "This will replace the current template list with filtered examples. Continue?", "Overwrite", "Cancel");

                if (!confirmed)
                    return;
            }

            EM_NarrativeLogTemplatePresets.ApplyFilteredTemplates(templates);
            templatesStatusLabel.text = "Filtered templates applied.";
            RefreshTemplatesInspector();
        }

        private void CreateSettingsAsset()
        {
            EnsureFolderExists(DefaultFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultSettingsPath);
            EM_NarrativeLogSettings asset = CreateInstance<EM_NarrativeLogSettings>();
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
    }
}
