using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Displays Emergence debug information in a TMP-based HUD.
    /// </summary>
    public sealed partial class EmergenceDebugUiManager : MonoBehaviour
    {
        #region Constants
        private const float DefaultRefreshInterval = 0.25f;
        private const int DefaultMaxLogLines = 40;
        private const int DefaultMaxLogCharacters = 4000;
        private const string DefaultTimeLabelTemplate = "Time of Day: {time}";
        private const string DefaultScheduleWindowTemplate = "[{time}] Society {society} entered {window} schedule window; window-driven rules can now activate.";
        private const string DefaultScheduleTickTemplate = "[{time}] Society {society} emitted {window} schedule tick (value {value}).";
        private const string DefaultTradeAttemptTemplate = "[{time}] {subject} paused current activity to seek {resource} for {need} from {target} (chance {value}).";
        private const string DefaultTradeSuccessTemplate = "[{time}] {subject} obtained {resource} from {target} to satisfy {need} (amount {value}).";
        private const string DefaultTradeFailTemplate = "[{time}] {subject} could not obtain {resource} for {need} (reason: {reason}).";
        private const string DefaultDistributionTemplate = "[{time}] Society {society} distributed {resource} to {subject} to satisfy {need} (amount {value}).";
        #endregion

        #region Serialized
        // Serialized references
        #region Serialized - References
        [Tooltip("TMP label used to display the current time of day.")]
        [Header("References")]
        [SerializeField] private TMP_Text timeLabel;

        [Tooltip("TMP label used to display the Emergence debug log.")]
        [SerializeField] private TMP_Text logLabel;

        [Tooltip("Templates that format debug messages and the time label.")]
        [SerializeField] private EM_DebugMessageTemplates templates;
        #endregion

        // Serialized behavior
        #region Serialized - Behavior
        [Tooltip("Refresh interval in seconds for polling the ECS debug buffers.")]
        [Header("Behavior")]
        [SerializeField] private float refreshInterval = DefaultRefreshInterval;

        [Tooltip("Maximum number of log lines kept in the UI.")]
        [SerializeField] private int maxLogLines = DefaultMaxLogLines;

        [Tooltip("Maximum number of characters kept in the log text. Set to 0 to disable the character limit.")]
        [SerializeField] private int maxLogCharacters = DefaultMaxLogCharacters;
        #endregion
        #endregion

        #region State
        private readonly List<string> logLines = new List<string>();
        private readonly StringBuilder logBuilder = new StringBuilder(2048);
        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery debugQuery;
        private EntityQuery clockQuery;
        private float nextRefreshTime;
        private int lastEventIndex;
        private bool hasQueries;
        #endregion

        #region Unity
        /// <summary>
        /// Resets refresh state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            nextRefreshTime = 0f;
            lastEventIndex = 0;
        }

        /// <summary>
        /// Updates time and log labels at the configured refresh interval.
        /// </summary>
        private void Update()
        {
            if (!EnsureWorld())
                return;

            float interval = refreshInterval;

            if (interval <= 0f)
                interval = DefaultRefreshInterval;

            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + interval;

            UpdateTimeLabel();
            UpdateLog();
        }
        #endregion

        #region Refresh
        /// <summary>
        /// Updates the time-of-day label.
        /// </summary>
        private void UpdateTimeLabel()
        {
            if (timeLabel == null)
                return;

            float timeOfDay = GetSocietyTime();
            string timeString = FormatTimeOfDay(timeOfDay);
            string template = GetTimeLabelTemplate();
            string formatted = template.Replace("{time}", timeString);

            if (timeLabel.text == formatted)
                return;

            timeLabel.text = formatted;
        }

        /// <summary>
        /// Updates the log label with newly received debug events.
        /// </summary>
        private void UpdateLog()
        {
            if (logLabel == null)
                return;

            if (!hasQueries)
                return;

            NativeArray<Entity> entities = debugQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
            {
                entities.Dispose();
                return;
            }

            Entity debugEntity = entities[0];
            entities.Dispose();

            DynamicBuffer<EmergenceDebugEvent> buffer = entityManager.GetBuffer<EmergenceDebugEvent>(debugEntity);

            if (lastEventIndex > buffer.Length)
                lastEventIndex = 0;

            bool appended = false;

            for (int i = lastEventIndex; i < buffer.Length; i++)
            {
                string line = FormatEvent(buffer[i]);

                if (string.IsNullOrEmpty(line))
                    continue;

                logLines.Add(line);
                appended = true;
            }

            lastEventIndex = buffer.Length;

            bool trimmed = TrimLogLines();

            if (!appended && !trimmed)
                return;

            logBuilder.Clear();

            for (int i = 0; i < logLines.Count; i++)
            {
                if (i > 0)
                    logBuilder.Append('\n');

                logBuilder.Append(logLines[i]);
            }

            logLabel.text = logBuilder.ToString();
        }
        #endregion

    }
}
