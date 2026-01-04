using System;
using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/NPC Schedule Preset", fileName = "EM_NpcSchedulePreset")]
    public sealed class EM_NpcSchedulePreset : ScriptableObject
    {
        #region Nested Types
        // Serialized definition of a schedule activity window.
        [Serializable]
        public struct ScheduleEntry
        {
            #region Data
            [Tooltip("Stable id for this activity. Keep unique within the preset for overrides and debug logs.")]
            [SerializeField] private string activityId;

            [Tooltip("Start hour for the activity (0-24).")]
            [SerializeField] private float startHour;

            [Tooltip("End hour for the activity (0-24). Can wrap over midnight.")]
            [SerializeField] private float endHour;

            [Tooltip("Interval in hours for activity tick signals. Set to 0 to disable ticks.")]
            [SerializeField] private float tickIntervalHours;

            [Tooltip("Signal emitted when the activity starts.")]
            [SerializeField] private string startSignalId;

            [Tooltip("Signal emitted each tick during the activity.")]
            [SerializeField] private string tickSignalId;

            [Tooltip("Curve applied across the activity window (0-1 time). Value scales tick signal magnitude.")]
            [SerializeField] private AnimationCurve tickSignalCurve;
            #endregion

            #region Properties
            public string ActivityId
            {
                get
                {
                    return activityId;
                }
            }

            public float StartHour
            {
                get
                {
                    return startHour;
                }
            }

            public float EndHour
            {
                get
                {
                    return endHour;
                }
            }

            public float TickIntervalHours
            {
                get
                {
                    return tickIntervalHours;
                }
            }

            public string StartSignalId
            {
                get
                {
                    return startSignalId;
                }
            }

            public string TickSignalId
            {
                get
                {
                    return tickSignalId;
                }
            }

            public AnimationCurve ActivityCurve
            {
                get
                {
                    return tickSignalCurve;
                }
            }
            #endregion
        }
        #endregion

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
    }
}
