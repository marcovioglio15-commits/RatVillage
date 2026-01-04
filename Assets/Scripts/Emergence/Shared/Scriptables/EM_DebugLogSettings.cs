using UnityEngine;

namespace EmergentMechanics
{
    public enum EM_DebugLogFilterMode
    {
        All,
        SignalsOnly,
        EventsOnly,
        ScheduleStartEndOnly,
        TradesOnly,
        Custom
    }

    [CreateAssetMenu(menuName = "Emergence/Log Settings", fileName = "EM_DebugLogSettings")]
    public sealed class EM_DebugLogSettings : ScriptableObject
    {
        #region Serialized
        #region Preset
        [Tooltip("Preset that configures which log categories are visible. Use Custom to edit individual flags.")]
        [Header("Preset")]
        [SerializeField] private EM_DebugLogFilterMode filterMode = EM_DebugLogFilterMode.All;
        #endregion

        #region Signals
        [Tooltip("Enable signal events in the log.")]
        [Header("Signals")]
        [SerializeField] private bool includeSignals = true;

        [Tooltip("Signal id prefix used to tag need-related signals. Default: \"Need.\"")]
        [SerializeField] private string needSignalPrefix = "Need.";

        [Tooltip("Include signals whose id starts with the need prefix.")]
        [SerializeField] private bool includeNeedSignals = true;

        [Tooltip("Signal id prefix used to tag trade-related signals. Default: \"Trade.\"")]
        [SerializeField] private string tradeSignalPrefix = "Trade.";

        [Tooltip("Include signals whose id starts with the trade prefix.")]
        [SerializeField] private bool includeTradeSignals = true;

        [Tooltip("Include signals that do not match the need or trade prefixes.")]
        [SerializeField] private bool includeOtherSignals = true;
        #endregion

        #region Intents
        [Tooltip("Enable intent creation events in the log.")]
        [Header("Intents")]
        [SerializeField] private bool includeIntents = true;
        #endregion

        #region Effects
        [Tooltip("Enable effect application events in the log.")]
        [Header("Effects")]
        [SerializeField] private bool includeEffects = true;
        #endregion

        #region Interactions
        [Tooltip("Enable interaction attempt events in the log.")]
        [Header("Interactions")]
        [SerializeField] private bool includeInteractionAttempts = true;

        [Tooltip("Enable interaction success events in the log.")]
        [SerializeField] private bool includeInteractionSuccess = true;

        [Tooltip("Enable interaction failure events in the log.")]
        [SerializeField] private bool includeInteractionFailures = true;
        #endregion

        #region Schedule
        [Tooltip("Enable schedule start events in the log.")]
        [Header("Schedule")]
        [SerializeField] private bool includeScheduleStart = true;

        [Tooltip("Enable schedule end events in the log.")]
        [SerializeField] private bool includeScheduleEnd = true;

        [Tooltip("Enable schedule tick events in the log.")]
        [SerializeField] private bool includeScheduleTick = true;
        #endregion
        #endregion

        #region Public Properties
        public EM_DebugLogFilterMode FilterMode
        {
            get
            {
                return filterMode;
            }
        }

        public bool IncludeSignals
        {
            get
            {
                return includeSignals;
            }
        }

        public string NeedSignalPrefix
        {
            get
            {
                return needSignalPrefix;
            }
        }

        public bool IncludeNeedSignals
        {
            get
            {
                return includeNeedSignals;
            }
        }

        public string TradeSignalPrefix
        {
            get
            {
                return tradeSignalPrefix;
            }
        }

        public bool IncludeTradeSignals
        {
            get
            {
                return includeTradeSignals;
            }
        }

        public bool IncludeOtherSignals
        {
            get
            {
                return includeOtherSignals;
            }
        }

        public bool IncludeIntents
        {
            get
            {
                return includeIntents;
            }
        }

        public bool IncludeEffects
        {
            get
            {
                return includeEffects;
            }
        }

        public bool IncludeInteractionAttempts
        {
            get
            {
                return includeInteractionAttempts;
            }
        }

        public bool IncludeInteractionSuccess
        {
            get
            {
                return includeInteractionSuccess;
            }
        }

        public bool IncludeInteractionFailures
        {
            get
            {
                return includeInteractionFailures;
            }
        }

        public bool IncludeScheduleStart
        {
            get
            {
                return includeScheduleStart;
            }
        }

        public bool IncludeScheduleEnd
        {
            get
            {
                return includeScheduleEnd;
            }
        }

        public bool IncludeScheduleTick
        {
            get
            {
                return includeScheduleTick;
            }
        }
        #endregion
    }
}
