using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public static class EM_Utility_LocationGrid
    {
        #region Indexing
        public static int ToIndex(int x, int y, int width)
        {
            return x + y * width;
        }

        public static bool TryGetCoords(int index, int width, int height, out int x, out int y)
        {
            x = 0;
            y = 0;

            if (width <= 0 || height <= 0)
                return false;

            if (index < 0 || index >= width * height)
                return false;

            x = index % width;
            y = index / width;
            return true;
        }
        #endregion

        #region World Mapping
        public static bool TryGetNodeIndex(float3 position, in EM_Component_LocationGrid grid, out int index)
        {
            index = -1;
            int width = grid.Width;
            int height = grid.Height;
            float nodeSize = grid.NodeSize;

            if (width <= 0 || height <= 0 || nodeSize <= 0f)
                return false;

            float3 local = position - grid.Origin;
            int x = (int)math.floor(local.x / nodeSize);
            int y = (int)math.floor(local.z / nodeSize);

            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            index = ToIndex(x, y, width);
            return true;
        }

        public static float3 GetNodeCenter(int index, in EM_Component_LocationGrid grid)
        {
            int x;
            int y;

            if (!TryGetCoords(index, grid.Width, grid.Height, out x, out y))
                return grid.Origin;

            float nodeSize = grid.NodeSize;
            return grid.Origin + new float3((x + 0.5f) * nodeSize, 0f, (y + 0.5f) * nodeSize);
        }
        #endregion

        #region Neighbors
        public static int GetNeighborIndex(int index, int offsetX, int offsetY, in EM_Component_LocationGrid grid)
        {
            int x;
            int y;

            if (!TryGetCoords(index, grid.Width, grid.Height, out x, out y))
                return -1;

            int nextX = x + offsetX;
            int nextY = y + offsetY;

            if (nextX < 0 || nextX >= grid.Width || nextY < 0 || nextY >= grid.Height)
                return -1;

            return ToIndex(nextX, nextY, grid.Width);
        }
        #endregion
    }
}
