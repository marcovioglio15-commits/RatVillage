using UnityEditor;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Presets
        private void CleanAndGeneratePreset()
        {
            bool confirmed = EditorUtility.DisplayDialog(WindowTitle,
                "This will delete Emergence scriptables (library, definitions, ids, schedule) in the selected root folder and regenerate new ones. Continue?",
                "Clean + Generate", "Cancel");

            if (!confirmed)
                return;

            int deleted = EM_StudioPresetGenerator.CleanEmergenceScriptables(rootFolder);
            EM_MechanicLibrary createdLibrary;
            EM_NpcSchedulePreset createdSchedule;
            bool generated = EM_StudioPresetGenerator.GeneratePreset(selectedPreset, rootFolder, out createdLibrary, out createdSchedule);

            if (generated)
            {
                library = createdLibrary;

                if (libraryField != null)
                    libraryField.SetValueWithoutNotify(library);

                Selection.activeObject = library;

                if (createdSchedule != null)
                    EditorGUIUtility.PingObject(createdSchedule);

                RefreshAll();

                if (presetStatusLabel != null)
                    presetStatusLabel.text = "Generated preset. Deleted assets: " + deleted;
            }
            else
            {
                if (presetStatusLabel != null)
                    presetStatusLabel.text = "Preset generation failed.";
            }
        }
        #endregion
    }
}
