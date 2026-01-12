using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Narrative Log Templates", fileName = "EM_NarrativeLogTemplates")]
    public sealed class EM_NarrativeLogTemplates : ScriptableObject
    {
        #region Serialized
        [Tooltip("Narrative templates used to format player-facing and designer-only log entries.")]
        [Header("Templates")]
        [SerializeField] private EM_NarrativeTemplate[] templates = new EM_NarrativeTemplate[0];
        #endregion

        #region Public Properties
        public EM_NarrativeTemplate[] Templates
        {
            get
            {
                return templates;
            }
        }
        #endregion
    }
}
