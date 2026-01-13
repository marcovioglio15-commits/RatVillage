using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcActivityNavigation
    {
        #region Helpers
        #region Grid
        private bool TryGetGrid(ref SystemState state, out Entity gridEntity, out EM_Component_LocationGrid grid)
        {
            gridEntity = Entity.Null;
            grid = default;

            foreach ((RefRO<EM_Component_LocationGrid> gridComponent, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_LocationGrid>>().WithEntityAccess())
            {
                gridEntity = entity;
                grid = gridComponent.ValueRO;
                return true;
            }

            return false;
        }

        private NativeParallelHashMap<int, Entity> BuildAnchorMap(ref SystemState state, Entity gridEntity)
        {
            NativeParallelHashMap<int, Entity> map = new NativeParallelHashMap<int, Entity>(32, Allocator.Temp);

            foreach ((RefRO<EM_Component_LocationAnchor> anchor, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_LocationAnchor>>().WithEntityAccess())
            {
                if (anchor.ValueRO.Grid != gridEntity)
                    continue;

                int nodeIndex = anchor.ValueRO.NodeIndex;

                if (nodeIndex < 0)
                    continue;

                if (map.ContainsKey(nodeIndex))
                    continue;

                map.TryAdd(nodeIndex, entity);
            }

            return map;
        }

        #endregion

        #region Timing
        private double ResolveSimulatedTimeSeconds(Entity societyRoot, double fallbackTimeSeconds)
        {
            if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                return fallbackTimeSeconds;

            return clockLookup[societyRoot].SimulatedTimeSeconds;
        }

        #endregion

        #region TargetState
        private static bool IsAtReservedNode(EM_Component_NpcActivityTargetState activityTargetState, int currentNodeIndex)
        {
            if (activityTargetState.HasReservation == 0)
                return false;

            if (activityTargetState.ReservedNodeIndex < 0)
                return false;

            return activityTargetState.ReservedNodeIndex == currentNodeIndex;
        }

        private static float ResolveRecheckInterval(EM_Component_NpcActivityTargetSettings settings)
        {
            return math.max(0.1f, settings.RecheckIntervalSeconds);
        }

        private static void ResetActivityTargetState(RefRW<EM_Component_NpcActivityTargetState> activityTargetState)
        {
            activityTargetState.ValueRW.ReservedNodeIndex = -1;
            activityTargetState.ValueRW.ApproachNodeIndex = -1;
            activityTargetState.ValueRW.ReservationExpiryTimeSeconds = -1d;
            activityTargetState.ValueRW.NextRecheckTimeSeconds = -1d;
            activityTargetState.ValueRW.HasReservation = 0;
            activityTargetState.ValueRW.IsWaitingForSlot = 0;
        }

        private static void ClearActivityTarget(Entity entity, RefRW<EM_Component_NpcNavigationState> navigationState,
            RefRW<EM_Component_NpcActivityTargetState> activityTargetState, DynamicBuffer<EM_BufferElement_LocationReservation> reservations)
        {
            ReleaseReservation(activityTargetState.ValueRO.ReservedNodeIndex, entity, reservations);
            activityTargetState.ValueRW.LocationId = default;
            ResetActivityTargetState(activityTargetState);
            ClearActivityDestination(navigationState);
        }

        #endregion

        #region Reservation
        private static void ReleaseReservation(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationReservation> reservations)
        {
            if (nodeIndex < 0 || nodeIndex >= reservations.Length)
                return;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy != entity)
                return;

            entry.ReservedBy = Entity.Null;
            entry.ReservedUntilTimeSeconds = -1d;
            reservations[nodeIndex] = entry;
        }

        private static bool IsReservationValid(EM_Component_NpcActivityTargetState activityTargetState, Entity entity,
            DynamicBuffer<EM_BufferElement_LocationNode> nodes, DynamicBuffer<EM_BufferElement_LocationReservation> reservations,
            double timeSeconds)
        {
            int nodeIndex = activityTargetState.ReservedNodeIndex;

            if (nodeIndex < 0 || nodeIndex >= nodes.Length)
                return false;

            if (nodeIndex >= reservations.Length)
                return false;

            if (nodes[nodeIndex].Walkable == 0)
                return false;

            if (!nodes[nodeIndex].LocationId.Equals(activityTargetState.LocationId))
                return false;

            if (activityTargetState.ReservationExpiryTimeSeconds > 0d &&
                timeSeconds >= activityTargetState.ReservationExpiryTimeSeconds)
                return false;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy == Entity.Null)
                return false;

            if (entry.ReservedBy != entity)
            {
                if (entry.ReservedUntilTimeSeconds > 0d && timeSeconds >= entry.ReservedUntilTimeSeconds)
                {
                    entry.ReservedBy = Entity.Null;
                    entry.ReservedUntilTimeSeconds = -1d;
                    reservations[nodeIndex] = entry;
                }

                return false;
            }

            if (entry.ReservedUntilTimeSeconds > 0d && timeSeconds >= entry.ReservedUntilTimeSeconds)
            {
                entry.ReservedBy = Entity.Null;
                entry.ReservedUntilTimeSeconds = -1d;
                reservations[nodeIndex] = entry;
                return false;
            }

            return true;
        }

        private static bool TryReserveNode(int nodeIndex, Entity entity, double timeSeconds, float timeoutSeconds,
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy, DynamicBuffer<EM_BufferElement_LocationReservation> reservations,
            out double reservationExpiry)
        {
            reservationExpiry = -1d;

            if (nodeIndex < 0 || nodeIndex >= occupancy.Length || nodeIndex >= reservations.Length)
                return false;

            Entity occupant = occupancy[nodeIndex].Occupant;

            if (occupant != Entity.Null && occupant != entity)
                return false;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy != Entity.Null && entry.ReservedBy != entity)
            {
                if (entry.ReservedUntilTimeSeconds > 0d && timeSeconds >= entry.ReservedUntilTimeSeconds)
                {
                    entry.ReservedBy = Entity.Null;
                    entry.ReservedUntilTimeSeconds = -1d;
                }
                else
                {
                    return false;
                }
            }

            float clampedTimeout = math.max(0.1f, timeoutSeconds);
            reservationExpiry = timeSeconds + clampedTimeout;
            entry.ReservedBy = entity;
            entry.ReservedUntilTimeSeconds = reservationExpiry;
            reservations[nodeIndex] = entry;
            return true;
        }

        #endregion

        #region Destination
        private static void SetActivityDestination(RefRW<EM_Component_NpcNavigationState> navigationState, int nodeIndex,
            ref NativeParallelHashMap<int, Entity> anchorMap)
        {
            if (nodeIndex < 0)
                return;

            if (navigationState.ValueRO.DestinationKind == EM_NpcDestinationKind.Activity &&
                navigationState.ValueRO.DestinationNodeIndex == nodeIndex)
                return;

            Entity anchorEntity = Entity.Null;
            anchorMap.TryGetValue(nodeIndex, out anchorEntity);

            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.Activity;
            navigationState.ValueRW.DestinationAnchor = anchorEntity;
            navigationState.ValueRW.DestinationPosition = float3.zero;
            navigationState.ValueRW.DestinationNodeIndex = nodeIndex;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }

        private static void ClearActivityDestination(RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            if (navigationState.ValueRO.DestinationKind != EM_NpcDestinationKind.Activity)
                return;

            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.None;
            navigationState.ValueRW.DestinationAnchor = Entity.Null;
            navigationState.ValueRW.DestinationPosition = float3.zero;
            navigationState.ValueRW.DestinationNodeIndex = -1;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }
        #endregion
        #endregion
    }
}
