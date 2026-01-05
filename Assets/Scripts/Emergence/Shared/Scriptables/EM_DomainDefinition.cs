using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Domain Definition", fileName = "EM_DomainDefinition")]
    public sealed class EM_DomainDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Id definition that supplies the unique key used to group signals and rule sets.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.Domain)]
        [SerializeField] private EM_IdDefinition domainIdDefinition;

        [Tooltip("Legacy domain id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string domainId = "Domain.Id";
        #endregion

        #region Visualization
        [Tooltip("Color used in Emergence Studio and debug overlays to identify this domain category at a glance.")]
        [Header("Visualization")]
        [SerializeField] private Color domainColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        #endregion

        #region Rule Sets
        [Tooltip("Rule sets owned by this domain. Use this to keep related behaviors organized for profiles and designers.")]
        [Header("Rule Sets")]
        [SerializeField] private EM_RuleSetDefinition[] ruleSets = new EM_RuleSetDefinition[0];
        #endregion

        #region Notes
        [Tooltip("Design intent for this domain, what behaviors it should drive, and how it should be tuned relative to other domains.")]
        [Header("Notes")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public Properties
        public string DomainId
        {
            get
            {
                return EM_IdUtility.ResolveId(domainIdDefinition, domainId);
            }
        }

        public EM_IdDefinition DomainIdDefinition
        {
            get
            {
                return domainIdDefinition;
            }
        }

        public Color DomainColor
        {
            get
            {
                return domainColor;
            }
        }

        public EM_RuleSetDefinition[] RuleSets
        {
            get
            {
                return ruleSets;
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
