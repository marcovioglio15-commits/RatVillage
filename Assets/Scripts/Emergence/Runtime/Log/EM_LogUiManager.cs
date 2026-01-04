using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace EmergentMechanics
{
    public sealed partial class EM_LogUiManager : MonoBehaviour
    {
        #region Fields

        #region Constants
        private const float DefaultRefreshInterval = 0.25f;
        private const int DefaultMaxLogLines = 40;
        private const int DefaultMaxLogCharacters = 4000;
        private const string DefaultTimeLabelTemplate = "Time of Day: {time}";
        private const string DefaultSignalEmittedTemplate = "[{time}] {subject} emitted {signal} ({context}) value {value}.";
        private const string DefaultIntentCreatedTemplate = "[{time}] {subject} created intent {intent} for {need} ({resource}) amount {value} urgency {delta}.";
        private const string DefaultEffectAppliedTemplate = "[{time}] {subject} applied {effect} on {target} ({parameter}/{context}) delta {delta} (from {before} to {after}).";
        private const string DefaultInteractionAttemptTemplate = "[{time}] {subject} attempts to resolve {need} using {resource}.";
        private const string DefaultInteractionSuccessTemplate = "[{time}] {subject} obtained {resource} from {target} for {need} (amount {value}).";
        private const string DefaultInteractionFailTemplate = "[{time}] {subject} failed to resolve {need} using {resource} (reason: {reason}).";
        private const string DefaultScheduleWindowTemplate = "[{time}] {subject} started activity {activity}.";
        private const string DefaultScheduleEndTemplate = "[{time}] {subject} ended activity {activity}.";
        private const string DefaultScheduleTickTemplate = "[{time}] {subject} activity {activity} tick (value {value}).";
        #endregion

        #region Serialized
        #region References
        [Tooltip("TMP label used to display the current time of day.")]
        [Header("References")]
        [SerializeField] private TMP_Text timeLabel;

        [Tooltip("ScrollRect that hosts the log content list.")]
        [SerializeField] private ScrollRect logScrollRect;

        [Tooltip("Content transform that receives log line instances.")]
        [SerializeField] private RectTransform logContent;

        [Tooltip("Prefab used to render a single log line. This should be a TMP_Text object.")]
        [SerializeField] private TMP_Text logLinePrefab;

        [Tooltip("Templates that format debug messages and the time label.")]
        [SerializeField] private EM_DebugMessageTemplates templates;
        #endregion

        #region Behavior
        [Tooltip("Refresh interval in seconds for polling the ECS debug buffers.")]
        [Header("Behavior")]
        [SerializeField] private float refreshInterval = DefaultRefreshInterval;

        [Tooltip("Maximum number of log lines kept in the UI.")]
        [SerializeField] private int maxLogLines = DefaultMaxLogLines;

        [Tooltip("Maximum number of characters kept in the log text. Set to 0 to disable the character limit.")]
        [SerializeField] private int maxLogCharacters = DefaultMaxLogCharacters;

        [Tooltip("When enabled, the scroll view stays pinned to the newest log entry.")]
        [SerializeField] private bool autoScroll = true;
        #endregion

        #region Filtering
        [Tooltip("Settings asset that filters which debug events are shown in the log.")]
        [Header("Filtering")]
        [SerializeField] private EM_DebugLogSettings logSettings;
        #endregion
        #endregion

        #region Lookup
        private readonly List<string> logLines = new List<string>();
        private readonly List<TMP_Text> linePool = new List<TMP_Text>();
        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery debugQuery;
        private EntityQuery clockQuery;
        private float nextRefreshTime;
        private int lastEventIndex;
        private int lastFilterSignature;
        private bool hasQueries;
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle Lyfecicle
        private void OnEnable()
        {
            nextRefreshTime = 0f;
            lastEventIndex = 0;
            lastFilterSignature = int.MinValue;
            logLines.Clear();

            for (int i = 0; i < linePool.Count; i++)
            {
                if (linePool[i] == null)
                    continue;

                if (linePool[i].gameObject.activeSelf)
                    linePool[i].gameObject.SetActive(false);
            }
        }

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

        #region Update
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

        private void UpdateLog()
        {
            if (logContent == null || logLinePrefab == null)
                return;

            if (!hasQueries)
                return;

            bool filterChanged = UpdateFilterSignature();

            NativeArray<Entity> entities = debugQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
            {
                entities.Dispose();
                return;
            }

            Entity debugEntity = entities[0];
            entities.Dispose();

            DynamicBuffer<EM_Component_Event> buffer = entityManager.GetBuffer<EM_Component_Event>(debugEntity);

            if (filterChanged)
            {
                lastEventIndex = 0;
                logLines.Clear();
            }

            if (lastEventIndex > buffer.Length)
                lastEventIndex = 0;

            bool appended = false;

            for (int i = lastEventIndex; i < buffer.Length; i++)
            {
                EM_Component_Event debugEvent = buffer[i];

                if (!ShouldIncludeEvent(debugEvent))
                    continue;

                string line = FormatEvent(debugEvent);

                if (string.IsNullOrEmpty(line))
                    continue;

                logLines.Add(line);
                appended = true;
            }

            lastEventIndex = buffer.Length;

            bool trimmed = TrimLogLines();

            if (!appended && !trimmed && !filterChanged)
                return;
            
            RefreshLogView();
        }
        #endregion

        #endregion

    }
}
