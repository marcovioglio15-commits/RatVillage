using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NpcSchedule))]
    [UpdateBefore(typeof(EM_System_NpcNavigation))]
    public partial struct EM_System_NpcActivityNavigation : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LocationGrid>();
            state.RequireForUpdate<EM_Component_LocationAnchor>();
            state.RequireForUpdate<EM_Component_NpcScheduleTarget>();
            state.RequireForUpdate<EM_Component_NpcNavigationState>();
            state.RequireForUpdate<EM_Component_NpcActivityTargetState>();
            state.RequireForUpdate<EM_Component_NpcActivityTargetSettings>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity;
            EM_Component_LocationGrid grid;

            if (!TryGetGrid(ref state, out gridEntity, out grid))
                return;

            DynamicBuffer<EM_BufferElement_LocationNode> nodes = SystemAPI.GetBuffer<EM_BufferElement_LocationNode>(gridEntity);
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy = SystemAPI.GetBuffer<EM_BufferElement_LocationOccupancy>(gridEntity);
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations = SystemAPI.GetBuffer<EM_BufferElement_LocationReservation>(gridEntity);

            if (nodes.Length == 0 || nodes.Length != occupancy.Length || nodes.Length != reservations.Length)
                return;

            NativeParallelHashMap<int, Entity> anchorMap = BuildAnchorMap(ref state, gridEntity);

            clockLookup.Update(ref state);
            memberLookup.Update(ref state);
            double worldTimeSeconds = SystemAPI.Time.ElapsedTime;

            foreach ((RefRO<EM_Component_NpcScheduleTarget> target, RefRO<EM_Component_NpcLocationState> locationState,
                RefRO<EM_Component_TradeRequestState> tradeRequest, RefRW<EM_Component_NpcNavigationState> navigationState,
                RefRW<EM_Component_NpcActivityTargetState> activityTargetState, RefRO<EM_Component_NpcActivityTargetSettings> activitySettings,
                RefRO<LocalTransform> transform, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcScheduleTarget>, RefRO<EM_Component_NpcLocationState>,
                    RefRO<EM_Component_TradeRequestState>, RefRW<EM_Component_NpcNavigationState>,
                    RefRW<EM_Component_NpcActivityTargetState>, RefRO<EM_Component_NpcActivityTargetSettings>,
                    RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (!memberLookup.HasComponent(entity))
                    continue;

                EM_Component_SocietyMember member = memberLookup[entity];

                if (tradeRequest.ValueRO.Stage != EM_TradeRequestStage.None)
                {
                    ClearActivityTarget(entity, navigationState, activityTargetState, reservations);
                    continue;
                }

                FixedString64Bytes locationId = target.ValueRO.LocationId;

                if (locationId.Length == 0)
                {
                    ClearActivityTarget(entity, navigationState, activityTargetState, reservations);
                    continue;
                }

                double timeSeconds = ResolveSimulatedTimeSeconds(member.SocietyRoot, worldTimeSeconds);

                if (!activityTargetState.ValueRO.LocationId.Equals(locationId))
                {
                    ReleaseReservation(activityTargetState.ValueRO.ReservedNodeIndex, entity, reservations);
                    ResetActivityTargetState(activityTargetState);
                    activityTargetState.ValueRW.LocationId = locationId;
                }

                int currentNodeIndex = locationState.ValueRO.CurrentNodeIndex;

                if (currentNodeIndex < 0)
                {
                    if (!EM_Utility_LocationGrid.TryGetNodeIndex(transform.ValueRO.Position, grid, out currentNodeIndex))
                        continue;
                }

                if (IsAtReservedNode(activityTargetState.ValueRO, currentNodeIndex))
                {
                    ClearActivityDestination(navigationState);
                    activityTargetState.ValueRW.IsWaitingForSlot = 0;
                    activityTargetState.ValueRW.NextRecheckTimeSeconds = -1d;

                    if (activityTargetState.ValueRO.ReservationExpiryTimeSeconds > 0d)
                        activityTargetState.ValueRW.ReservationExpiryTimeSeconds = -1d;

                    continue;
                }

                if (activityTargetState.ValueRO.HasReservation != 0)
                {
                    if (IsReservationValid(activityTargetState.ValueRO, entity, nodes, reservations, timeSeconds))
                    {
                        SetActivityDestination(navigationState, activityTargetState.ValueRO.ReservedNodeIndex, ref anchorMap);
                        continue;
                    }

                    ReleaseReservation(activityTargetState.ValueRO.ReservedNodeIndex, entity, reservations);
                    activityTargetState.ValueRW.HasReservation = 0;
                    activityTargetState.ValueRW.ReservedNodeIndex = -1;
                    activityTargetState.ValueRW.ReservationExpiryTimeSeconds = -1d;
                    activityTargetState.ValueRW.NextRecheckTimeSeconds = -1d;
                }

                if (activityTargetState.ValueRO.NextRecheckTimeSeconds > 0d &&
                    timeSeconds < activityTargetState.ValueRO.NextRecheckTimeSeconds)
                {
                    if (activityTargetState.ValueRO.ApproachNodeIndex >= 0)
                        SetActivityDestination(navigationState, activityTargetState.ValueRO.ApproachNodeIndex, ref anchorMap);

                    continue;
                }

                int freeNodeIndex;
                int approachNodeIndex;
                bool found = TryFindActivityNode(currentNodeIndex, locationId, entity, grid, nodes, occupancy, reservations, timeSeconds,
                    out freeNodeIndex, out approachNodeIndex);

                if (!found)
                {
                    ClearActivityDestination(navigationState);
                    activityTargetState.ValueRW.ApproachNodeIndex = -1;
                    activityTargetState.ValueRW.IsWaitingForSlot = 0;
                    activityTargetState.ValueRW.NextRecheckTimeSeconds = timeSeconds + ResolveRecheckInterval(activitySettings.ValueRO);
                    continue;
                }

                if (freeNodeIndex >= 0)
                {
                    double reservationExpiry;
                    bool reserved = TryReserveNode(freeNodeIndex, entity, timeSeconds, activitySettings.ValueRO.ReservationTimeoutSeconds,
                        occupancy, reservations, out reservationExpiry);

                    if (reserved)
                    {
                        activityTargetState.ValueRW.HasReservation = 1;
                        activityTargetState.ValueRW.ReservedNodeIndex = freeNodeIndex;
                        activityTargetState.ValueRW.ApproachNodeIndex = freeNodeIndex;
                        activityTargetState.ValueRW.ReservationExpiryTimeSeconds = reservationExpiry;
                        activityTargetState.ValueRW.IsWaitingForSlot = 0;
                        activityTargetState.ValueRW.NextRecheckTimeSeconds = -1d;
                        SetActivityDestination(navigationState, freeNodeIndex, ref anchorMap);
                        continue;
                    }
                }

                if (approachNodeIndex < 0)
                    approachNodeIndex = freeNodeIndex;

                activityTargetState.ValueRW.HasReservation = 0;
                activityTargetState.ValueRW.ReservedNodeIndex = -1;
                activityTargetState.ValueRW.ApproachNodeIndex = approachNodeIndex;
                activityTargetState.ValueRW.ReservationExpiryTimeSeconds = -1d;
                activityTargetState.ValueRW.IsWaitingForSlot = 1;
                activityTargetState.ValueRW.NextRecheckTimeSeconds = timeSeconds + ResolveRecheckInterval(activitySettings.ValueRO);

                if (approachNodeIndex >= 0)
                    SetActivityDestination(navigationState, approachNodeIndex, ref anchorMap);
            }

            anchorMap.Dispose();
        }
        #endregion
    }
}
