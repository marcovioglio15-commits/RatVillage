using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Ensures a debug log entity exists for Emergence runtime events.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EmergenceDebugLogSystem : ISystem
    {
        #region Constants
        private const int DefaultMaxEntries = 256;
        #endregion

        #region Unity
        /// <summary>
        /// Creates the debug log entity if it does not already exist.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            EntityQuery query = state.GetEntityQuery(ComponentType.ReadOnly<EmergenceDebugLog>());
            int count = query.CalculateEntityCount();

            if (count > 0)
            {
                state.Enabled = false;
                return;
            }

            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new EmergenceDebugLog { MaxEntries = DefaultMaxEntries });
            state.EntityManager.AddBuffer<EmergenceDebugEvent>(entity);
            state.Enabled = false;
        }

        /// <summary>
        /// Reserved for future debug log maintenance.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
        }
        #endregion
    }
}
