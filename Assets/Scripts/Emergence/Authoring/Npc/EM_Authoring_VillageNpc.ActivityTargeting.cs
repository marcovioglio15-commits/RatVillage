using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Activity Targeting
        [Tooltip("Simulated seconds reserved for reaching a selected activity node before the reservation expires.")]
        [Header("Activity Targeting")]
        [SerializeField] private float activityReservationTimeoutSeconds = 15f;

        [Tooltip("Simulated seconds between rechecks when no free activity node is available.")]
        [SerializeField] private float activityRecheckIntervalSeconds = 1f;
        #endregion

        #region Bake Helpers
        private static void AddActivityTargetingComponents(EM_Authoring_VillageNpc authoring, Entity entity, Baker<EM_Authoring_VillageNpc> baker)
        {
            baker.AddComponent(entity, new EM_Component_NpcActivityTargetSettings
            {
                ReservationTimeoutSeconds = math.max(0.1f, authoring.activityReservationTimeoutSeconds),
                RecheckIntervalSeconds = math.max(0.1f, authoring.activityRecheckIntervalSeconds)
            });
            baker.AddComponent(entity, new EM_Component_NpcActivityTargetState
            {
                LocationId = default,
                ReservedNodeIndex = -1,
                ApproachNodeIndex = -1,
                ReservationExpiryTimeSeconds = -1d,
                NextRecheckTimeSeconds = -1d,
                HasReservation = 0,
                IsWaitingForSlot = 0
            });
        }
        #endregion
    }
}
