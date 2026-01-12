using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EM_System_NarrativeLog : ISystem
    {
        #region Constants
        private const int DefaultMaxSignalEntries = 1024;
        private const int DefaultMaxLogEntries = 512;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            EntityQuery query = state.GetEntityQuery(ComponentType.ReadOnly<EM_Component_NarrativeLog>());
            int count = query.CalculateEntityCount();

            if (count > 0)
            {
                state.Enabled = false;
                return;
            }

            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new EM_Component_NarrativeLog
            {
                MaxSignalEntries = DefaultMaxSignalEntries,
                MaxLogEntries = DefaultMaxLogEntries,
                NextSignalSequence = 1,
                NextLogSequence = 1
            });
            state.EntityManager.AddBuffer<EM_BufferElement_NarrativeSignal>(entity);
            state.EntityManager.AddBuffer<EM_BufferElement_NarrativeLogEntry>(entity);
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }
        #endregion
    }
}
