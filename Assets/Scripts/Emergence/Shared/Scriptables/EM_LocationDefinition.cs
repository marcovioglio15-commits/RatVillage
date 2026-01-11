using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Location Definition", fileName = "EM_LocationDefinition")]
    public sealed class EM_LocationDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Stable id string used at runtime. Keep unique across all locations.")]
        [Header("Identity")]
        [SerializeField] private string id = "Location.Id";

        [Tooltip("Display name shown in logs and editor tools.")]
        [SerializeField] private string displayName = "Location";

        [Tooltip("Color used by gizmos and editor previews.")]
        [SerializeField] private Color color = Color.white;
        #endregion

        #region Queue
        [Tooltip("Radius around the location anchor used to place queue slots.")]
        [Header("Queue")]
        [SerializeField] private float queueRadius = 2f;

        [Tooltip("Maximum number of queue slots around this location.")]
        [SerializeField] private int queueSlotCount = 4;
        #endregion
        #endregion

        #region Public Properties
        public string Id
        {
            get
            {
                return id;
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                return id;
            }
        }

        public Color Color
        {
            get
            {
                return color;
            }
        }

        public float QueueRadius
        {
            get
            {
                if (queueRadius < 0f)
                    return 0f;

                return queueRadius;
            }
        }

        public int QueueSlotCount
        {
            get
            {
                if (queueSlotCount < 0)
                    return 0;

                return queueSlotCount;
            }
        }
        #endregion
    }
}
