using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    [CustomEditor(typeof(EM_NpcSchedulePreset))]
    public sealed class EM_NpcSchedulePresetEditor : Editor
    {
        #region Unity Lifecycle
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
