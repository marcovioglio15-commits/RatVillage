using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Presets Tab
        private VisualElement BuildPresetsTab()
        {
            VisualElement root = new VisualElement();
            root.name = "PresetsTab";
            root.style.flexGrow = 1f;

            Label header = new Label("Preset Generator");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(header);

            Label description = new Label("Generates a fresh library and schedule preset using the selected tuning. " +
                "Clean will remove Emergence scriptables in the root folder before generating.");
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 6f;
            root.Add(description);

            presetField = new EnumField("Preset", selectedPreset);
            presetField.tooltip = "Slow reduces sampling and schedule frequency, Aggressive increases them.";
            presetField.RegisterValueChangedCallback(OnPresetChanged);
            root.Add(presetField);

            Button cleanGenerateButton = new Button(CleanAndGeneratePreset);
            cleanGenerateButton.text = "Clean + Generate";
            cleanGenerateButton.tooltip = "Deletes Emergence scriptables in the root folder and generates new ones.";
            root.Add(cleanGenerateButton);

            presetStatusLabel = new Label();
            presetStatusLabel.style.marginTop = 6f;
            presetStatusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            root.Add(presetStatusLabel);

            return root;
        }
        #endregion

        #region Preset Events
        private void OnPresetChanged(ChangeEvent<System.Enum> changeEvent)
        {
            selectedPreset = (EM_StudioPresetType)changeEvent.newValue;
        }
        #endregion
    }
}
