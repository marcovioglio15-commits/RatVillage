using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Debug Message Templates", fileName = "EM_DebugMessageTemplates")]
    public sealed class EM_DebugMessageTemplates : ScriptableObject
    {
        #region Serialized
        #region Time Label
        [Tooltip("Template for the time-of-day label shown in the debug HUD. Token: {time} inserts the formatted HH:MM value from the society clock.")]
        [Header("Time Label")]
        [TextArea(2, 3)]
        [SerializeField] private string timeLabelTemplate = "Time of Day: {time}";
        #endregion

        #region Schedule
        [Tooltip("Template used when an NPC schedule enters a new activity. Tokens: {time} (HH:MM), {subject} (npc id), {society} (society id), {window} (activity id), {value} (activity strength).")]
        [Header("Schedule")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleWindowTemplate = "[{time}] {subject} started {window} activity.";

        [Tooltip("Template used for schedule tick events emitted during an activity. Tokens: {time} (HH:MM), {subject}, {society}, {window}, {value} (curve-scaled tick value).")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleTickTemplate = "[{time}] {subject} {window} activity tick (value {value}).";
        #endregion

        #region Trade
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

        #region Distribution
        [Tooltip("Template used when society resources are distributed to members. Tokens: {time}, {society}, {subject}, {need}, {resource}, {value} (transfer amount).")]
        [Header("Distribution")]
        [TextArea(2, 4)]
        [SerializeField] private string distributionTemplate = "[{time}] Society {society} distributed {resource} to {subject} to satisfy {need} (amount {value}).";
        #endregion
        #endregion

        #region Public Properties
        public string TimeLabelTemplate
        {
            get
            {
                return timeLabelTemplate;
            }
        }

        public string ScheduleWindowTemplate
        {
            get
            {
                return scheduleWindowTemplate;
            }
        }

        public string ScheduleTickTemplate
        {
            get
            {
                return scheduleTickTemplate;
            }
        }

        public string TradeAttemptTemplate
        {
            get
            {
                return tradeAttemptTemplate;
            }
        }

        public string TradeSuccessTemplate
        {
            get
            {
                return tradeSuccessTemplate;
            }
        }

        public string TradeFailTemplate
        {
            get
            {
                return tradeFailTemplate;
            }
        }

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
