using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Stores templates used to format Emergence debug messages.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Debug Message Templates", fileName = "EM_DebugMessageTemplates")]
    public sealed class EM_DebugMessageTemplates : ScriptableObject
    {
        #region Serialized
        // Serialized time label
        #region Serialized - Time Label
        [Tooltip("Template for the time-of-day label shown in the debug HUD. Token: {time} inserts the formatted HH:MM value from the society clock.")]
        [Header("Time Label")]
        [TextArea(2, 3)]
        [SerializeField] private string timeLabelTemplate = "Time of Day: {time}";
        #endregion

        // Serialized schedule
        #region Serialized - Schedule
        [Tooltip("Template used when the society schedule enters a new window (Sleep/Work/Leisure). Tokens: {time} (HH:MM), {society} (entity id), {window} (window name), {value} (window strength).")]
        [Header("Schedule")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleWindowTemplate = "[{time}] Society {society} entered {window} schedule window; window-driven rules can now activate.";

        [Tooltip("Template used for schedule tick events emitted during a window. Tokens: {time} (HH:MM), {society}, {window}, {value} (curve-scaled tick value).")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleTickTemplate = "[{time}] Society {society} emitted {window} schedule tick (value {value}).";
        #endregion

        // Serialized trade
        #region Serialized - Trade
        [Tooltip("Template used when an NPC attempts trade after the probability check. Tokens: {time}, {subject} (npc id), {target} (partner id or None), {need}, {resource}, {value} (attempt probability 0-1).")]
        [Header("Trade")]
        [TextArea(2, 4)]
        [SerializeField] private string tradeAttemptTemplate = "[{time}] {subject} paused current activity to seek {resource} for {need} from {target} (chance {value}).";

        [Tooltip("Template used when a trade succeeds. Tokens: {time}, {subject}, {target}, {need}, {resource}, {value} (transfer amount).")]
        [TextArea(2, 4)]
        [SerializeField] private string tradeSuccessTemplate = "[{time}] {subject} obtained {resource} from {target} to satisfy {need} (amount {value}).";

        [Tooltip("Template used when a trade fails. Tokens: {time}, {subject}, {target}, {need}, {resource}, {reason} (NoPartner/Rejected/NoResource).")]
        [TextArea(2, 4)]
        [SerializeField] private string tradeFailTemplate = "[{time}] {subject} could not obtain {resource} for {need} (reason: {reason}).";
        #endregion

        // Serialized distribution
        #region Serialized - Distribution
        [Tooltip("Template used when society resources are distributed to members. Tokens: {time}, {society}, {subject}, {need}, {resource}, {value} (transfer amount).")]
        [Header("Distribution")]
        [TextArea(2, 4)]
        [SerializeField] private string distributionTemplate = "[{time}] Society {society} distributed {resource} to {subject} to satisfy {need} (amount {value}).";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the time label template.
        /// </summary>
        public string TimeLabelTemplate
        {
            get
            {
                return timeLabelTemplate;
            }
        }

        /// <summary>
        /// Gets the schedule window template.
        /// </summary>
        public string ScheduleWindowTemplate
        {
            get
            {
                return scheduleWindowTemplate;
            }
        }

        /// <summary>
        /// Gets the schedule tick template.
        /// </summary>
        public string ScheduleTickTemplate
        {
            get
            {
                return scheduleTickTemplate;
            }
        }

        /// <summary>
        /// Gets the trade attempt template.
        /// </summary>
        public string TradeAttemptTemplate
        {
            get
            {
                return tradeAttemptTemplate;
            }
        }

        /// <summary>
        /// Gets the trade success template.
        /// </summary>
        public string TradeSuccessTemplate
        {
            get
            {
                return tradeSuccessTemplate;
            }
        }

        /// <summary>
        /// Gets the trade fail template.
        /// </summary>
        public string TradeFailTemplate
        {
            get
            {
                return tradeFailTemplate;
            }
        }

        /// <summary>
        /// Gets the distribution template.
        /// </summary>
        public string DistributionTemplate
        {
            get
            {
                return distributionTemplate;
            }
        }
        #endregion
    }
}
