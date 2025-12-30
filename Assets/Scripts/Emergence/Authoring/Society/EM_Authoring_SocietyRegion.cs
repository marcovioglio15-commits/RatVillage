using UnityEngine;

namespace EmergentMechanics
{
    /// <summary>
    /// Authoring component that defines a society region used for LOD and aggregation.
    /// </summary>
    public sealed class EM_Authoring_SocietyRegion : MonoBehaviour
    {
        #region Fields

        #region Serialized
        #region Region
        [Tooltip("Region size in world units.")]
        [SerializeField] private Vector2 regionSize = new Vector2(250f, 250f);

        [Tooltip("Height used for debug gizmo rendering.")]
        [SerializeField] private float regionHeight = 5f;
        #endregion

        #region Visualization
        [Tooltip("Enables region gizmo rendering.")]
        [SerializeField] private bool drawGizmo = true;

        [Tooltip("Draws the gizmo only when the object is selected.")]
        [SerializeField] private bool drawOnlyWhenSelected = true;

        [Tooltip("Gizmo color used for the region bounds.")]
        [SerializeField] private Color gizmoColor = new Color(0.1f, 0.8f, 0.6f, 0.8f);
        #endregion
        #endregion

        #region Public Properties Properties
        /// <summary>
        /// Gets the region size in world units.
        /// </summary>
        public Vector2 RegionSize
        {
            get
            {
                return regionSize;
            }
        }

        /// <summary>
        /// Gets the region height used for gizmo rendering.
        /// </summary>
        public float RegionHeight
        {
            get
            {
                return regionHeight;
            }
        }
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle Lifecycle
        private void OnDrawGizmos()
        {
            if (!drawGizmo || drawOnlyWhenSelected)
                return;

            DrawRegionGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || !drawOnlyWhenSelected)
                return;

            DrawRegionGizmo();
        }
        #endregion

        #region Helpers
        private void DrawRegionGizmo()
        {
            Vector3 size = new Vector3(regionSize.x, regionHeight, regionSize.y);
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, size);
        }
        #endregion

        #endregion
    }
}
