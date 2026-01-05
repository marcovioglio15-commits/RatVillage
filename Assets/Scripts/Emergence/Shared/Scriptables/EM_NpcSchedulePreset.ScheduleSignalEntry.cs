using System;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcSchedulePreset
    {
        #region Nested Types
        // Serialized signal ids for a schedule activity.
        [Serializable]
        public struct ScheduleSignalEntry
        {
            #region Data
            [Tooltip("Id definition for the signal emitted when the activity starts.")]
            [EM_IdSelector(EM_IdCategory.Signal)]
            [SerializeField] private EM_IdDefinition startSignalIdDefinition;

            [Tooltip("Legacy start signal id string (auto-migrated when missing an id definition).")]
            [SerializeField]
            [HideInInspector] private string startSignalId;

            [Tooltip("Id definition for the signal emitted each tick during the activity.")]
            [EM_IdSelector(EM_IdCategory.Signal)]
            [SerializeField] private EM_IdDefinition tickSignalIdDefinition;

            [Tooltip("Legacy tick signal id string (auto-migrated when missing an id definition).")]
            [SerializeField]
            [HideInInspector] private string tickSignalId;

            [Tooltip("Interval in hours for activity tick signals. Set to 0 to disable ticks.")]
            [SerializeField] private float tickIntervalHours;

            [Tooltip("Curve applied across the activity window (0-1 time). Value scales tick signal magnitude.")]
            [SerializeField] private AnimationCurve tickSignalCurve;
            #endregion

            #region Properties
            public string StartSignalId
            {
                get
                {
                    return EM_IdUtility.ResolveId(startSignalIdDefinition, startSignalId);
                }
            }

            public string TickSignalId
            {
                get
                {
                    return EM_IdUtility.ResolveId(tickSignalIdDefinition, tickSignalId);
                }
            }

            public EM_IdDefinition StartSignalIdDefinition
            {
                get
                {
                    return startSignalIdDefinition;
                }
            }

            public EM_IdDefinition TickSignalIdDefinition
            {
                get
                {
                    return tickSignalIdDefinition;
                }
            }

            public float TickIntervalHours
            {
                get
                {
                    return tickIntervalHours;
                }
            }

            public AnimationCurve TickSignalCurve
            {
                get
                {
                    return tickSignalCurve;
                }
            }
            #endregion

            #region Methods
            public static ScheduleSignalEntry Create(EM_IdDefinition startDefinition, string startLegacy,
                EM_IdDefinition tickDefinition, string tickLegacy, float tickInterval, AnimationCurve tickCurve)
            {
                ScheduleSignalEntry entry = new ScheduleSignalEntry
                {
                    startSignalIdDefinition = startDefinition,
                    startSignalId = startLegacy,
                    tickSignalIdDefinition = tickDefinition,
                    tickSignalId = tickLegacy,
                    tickIntervalHours = tickInterval,
                    tickSignalCurve = tickCurve
                };

                return entry;
            }

            public bool EnsureDefaults()
            {
                if (tickIntervalHours >= 0f)
                    return false;

                tickIntervalHours = 0f;
                return true;
            }
            #endregion
        }
        #endregion
    }
}
