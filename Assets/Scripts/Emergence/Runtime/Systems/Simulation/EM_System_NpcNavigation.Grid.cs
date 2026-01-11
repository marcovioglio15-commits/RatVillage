using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcNavigation
    {
        #region Grid
        private static bool TryGetGrid(ref SystemState state, out Entity gridEntity, out EM_Component_LocationGrid grid)
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

        private static NativeParallelHashMap<int, Entity> BuildAnchorMap(ref SystemState state, Entity gridEntity)
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
    }
}
