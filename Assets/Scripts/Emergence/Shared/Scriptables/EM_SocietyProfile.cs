using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Society Profile", fileName = "EM_SocietyProfile")]
    public sealed class EM_SocietyProfile : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Id definition that supplies the stable identifier for this profile.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.Profile)]
        [SerializeField] private EM_IdDefinition profileIdDefinition;

        [Tooltip("Legacy profile id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string profileId = "Society.Profile";
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
                return EM_IdUtility.ResolveId(profileIdDefinition, profileId);
            }
        }

        public EM_IdDefinition ProfileIdDefinition
        {
            get
            {
                return profileIdDefinition;
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
