using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines an institution controlling norms and influence within a society.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Institution Definition", fileName = "EM_InstitutionDefinition")]
    public sealed class EM_InstitutionDefinition : ScriptableObject
    {
        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key for this institution. Used by profiles and runtime references.")]
        [SerializeField] private string institutionId = "Institution.Id";

        [Tooltip("Designer-facing label shown in tools.")]
        [SerializeField] private string displayName = "Institution";
        #endregion

        // Serialized influence
        #region Serialized - Influence
        [Tooltip("Authority weight applied when enforcing norms. Higher values mean stronger influence.")]
        [SerializeField] private float authorityWeight = 1f;

        [Tooltip("Norms enforced by this institution.")]
        [SerializeField] private EM_NormDefinition[] norms = new EM_NormDefinition[0];
        #endregion

        // Serialized notes
        #region Serialized - Notes
        [Tooltip("Describe the institution role and how it shapes society behavior.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the institution identifier.
        /// </summary>
        public string InstitutionId
        {
            get
            {
                return institutionId;
            }
        }

        /// <summary>
        /// Gets the display name for this institution.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets the authority weight for this institution.
        /// </summary>
        public float AuthorityWeight
        {
            get
            {
                return authorityWeight;
            }
        }

        /// <summary>
        /// Gets the norms enforced by this institution.
        /// </summary>
        public EM_NormDefinition[] Norms
        {
            get
            {
                return norms;
            }
        }

        /// <summary>
        /// Gets the designer description for this institution.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }
        #endregion
    }
}
