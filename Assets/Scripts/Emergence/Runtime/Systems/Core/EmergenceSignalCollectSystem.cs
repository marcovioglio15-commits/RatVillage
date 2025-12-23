using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Collects emitted signals into a central queue for rule evaluation.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceTradeSystem))]
    [UpdateAfter(typeof(EmergenceScheduleBroadcastSystem))]
    public partial struct EmergenceSignalCollectSystem : ISystem
    {
        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceLibraryReference>();
            state.RequireForUpdate<EmergenceGlobalSettings>();
        }

        /// <summary>
        /// Collects signal events from emitters and appends them to the queue.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            Entity libraryEntity = SystemAPI.GetSingletonEntity<EmergenceLibraryReference>();
            DynamicBuffer<EmergenceSignalEvent> queue = state.EntityManager.GetBuffer<EmergenceSignalEvent>(libraryEntity);
            EmergenceGlobalSettings settings = SystemAPI.GetSingleton<EmergenceGlobalSettings>();
            int maxQueue = settings.MaxSignalQueue;

            if (maxQueue <= 0)
                return;

            int available = maxQueue - queue.Length;
            double currentTime = SystemAPI.Time.ElapsedTime;

            foreach (DynamicBuffer<EmergenceSignalEvent> emitterBuffer in SystemAPI.Query<DynamicBuffer<EmergenceSignalEvent>>().WithAll<EmergenceSignalEmitter>())
            {
                int toCopy = emitterBuffer.Length;

                if (toCopy > available)
                    toCopy = available;

                for (int i = 0; i < toCopy; i++)
                {
                    EmergenceSignalEvent signalEvent = emitterBuffer[i];
                    signalEvent.Time = currentTime;
                    queue.Add(signalEvent);
                }

                emitterBuffer.Clear();

                available -= toCopy;

                if (available < 0)
                    available = 0;
            }
        }
        #endregion
    }
}
