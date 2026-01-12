using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace EmergentMechanics
{
    [DisallowMultipleComponent]
    public sealed partial class EM_NarrativeLogUiManager : MonoBehaviour
    {
        #region Constants
        private const float DefaultRefreshInterval = 0.25f;
        private const int DefaultMaxLogLines = 50;
        private const int DefaultMaxLogCharacters = 6000;
        #endregion

        #region Serialized
        #region References
        [Tooltip("ScrollRect that hosts the narrative log content list.")]
        [Header("References")]
        [SerializeField] private ScrollRect logScrollRect;

        [Tooltip("Content transform that receives narrative log line instances.")]
        [SerializeField] private RectTransform logContent;

        [Tooltip("Prefab used to render a single narrative log line. This should be a TMP_Text object.")]
        [SerializeField] private TMP_Text logLinePrefab;
        #endregion

        #region Behavior
        [Tooltip("Refresh interval in seconds for polling narrative log entries.")]
        [Header("Behavior")]
        [SerializeField] private float refreshInterval = DefaultRefreshInterval;

        [Tooltip("Maximum number of log lines kept in the UI.")]
        [SerializeField] private int maxLogLines = DefaultMaxLogLines;

        [Tooltip("Maximum number of characters kept in the log text. Set to 0 to disable the character limit.")]
        [SerializeField] private int maxLogCharacters = DefaultMaxLogCharacters;

        [Tooltip("When enabled, the scroll view stays pinned to the newest log entry.")]
        [SerializeField] private bool autoScroll = true;
        #endregion

        #region Settings
        [Tooltip("Settings asset that configures narrative log filtering.")]
        [Header("Settings")]
        [SerializeField] private EM_NarrativeLogSettings settings;
        #endregion
        #endregion

        #region State
        private readonly List<string> logLines = new List<string>();
        private readonly List<TMP_Text> linePool = new List<TMP_Text>();
        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery logQuery;
        private float nextRefreshTime;
        private ulong lastSequence;
        private int lastFilterSignature;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            nextRefreshTime = 0f;
            lastSequence = 0;
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
            UpdateLog();
        }
        #endregion
    }
}
