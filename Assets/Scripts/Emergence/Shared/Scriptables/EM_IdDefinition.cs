using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Id Definition", fileName = "EM_IdDefinition")]
    public sealed class EM_IdDefinition : ScriptableObject
    {
        #region Serialized
        #region Identity
        [Tooltip("Category that scopes uniqueness for this id definition.")]
        [Header("Identity")]
        [SerializeField] private EM_IdCategory category = EM_IdCategory.None;

        [Tooltip("Stable id string used at runtime. Keep unique within the selected category.")]
        [SerializeField] private string id = "Id.Value";
        #endregion

        #region Notes
        [Tooltip("Optional description for designers and tooling.")]
        [Header("Notes")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public Properties
        public EM_IdCategory Category
        {
            get
            {
                return category;
            }
        }

        public string Id
        {
            get
            {
                return id;
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
