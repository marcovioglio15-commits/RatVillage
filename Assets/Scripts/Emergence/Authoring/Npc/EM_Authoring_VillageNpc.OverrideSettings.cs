using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Fields
        #region Serialized
        #region Override Cooldowns
        [Tooltip("Cooldown in hours before the same override activity can be applied again.")]
        [Header("Override Cooldowns")]
        [SerializeField] private float sameOverrideCooldownHours;

        [Tooltip("Cooldown in hours before any override activity can be applied again.")]
        [SerializeField] private float anyOverrideCooldownHours;
        #endregion

        #region Health
        [Tooltip("Maximum health for this NPC.")]
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Starting health for this NPC.")]
        [SerializeField] private float initialHealth = 100f;
        #endregion
        #endregion
        #endregion
    }
}
