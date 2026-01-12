using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Movement
        [Tooltip("Base movement speed in meters per second at simulation speed 1.")]
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("Movement acceleration in meters per second squared at simulation speed 1.")]
        [SerializeField] private float acceleration = 8f;

        [Tooltip("Turning speed in degrees per second at simulation speed 1.")]
        [SerializeField] private float turnSpeed = 360f;

        [Tooltip("Stop radius used to determine when the NPC has reached its target.")]
        [SerializeField] private float stopRadius = 0.25f;
        #endregion

        #region Trade Interaction
        [Tooltip("Maximum distance between NPCs to allow trade interactions.")]
        [Header("Trade Interaction")]
        [SerializeField] private float tradeInteractionDistance = 2f;

        [Tooltip("Maximum simulated seconds an NPC waits in a provider queue before timing out.")]
        [SerializeField] private float tradeWaitSeconds = 10f;

        [Tooltip("Allow midway trades anywhere when the interaction distance is met.")]
        [SerializeField] private bool allowMidwayTradeAnywhere = true;
        #endregion

        #region Bake Helpers
        private static void AddMovementComponents(EM_Authoring_VillageNpc authoring, Entity entity, Baker<EM_Authoring_VillageNpc> baker)
        {
            baker.AddComponent(entity, new EM_Component_NpcMovementSettings
            {
                MaxSpeed = math.max(0f, authoring.moveSpeed),
                Acceleration = math.max(0f, authoring.acceleration),
                TurnSpeed = math.max(0f, authoring.turnSpeed),
                StopRadius = math.max(0f, authoring.stopRadius)
            });
            baker.AddComponent(entity, new EM_Component_NpcNavigationState
            {
                DestinationKind = EM_NpcDestinationKind.None,
                DestinationAnchor = Entity.Null,
                DestinationPosition = float3.zero,
                DestinationNodeIndex = -1,
                PathIndex = 0,
                HasPath = 0,
                IsMoving = 0
            });
            baker.AddComponent(entity, new EM_Component_NpcLocationState
            {
                CurrentNodeIndex = -1,
                CurrentLocationId = default,
                CurrentLocationAnchor = Entity.Null,
                LastTradeAnchor = Entity.Null
            });
            baker.AddComponent(entity, new EM_Component_NpcMovementState
            {
                CurrentSpeed = 0f
            });
            baker.AddBuffer<EM_BufferElement_NpcPathNode>(entity);
        }

        private static void AddTradeInteractionComponents(EM_Authoring_VillageNpc authoring, Entity entity, Baker<EM_Authoring_VillageNpc> baker)
        {
            baker.AddComponent(entity, new EM_Component_NpcTradeInteraction
            {
                InteractionDistance = math.max(0f, authoring.tradeInteractionDistance),
                WaitSeconds = math.max(0f, authoring.tradeWaitSeconds),
                AllowMidwayTradeAnywhere = (byte)(authoring.allowMidwayTradeAnywhere ? 1 : 0)
            });
            baker.AddComponent(entity, new EM_Component_TradeRequestState
            {
                IntentId = default,
                NeedId = default,
                ResourceId = default,
                DesiredAmount = 0f,
                Urgency = 0f,
                Provider = Entity.Null,
                TargetAnchor = Entity.Null,
                StartTimeSeconds = -1d,
                WaitStartTimeSeconds = -1d,
                QueueSlotIndex = -1,
                IsOverrideRequest = 0,
                Stage = EM_TradeRequestStage.None
            });
            baker.AddComponent(entity, new EM_Component_TradeProviderState
            {
                ActiveRequester = Entity.Null,
                BusyUntilSeconds = -1d
            });
            baker.AddBuffer<EM_BufferElement_TradeAttemptedProvider>(entity);
            baker.AddBuffer<EM_BufferElement_TradeQueueEntry>(entity);
        }
        #endregion
    }
}
