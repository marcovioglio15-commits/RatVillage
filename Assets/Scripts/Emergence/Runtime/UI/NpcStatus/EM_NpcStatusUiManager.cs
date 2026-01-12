using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    [DisallowMultipleComponent]
    public sealed partial class EM_NpcStatusUiManager : MonoBehaviour
    {
        #region Constants
        private const float DefaultRefreshInterval = 0.25f;
        #endregion

        #region Serialized
        #region References
        [Tooltip("Prefab used to render NPC status text. Use a TextMeshPro (3D) or a world-space TextMeshProUGUI.")]
        [Header("References")]
        [SerializeField] private TMP_Text statusTextPrefab;

        [Tooltip("Optional parent for spawned status text instances. Defaults to this transform when unset.")]
        [SerializeField] private Transform statusRoot;
        #endregion

        #region Behavior
        [Tooltip("World offset applied above each NPC position.")]
        [Header("Behavior")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

        [Tooltip("Refresh interval in seconds for rebuilding NPC status text.")]
        [SerializeField] private float refreshInterval = DefaultRefreshInterval;

        [Tooltip("When enabled, status text rotates to face the camera.")]
        [SerializeField] private bool faceCamera = true;
        #endregion
        #endregion

        #region State
        private readonly Dictionary<Entity, NpcStatusEntry> entries = new Dictionary<Entity, NpcStatusEntry>();
        private readonly HashSet<Entity> activeEntities = new HashSet<Entity>();
        private readonly List<Entity> removalBuffer = new List<Entity>();
        private World cachedWorld;
        private EntityManager entityManager;
        private EntityQuery npcQuery;
        private Camera cachedCamera;
        private float nextRefreshTime;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            nextRefreshTime = 0f;
            ClearEntries();
        }

        private void OnDisable()
        {
            ClearEntries();
        }

        private void Update()
        {
            if (!EnsureWorld())
                return;

            if (statusTextPrefab == null)
                return;

            float interval = refreshInterval;

            if (interval <= 0f)
                interval = DefaultRefreshInterval;

            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + interval;
            RefreshEntries();
        }

        private void LateUpdate()
        {
            if (entries.Count == 0)
                return;

            if (!EnsureWorld())
                return;

            UpdateEntryTransforms();
        }
        #endregion

        #region Entry Data
        private sealed class NpcStatusEntry
        {
            public Entity Entity;
            public TMP_Text Text;
            public Transform Transform;
        }
        #endregion
    }
}
