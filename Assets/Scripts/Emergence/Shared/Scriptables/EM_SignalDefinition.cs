using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Signal Definition", fileName = "EM_SignalDefinition")]
    public sealed class EM_SignalDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Stable runtime key used by emitters and rule sets. Treat this like an API id: keep it stable after it is referenced in rules or profiles to avoid breaking mappings.")]
        [Header("Identity")]
        [SerializeField] private string signalId = "Signal.Id";

        [Tooltip("Designer-facing label shown in Emergence Studio and inspectors. Safe to rename for readability without affecting runtime links.")]
        [SerializeField] private string displayName = "Signal";
        #endregion

        #region Classification
        [Tooltip("Domain owner for this signal. Domains are high-level categories that group related mechanics for authoring and profiling.")]
        [Header("Classification")]
        [SerializeField] private EM_DomainDefinition domain;
        #endregion

        #region Notes
        [Tooltip("Explain what event this signal represents, which systems should emit it, and typical gameplay moments that produce it.")]
        [Header("Notes")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public Properties
        public string SignalId
        {
            get
            {
                return signalId;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public EM_DomainDefinition Domain
        {
            get
            {
                return domain;
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
