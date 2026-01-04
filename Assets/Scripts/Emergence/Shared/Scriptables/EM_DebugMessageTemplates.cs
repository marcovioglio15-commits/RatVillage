using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Log Templates", fileName = "EM_LogTemplates")]
    public sealed class EM_DebugMessageTemplates : ScriptableObject
    {
        #region Serialized
        #region Time Label
        [Tooltip("Template for the time-of-day label shown in the debug HUD. Token: {time} inserts the formatted HH:MM value from the society clock.")]
        [Header("Time Label")]
        [TextArea(2, 3)]
        [SerializeField] private string timeLabelTemplate = "Time of Day: {time}";
        #endregion

        #region Signals
        [Tooltip("Template used when a signal is emitted. Tokens: {time} (HH:MM), {subject}, {target}, {signal}, {context}, {value}.")]
        [Header("Signals")]
        [TextArea(2, 4)]
        [SerializeField] private string signalEmittedTemplate = "[{time}] {subject} emitted {signal} ({context}) value {value}.";
        #endregion

        #region Intents
        [Tooltip("Template used when a new intent is created. Tokens: {time}, {subject}, {intent}, {need}, {resource}, {value} (desired amount), {delta} (urgency).")]
        [Header("Intents")]
        [TextArea(2, 4)]
        [SerializeField] private string intentCreatedTemplate = "[{time}] {subject} created intent {intent} for {need} ({resource}) amount {value} urgency {delta}.";
        #endregion

        #region Effects
        [Tooltip("Template used when an effect is applied. Tokens: {time}, {subject}, {target}, {effect}, {parameter}, {context}, {delta}, {before}, {after}.")]
        [Header("Effects")]
        [TextArea(2, 4)]
        [SerializeField] private string effectAppliedTemplate = "[{time}] {subject} applied {effect} on {target} ({parameter}/{context}) delta {delta} (from {before} to {after}).";
        #endregion

        #region Interactions
        [Tooltip("Template used when an interaction attempt starts. Tokens: {time}, {subject}, {target}, {need}, {resource}, {value}.")]
        [Header("Interactions")]
        [TextArea(2, 4)]
        [SerializeField] private string interactionAttemptTemplate = "[{time}] {subject} attempts to resolve {need} using {resource}.";

        [Tooltip("Template used when an interaction succeeds. Tokens: {time}, {subject}, {target}, {need}, {resource}, {value} (transfer amount).")]
        [TextArea(2, 4)]
        [SerializeField] private string interactionSuccessTemplate = "[{time}] {subject} obtained {resource} from {target} for {need} (amount {value}).";

        [Tooltip("Template used when an interaction fails. Tokens: {time}, {subject}, {target}, {need}, {resource}, {reason}.")]
        [TextArea(2, 4)]
        [SerializeField] private string interactionFailTemplate = "[{time}] {subject} failed to resolve {need} using {resource} (reason: {reason}).";
        #endregion

        #region Schedule
        [Tooltip("Template used when an NPC schedule enters a new activity. Tokens: {time} (HH:MM), {subject}, {society}, {activity}, {value}.")]
        [Header("Schedule")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleWindowTemplate = "[{time}] {subject} started activity {activity}.";

        [Tooltip("Template used when an NPC schedule ends an activity. Tokens: {time} (HH:MM), {subject}, {society}, {activity}, {value}.")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleEndTemplate = "[{time}] {subject} ended activity {activity}.";

        [Tooltip("Template used for schedule tick events emitted during an activity. Tokens: {time} (HH:MM), {subject}, {society}, {activity}, {value}.")]
        [TextArea(2, 4)]
        [SerializeField] private string scheduleTickTemplate = "[{time}] {subject} activity {activity} tick (value {value}).";
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

        public string SignalEmittedTemplate
        {
            get
            {
                return signalEmittedTemplate;
            }
        }

        public string IntentCreatedTemplate
        {
            get
            {
                return intentCreatedTemplate;
            }
        }

        public string EffectAppliedTemplate
        {
            get
            {
                return effectAppliedTemplate;
            }
        }

        public string InteractionAttemptTemplate
        {
            get
            {
                return interactionAttemptTemplate;
            }
        }

        public string InteractionSuccessTemplate
        {
            get
            {
                return interactionSuccessTemplate;
            }
        }

        public string InteractionFailTemplate
        {
            get
            {
                return interactionFailTemplate;
            }
        }

        public string ScheduleWindowTemplate
        {
            get
            {
                return scheduleWindowTemplate;
            }
        }

        public string ScheduleEndTemplate
        {
            get
            {
                return scheduleEndTemplate;
            }
        }

        public string ScheduleTickTemplate
        {
            get
            {
                return scheduleTickTemplate;
            }
        }
        #endregion
    }
}
