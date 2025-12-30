using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Domain Definition", fileName = "EM_DomainDefinition")]
    public sealed class EM_DomainDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Unique key used to group signals and rule sets. Treat as a stable category id so profiles and masks remain valid.")]
        [Header("Identity")]
        [SerializeField] private string domainId = "Domain.Id";

        [Tooltip("Designer-facing label shown in tools and debug views. Use clear category names (e.g., Economy, Social, Survival).")]
        [SerializeField] private string displayName = "Domain";
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
                return domainId;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
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
