using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Signal Definition", fileName = "EM_SignalDefinition")]
    public sealed class EM_SignalDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Header("Identity")]
        [Tooltip("Stable runtime key used by emitters and rule sets. Treat this like an API id: keep it stable after it is referenced in rules or profiles to avoid breaking mappings.")]
        [SerializeField] private string signalId = "Signal.Id";
        #endregion

        #region Notes
        [Header("Notes")]
        [Tooltip("Explain what event this signal represents, which systems should emit it, and typical gameplay moments that produce it.")]
        [TextArea(2, 4)]
        [SerializeField] private string description;
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
        #endregion
    }
}
