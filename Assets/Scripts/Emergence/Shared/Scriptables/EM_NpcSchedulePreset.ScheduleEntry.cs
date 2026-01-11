using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcSchedulePreset
    {
        #region Nested Types
        // Serialized definition of a trade-allowed need for an activity.
        [Serializable]
        public struct ScheduleTradeNeedEntry
        {
            #region Data
            [Tooltip("Id definition for the need that can be resolved via trade during this activity.")]
            [EM_IdSelector(EM_IdCategory.Need)]
            [SerializeField] private EM_IdDefinition needIdDefinition;

            [Tooltip("Legacy need id string (auto-migrated when missing an id definition).")]
            [SerializeField]
            [HideInInspector] private string needId;
            #endregion

            #region Properties
            public string NeedId
            {
                get
                {
                    return EM_IdUtility.ResolveId(needIdDefinition, needId);
                }
            }

            public EM_IdDefinition NeedIdDefinition
            {
                get
                {
                    return needIdDefinition;
                }
            }
            #endregion
        }

        // Serialized definition of a schedule activity window.
        [Serializable]
        public struct ScheduleEntry
        {
            #region Data
            [Tooltip("Id definition for this activity. Keep unique within the preset for overrides and debug logs.")]
            [EM_IdSelector(EM_IdCategory.Activity)]
            [SerializeField] private EM_IdDefinition activityIdDefinition;

            [Tooltip("Legacy activity id string (auto-migrated when missing an id definition).")]
            [SerializeField]
            [HideInInspector] private string activityId;

            [Tooltip("Location definition required before the activity can start. Leave empty to allow the activity anywhere.")]
            [SerializeField] private EM_LocationDefinition locationDefinition;

            [Tooltip("Whether trading is allowed while performing this activity. Requires a location definition.")]
            [SerializeField] private bool tradeCapable;

            [Tooltip("Start hour for the activity (0-24).")]
            [SerializeField] private float startHour;

            [Tooltip("End hour for the activity (0-24). Can wrap over midnight.")]
            [SerializeField] private float endHour;

            [Tooltip("Enable duration mode for this activity, allowing it to run for a set time range.")]
            [SerializeField] private bool useDuration;

            [Tooltip("Minimum hours this activity stays active when duration mode is enabled.")]
            [SerializeField] private float minDurationHours;

            [Tooltip("Maximum hours this activity stays active when duration mode is enabled.")]
            [SerializeField] private float maxDurationHours;

            [Tooltip("Interval in hours for activity tick signals. Set to 0 to disable ticks.")]
            [FormerlySerializedAs("tickIntervalHours")]
            [SerializeField]
            [HideInInspector] private float legacyTickIntervalHours;

            [Tooltip("Legacy tick signal curve (auto-migrated into signal entries).")]
            [FormerlySerializedAs("tickSignalCurve")]
            [SerializeField]
            [HideInInspector] private AnimationCurve legacyTickSignalCurve;

            [Tooltip("Optional signals emitted for this activity. Each entry can define a start and/or tick signal.")]
            [SerializeField] private ScheduleSignalEntry[] signalEntries;

            [Tooltip("Trade policy for this activity.")]
            [SerializeField] private EM_ScheduleTradePolicy tradePolicy;

            [Tooltip("Needs allowed for trade when policy is AllowOnlyListed.")]
            [SerializeField] private ScheduleTradeNeedEntry[] allowedTradeNeeds;

            [Tooltip("Legacy start signal id definition (auto-migrated into signal entries).")]
            [FormerlySerializedAs("startSignalIdDefinition")]
            [SerializeField]
            [HideInInspector] private EM_IdDefinition legacyStartSignalIdDefinition;

            [Tooltip("Legacy start signal id string (auto-migrated into signal entries).")]
            [FormerlySerializedAs("startSignalId")]
            [SerializeField]
            [HideInInspector] private string legacyStartSignalId;

            [Tooltip("Legacy tick signal id definition (auto-migrated into signal entries).")]
            [FormerlySerializedAs("tickSignalIdDefinition")]
            [SerializeField]
            [HideInInspector] private EM_IdDefinition legacyTickSignalIdDefinition;

            [Tooltip("Legacy tick signal id string (auto-migrated into signal entries).")]
            [FormerlySerializedAs("tickSignalId")]
            [SerializeField]
            [HideInInspector] private string legacyTickSignalId;
            #endregion

            #region Properties
            public string ActivityId
            {
                get
                {
                    return EM_IdUtility.ResolveId(activityIdDefinition, activityId);
                }
            }

            public EM_IdDefinition ActivityIdDefinition
            {
                get
                {
                    return activityIdDefinition;
                }
            }

            public EM_LocationDefinition LocationDefinition
            {
                get
                {
                    return locationDefinition;
                }
            }

            public string LocationId
            {
                get
                {
                    if (locationDefinition == null)
                        return string.Empty;

                    return locationDefinition.Id;
                }
            }

            public bool TradeCapable
            {
                get
                {
                    return tradeCapable;
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

            public ScheduleSignalEntry[] SignalEntries
            {
                get
                {
                    return signalEntries;
                }
            }

            public EM_ScheduleTradePolicy TradePolicy
            {
                get
                {
                    return tradePolicy;
                }
            }

            public ScheduleTradeNeedEntry[] AllowedTradeNeeds
            {
                get
                {
                    return allowedTradeNeeds;
                }
            }

            public bool UseDuration
            {
                get
                {
                    return useDuration;
                }
            }

            public float MinDurationHours
            {
                get
                {
                    return minDurationHours;
                }
            }

            public float MaxDurationHours
            {
                get
                {
                    return maxDurationHours;
                }
            }
            #endregion

            #region Methods
            public bool EnsureDefaults()
            {
                bool updated = false;

                if (signalEntries == null)
                {
                    signalEntries = new ScheduleSignalEntry[0];
                    updated = true;
                }

                if (allowedTradeNeeds == null)
                {
                    allowedTradeNeeds = new ScheduleTradeNeedEntry[0];
                    updated = true;
                }

                if (locationDefinition == null && tradeCapable)
                {
                    tradeCapable = false;
                    updated = true;
                }

                if (useDuration)
                {
                    if (minDurationHours < 0f)
                    {
                        minDurationHours = 0f;
                        updated = true;
                    }

                    if (maxDurationHours < 0f)
                    {
                        maxDurationHours = 0f;
                        updated = true;
                    }

                    if (maxDurationHours < minDurationHours)
                    {
                        maxDurationHours = minDurationHours;
                        updated = true;
                    }
                }
                else
                {
                    if (minDurationHours != 0f)
                    {
                        minDurationHours = 0f;
                        updated = true;
                    }

                    if (maxDurationHours != 0f)
                    {
                        maxDurationHours = 0f;
                        updated = true;
                    }
                }

                if (legacyTickIntervalHours < 0f)
                {
                    legacyTickIntervalHours = 0f;
                    updated = true;
                }

                bool hasLegacyStart = EM_IdUtility.HasId(legacyStartSignalIdDefinition, legacyStartSignalId);
                bool hasLegacyTick = EM_IdUtility.HasId(legacyTickSignalIdDefinition, legacyTickSignalId);
                bool hasLegacyInterval = legacyTickIntervalHours > 0f;
                bool hasLegacyCurve = legacyTickSignalCurve != null && legacyTickSignalCurve.length > 0;
                bool hasLegacyData = hasLegacyStart || hasLegacyTick || hasLegacyInterval || hasLegacyCurve;

                if (signalEntries.Length == 0 && hasLegacyData)
                {
                    signalEntries = new ScheduleSignalEntry[1];
                    signalEntries[0] = ScheduleSignalEntry.Create(legacyStartSignalIdDefinition, legacyStartSignalId,
                        legacyTickSignalIdDefinition, legacyTickSignalId, legacyTickIntervalHours, legacyTickSignalCurve);
                    updated = true;
                    ClearLegacySignalData();
                }
                else if (signalEntries.Length > 0 && hasLegacyData)
                {
                    updated |= ClearLegacySignalData();
                }

                for (int i = 0; i < signalEntries.Length; i++)
                {
                    ScheduleSignalEntry signalEntry = signalEntries[i];
                    bool signalUpdated = signalEntry.EnsureDefaults();

                    if (!signalUpdated)
                        continue;

                    signalEntries[i] = signalEntry;
                    updated = true;
                }

                return updated;
            }

            private bool ClearLegacySignalData()
            {
                bool cleared = false;

                if (legacyStartSignalIdDefinition != null)
                {
                    legacyStartSignalIdDefinition = null;
                    cleared = true;
                }

                if (!string.IsNullOrWhiteSpace(legacyStartSignalId))
                {
                    legacyStartSignalId = string.Empty;
                    cleared = true;
                }

                if (legacyTickSignalIdDefinition != null)
                {
                    legacyTickSignalIdDefinition = null;
                    cleared = true;
                }

                if (!string.IsNullOrWhiteSpace(legacyTickSignalId))
                {
                    legacyTickSignalId = string.Empty;
                    cleared = true;
                }

                if (legacyTickIntervalHours != 0f)
                {
                    legacyTickIntervalHours = 0f;
                    cleared = true;
                }

                if (legacyTickSignalCurve != null)
                {
                    legacyTickSignalCurve = null;
                    cleared = true;
                }

                return cleared;
            }
            #endregion
        }
        #endregion
    }
}
