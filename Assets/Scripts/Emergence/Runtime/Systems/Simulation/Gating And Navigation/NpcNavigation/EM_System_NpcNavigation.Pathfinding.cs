using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcNavigation
    {
        #region Pathfinding
        private static bool TryBuildPath(int startIndex, int goalIndex, Entity entity, EM_Component_LocationGrid grid,
            DynamicBuffer<EM_BufferElement_LocationNode> nodes, DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy,
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations, DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes,
            double timeSeconds)
        {
            int width = grid.Width;
            int height = grid.Height;
            int nodeCount = width * height;

            if (nodeCount <= 0)
                return false;

            if (startIndex < 0 || startIndex >= nodeCount || goalIndex < 0 || goalIndex >= nodeCount)
                return false;

            if (nodes[goalIndex].Walkable == 0)
                return false;

            NativeArray<float> gScore = new NativeArray<float>(nodeCount, Allocator.Temp);
            NativeArray<float> fScore = new NativeArray<float>(nodeCount, Allocator.Temp);
            NativeArray<int> cameFrom = new NativeArray<int>(nodeCount, Allocator.Temp);
            NativeArray<byte> inOpen = new NativeArray<byte>(nodeCount, Allocator.Temp);
            NativeList<int> openSet = new NativeList<int>(Allocator.Temp);

            for (int i = 0; i < nodeCount; i++)
            {
                gScore[i] = float.MaxValue;
                fScore[i] = float.MaxValue;
                cameFrom[i] = -1;
                inOpen[i] = 0;
            }

            gScore[startIndex] = 0f;
            fScore[startIndex] = Heuristic(startIndex, goalIndex, width);
            openSet.Add(startIndex);
            inOpen[startIndex] = 1;

            bool found = false;

            while (openSet.Length > 0)
            {
                int currentIndex = ExtractLowest(openSet, fScore);

                if (currentIndex == goalIndex)
                {
                    found = true;
                    break;
                }

                inOpen[currentIndex] = 0;

                for (int i = 0; i < 4; i++)
                {
                    int offsetX;
                    int offsetY;
                    ResolveNeighborOffset(i, out offsetX, out offsetY);
                    int neighborIndex = EM_Utility_LocationGrid.GetNeighborIndex(currentIndex, offsetX, offsetY, grid);

                    if (neighborIndex < 0)
                        continue;

                    if (nodes[neighborIndex].Walkable == 0)
                        continue;

                    if (IsNodeBlocked(neighborIndex, entity, occupancy, reservations, timeSeconds) && neighborIndex != goalIndex)
                        continue;

                    float tentative = gScore[currentIndex] + 1f;

                    if (tentative >= gScore[neighborIndex])
                        continue;

                    cameFrom[neighborIndex] = currentIndex;
                    gScore[neighborIndex] = tentative;
                    fScore[neighborIndex] = tentative + Heuristic(neighborIndex, goalIndex, width);

                    if (inOpen[neighborIndex] == 0)
                    {
                        openSet.Add(neighborIndex);
                        inOpen[neighborIndex] = 1;
                    }
                }
            }

            if (!found)
            {
                gScore.Dispose();
                fScore.Dispose();
                cameFrom.Dispose();
                inOpen.Dispose();
                openSet.Dispose();
                return false;
            }

            BuildPath(startIndex, goalIndex, cameFrom, pathNodes);

            gScore.Dispose();
            fScore.Dispose();
            cameFrom.Dispose();
            inOpen.Dispose();
            openSet.Dispose();
            return true;
        }

        private static void BuildPath(int startIndex, int goalIndex, NativeArray<int> cameFrom,
            DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes)
        {
            NativeList<int> reversed = new NativeList<int>(Allocator.Temp);
            int current = goalIndex;

            while (current >= 0)
            {
                reversed.Add(current);

                if (current == startIndex)
                    break;

                current = cameFrom[current];
            }

            if (reversed.Length == 0)
            {
                reversed.Dispose();
                return;
            }

            pathNodes.ResizeUninitialized(reversed.Length);
            int writeIndex = 0;

            for (int i = reversed.Length - 1; i >= 0; i--)
            {
                pathNodes[writeIndex] = new EM_BufferElement_NpcPathNode
                {
                    NodeIndex = reversed[i]
                };
                writeIndex++;
            }

            reversed.Dispose();
        }

        private static int ExtractLowest(NativeList<int> openSet, NativeArray<float> fScore)
        {
            int bestIndex = 0;
            float bestScore = fScore[openSet[0]];

            for (int i = 1; i < openSet.Length; i++)
            {
                float score = fScore[openSet[i]];

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestIndex = i;
            }

            int bestNode = openSet[bestIndex];
            openSet.RemoveAtSwapBack(bestIndex);
            return bestNode;
        }

        private static float Heuristic(int index, int goalIndex, int width)
        {
            if (width <= 0)
                return 0f;

            int x = index % width;
            int y = index / width;
            int gx = goalIndex % width;
            int gy = goalIndex / width;

            int dx = math.abs(x - gx);
            int dy = math.abs(y - gy);
            return dx + dy;
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
