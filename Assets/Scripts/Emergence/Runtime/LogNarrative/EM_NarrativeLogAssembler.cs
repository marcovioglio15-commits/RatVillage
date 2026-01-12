using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    [DisallowMultipleComponent]
    public sealed partial class EM_NarrativeLogAssembler : MonoBehaviour
    {
        #region Constants
        private const float DefaultRefreshIntervalSeconds = 0.25f;
        #endregion

        #region Serialized
        #region Settings
        [Tooltip("Settings asset that configures narrative log thresholds and cadence.")]
        [Header("Settings")]
        [SerializeField] private EM_NarrativeLogSettings settings;

        [Tooltip("Templates asset used to format narrative log entries.")]
        [SerializeField] private EM_NarrativeLogTemplates templates;

        [Tooltip("Override the refresh interval in seconds. Use 0 to follow the settings asset.")]
        [SerializeField] private float refreshIntervalOverride;

        [Tooltip("When disabled, narrative log processing is paused.")]
        [SerializeField] private bool enableProcessing = true;
        #endregion
        #endregion

        #region State
        private readonly Dictionary<EM_NarrativeNeedKey, EM_NarrativeNeedState> needStates =
            new Dictionary<EM_NarrativeNeedKey, EM_NarrativeNeedState>();
        private readonly Dictionary<Entity, EM_NarrativeHealthState> healthStates =
            new Dictionary<Entity, EM_NarrativeHealthState>();
        private readonly Dictionary<EM_NarrativeEventKey, double> lastEventTimes =
            new Dictionary<EM_NarrativeEventKey, double>();
        private readonly List<int> matchingTemplates = new List<int>();
        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery logQuery;
        private float nextRefreshTime;
        private ulong lastSignalSequence;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            nextRefreshTime = 0f;
            lastSignalSequence = 0;
            needStates.Clear();
            healthStates.Clear();
            lastEventTimes.Clear();
        }

        private void Update()
        {
            if (!enableProcessing)
                return;

            if (!EnsureWorld())
                return;

            float interval = ResolveRefreshInterval();

            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + interval;
            ProcessSignals();
        }
        #endregion

        #region World
        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            logQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EM_Component_NarrativeLog>(),
                ComponentType.ReadOnly<EM_BufferElement_NarrativeSignal>());
            return true;
        }
        #endregion

        #region Settings
        private float ResolveRefreshInterval()
        {
            if (refreshIntervalOverride > 0f)
                return refreshIntervalOverride;

            if (settings == null)
                return DefaultRefreshIntervalSeconds;

            float interval = settings.RefreshIntervalSeconds;

            if (interval <= 0f)
                interval = DefaultRefreshIntervalSeconds;

            return interval;
        }
        #endregion
    }
}
