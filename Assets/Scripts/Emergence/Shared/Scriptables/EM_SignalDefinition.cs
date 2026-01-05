using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Signal Definition", fileName = "EM_SignalDefinition")]
    public sealed class EM_SignalDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Id definition that supplies the stable runtime key for this signal.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.Signal)]
        [SerializeField] private EM_IdDefinition signalIdDefinition;

        [Tooltip("Legacy signal id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string signalId = "Signal.Id";
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
                return EM_IdUtility.ResolveId(signalIdDefinition, signalId);
            }
        }

        public EM_IdDefinition SignalIdDefinition
        {
            get
            {
                return signalIdDefinition;
            }
        }
        #endregion
    }
}
