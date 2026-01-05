using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/NPC Schedule Preset", fileName = "EM_NpcSchedulePreset")]
    public sealed partial class EM_NpcSchedulePreset : ScriptableObject
    {
        #region Fields
        // Serialized preset configuration.
        #region Serialized
        #region Schedule
        [Tooltip("Sample count used to bake activity curves. Higher values are smoother but use more memory.")]
        [Header("Schedule")]
        [SerializeField] private int curveSamples = 32;

        [Tooltip("Ordered list of activity windows for the daily schedule.")]
        [SerializeField] private ScheduleEntry[] entries = new ScheduleEntry[0];
        #endregion
        #endregion
        #endregion

        #region Public Properties
        public int CurveSamples
        {
            get
            {
                return curveSamples;
            }
        }

        public ScheduleEntry[] Entries
        {
            get
            {
                return entries;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void OnValidate()
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                ScheduleEntry entry = entries[i];
                bool updated = entry.EnsureDefaults();

                if (!updated)
                    continue;

                entries[i] = entry;
            }
        }
        #endregion
    }
}
