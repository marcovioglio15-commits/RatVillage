using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcNavigation
    {
        #region Occupancy
        private static void UpdateCurrentNode(float3 position, Entity entity, EM_Component_LocationGrid grid,
            DynamicBuffer<EM_BufferElement_LocationNode> nodes, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, RefRW<EM_Component_NpcLocationState> locationState,
            ref NativeParallelHashMap<int, Entity> anchorMap)
        {
            int nodeIndex;

            if (!EM_Utility_LocationGrid.TryGetNodeIndex(position, grid, out nodeIndex))
                return;

            if (nodeIndex == locationState.ValueRO.CurrentNodeIndex)
                return;

            ClearOccupancy(locationState.ValueRO.CurrentNodeIndex, entity, occupancy);
            occupancy[nodeIndex] = new EM_BufferElement_LocationOccupancy { Occupant = entity };
            ClearReservationForEntity(nodeIndex, entity, reservations);

            locationState.ValueRW.CurrentNodeIndex = nodeIndex;
            locationState.ValueRW.CurrentLocationId = nodes[nodeIndex].LocationId;

            Entity anchorEntity;
            bool hasAnchor = anchorMap.TryGetValue(nodeIndex, out anchorEntity);
            locationState.ValueRW.CurrentLocationAnchor = hasAnchor ? anchorEntity : Entity.Null;
        }

        private static void UpdateNodeOccupancy(int nodeIndex, Entity entity, RefRW<EM_Component_NpcLocationState> locationState,
            DynamicBuffer<EM_BufferElement_LocationNode> nodes, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, ref NativeParallelHashMap<int, Entity> anchorMap)
        {
            if (nodeIndex < 0 || nodeIndex >= occupancy.Length)
                return;

            ClearOccupancy(locationState.ValueRO.CurrentNodeIndex, entity, occupancy);
            occupancy[nodeIndex] = new EM_BufferElement_LocationOccupancy { Occupant = entity };
            ClearReservationForEntity(nodeIndex, entity, reservations);
            locationState.ValueRW.CurrentNodeIndex = nodeIndex;
            locationState.ValueRW.CurrentLocationId = nodes[nodeIndex].LocationId;

            Entity anchorEntity;
            bool hasAnchor = anchorMap.TryGetValue(nodeIndex, out anchorEntity);
            locationState.ValueRW.CurrentLocationAnchor = hasAnchor ? anchorEntity : Entity.Null;
        }

        private static void ClearOccupancy(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy)
        {
            if (nodeIndex < 0 || nodeIndex >= occupancy.Length)
                return;

            EM_BufferElement_LocationOccupancy entry = occupancy[nodeIndex];

            if (entry.Occupant != entity)
                return;

            entry.Occupant = Entity.Null;
            occupancy[nodeIndex] = entry;
        }

        private static void ClearReservationForEntity(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationReservation> reservations)
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

        private static bool IsNodeBlocked(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, double timeSeconds)
        {
            if (nodeIndex < 0 || nodeIndex >= occupancy.Length)
                return true;

            Entity occupant = occupancy[nodeIndex].Occupant;

            if (occupant == Entity.Null)
                return IsReservationBlocked(nodeIndex, entity, reservations, timeSeconds);

            if (occupant == entity)
                return false;

            return true;
        }

        private static bool IsReservationBlocked(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationReservation> reservations,
            double timeSeconds)
        {
            if (nodeIndex < 0 || nodeIndex >= reservations.Length)
                return true;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy == Entity.Null)
                return false;

            if (entry.ReservedBy == entity)
                return false;

            if (entry.ReservedUntilTimeSeconds > 0d && timeSeconds >= entry.ReservedUntilTimeSeconds)
            {
                entry.ReservedBy = Entity.Null;
                entry.ReservedUntilTimeSeconds = -1d;
                reservations[nodeIndex] = entry;
                return false;
            }

            return true;
        }
        #endregion
    }
}
