using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcActivityNavigation
    {
        #region Selection
        private static bool TryFindActivityNode(int startIndex, FixedString64Bytes locationId, Entity entity, EM_Component_LocationGrid grid,
            DynamicBuffer<EM_BufferElement_LocationNode> nodes, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, double timeSeconds, out int freeNodeIndex,
            out int approachNodeIndex)
        {
            freeNodeIndex = -1;
            approachNodeIndex = -1;

            int width = grid.Width;
            int height = grid.Height;
            int nodeCount = width * height;

            if (nodeCount <= 0)
                return false;

            if (startIndex < 0 || startIndex >= nodeCount)
                return false;

            NativeArray<byte> visited = new NativeArray<byte>(nodeCount, Allocator.Temp);
            NativeList<int> queue = new NativeList<int>(Allocator.Temp);
            int readIndex = 0;
            queue.Add(startIndex);
            visited[startIndex] = 1;

            while (readIndex < queue.Length)
            {
                int currentIndex = queue[readIndex];
                readIndex++;

                EM_BufferElement_LocationNode node = nodes[currentIndex];

                if (node.Walkable == 0)
                    continue;

                bool isLocationMatch = node.LocationId.Equals(locationId);
                bool blocked = IsNodeBlocked(currentIndex, entity, occupancy, reservations, timeSeconds);

                if (isLocationMatch)
                {
                    if (!blocked)
                    {
                        freeNodeIndex = currentIndex;
                        approachNodeIndex = currentIndex;
                        break;
                    }

                    if (approachNodeIndex < 0)
                        approachNodeIndex = currentIndex;
                }

                if (blocked && currentIndex != startIndex)
                    continue;

                for (int direction = 0; direction < 4; direction++)
                {
                    int offsetX;
                    int offsetY;
                    ResolveNeighborOffset(direction, out offsetX, out offsetY);
                    int neighborIndex = EM_Utility_LocationGrid.GetNeighborIndex(currentIndex, offsetX, offsetY, grid);

                    if (neighborIndex < 0)
                        continue;

                    if (visited[neighborIndex] != 0)
                        continue;

                    EM_BufferElement_LocationNode neighbor = nodes[neighborIndex];

                    if (neighbor.Walkable == 0)
                        continue;

                    bool neighborBlocked = IsNodeBlocked(neighborIndex, entity, occupancy, reservations, timeSeconds);

                    if (neighborBlocked && !neighbor.LocationId.Equals(locationId))
                        continue;

                    visited[neighborIndex] = 1;
                    queue.Add(neighborIndex);
                }
            }

            visited.Dispose();
            queue.Dispose();

            return approachNodeIndex >= 0;
        }

        private static bool IsNodeBlocked(int nodeIndex, Entity entity, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, double timeSeconds)
        {
            if (nodeIndex < 0 || nodeIndex >= occupancy.Length || nodeIndex >= reservations.Length)
                return true;

            Entity occupant = occupancy[nodeIndex].Occupant;

            if (occupant != Entity.Null && occupant != entity)
                return true;

            return IsReservationBlocked(nodeIndex, entity, reservations, timeSeconds);
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

        private static void ResolveNeighborOffset(int index, out int offsetX, out int offsetY)
        {
            offsetX = 0;
            offsetY = 0;

            if (index == 0)
            {
                offsetX = 1;
                return;
            }

            if (index == 1)
            {
                offsetX = -1;
                return;
            }

            if (index == 2)
            {
                offsetY = 1;
                return;
            }

            offsetY = -1;
        }
        #endregion
    }
}
