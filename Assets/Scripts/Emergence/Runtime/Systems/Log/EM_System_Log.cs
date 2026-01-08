using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EM_System_Log : ISystem
    {
        #region Constants
        private const int DefaultMaxEntries = 256;
        #endregion

        #region Unity LyfeCycle
        public void OnCreate(ref SystemState state)
        {
            EntityQuery query = state.GetEntityQuery(ComponentType.ReadOnly<EM_Component_Log>());
            int count = query.CalculateEntityCount();

            if (count > 0)
            {
                state.Enabled = false;
                return;
            }

            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new EM_Component_Log
            {
                MaxEntries = DefaultMaxEntries,
                NextSequence = 1
            });
            state.EntityManager.AddBuffer<EM_Component_Event>(entity);
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }
        #endregion
    }
}
