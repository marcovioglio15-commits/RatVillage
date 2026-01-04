using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Society Profile", fileName = "EM_SocietyProfile")]
    public sealed class EM_SocietyProfile : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Stable identifier for this profile.")]
        [Header("Identity")]
        [SerializeField] private string profileId = "Society.Profile";
        #endregion

        #region Composition
        [Tooltip("Domains active for this profile.")]
        [Header("Composition")]
        [SerializeField] private EM_DomainDefinition[] domains = new EM_DomainDefinition[0];
        #endregion

        #region Notes
        [Tooltip("Optional description for designers.")]
        [Header("Notes")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
		#endregion
		#endregion

		#region Public Properties Properties
		public string ProfileId
        {
            get
            {
                return profileId;
            }
        }

        public EM_DomainDefinition[] Domains
        {
            get
            {
                return domains;
            }
        }

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
