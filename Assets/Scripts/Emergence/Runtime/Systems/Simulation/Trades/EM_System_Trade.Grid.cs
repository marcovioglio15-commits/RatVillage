using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Grid
        private bool TryResolveGrid(ref SystemState state, out Entity gridEntity, out EM_Component_LocationGrid grid)
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
        #endregion
    }
}
