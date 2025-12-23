using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Broadcasts schedule signals from society roots to society members.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceSocietyClockSystem))]
    [UpdateBefore(typeof(EmergenceSignalCollectSystem))]
    public partial struct EmergenceScheduleBroadcastSystem : ISystem
    {
        private struct ScheduleBroadcast
        {
            public Entity SocietyRoot;
            public FixedString64Bytes SignalId;
            public float Value;
        }

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceScheduleSignal>();
        }

        /// <summary>
        /// Broadcasts schedule signals to society members.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            NativeList<ScheduleBroadcast> broadcasts = new NativeList<ScheduleBroadcast>(Allocator.Temp);

            foreach ((DynamicBuffer<EmergenceScheduleSignal> scheduleBuffer, Entity society)
                in SystemAPI.Query<DynamicBuffer<EmergenceScheduleSignal>>().WithAll<EmergenceSocietyRoot>().WithEntityAccess())
            {
                DynamicBuffer<EmergenceScheduleSignal> signals = scheduleBuffer;

                if (signals.Length == 0)
                    continue;

                for (int i = 0; i < signals.Length; i++)
                {
                    ScheduleBroadcast broadcast = new ScheduleBroadcast
                    {
                        SocietyRoot = society,
                        SignalId = signals[i].SignalId,
                        Value = signals[i].Value
                    };

                    broadcasts.Add(broadcast);
                }

                signals.Clear();
            }

            if (broadcasts.Length == 0)
            {
                broadcasts.Dispose();
                return;
            }

            foreach ((DynamicBuffer<EmergenceSignalEvent> signalBuffer, EmergenceSocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EmergenceSignalEvent>, EmergenceSocietyMember>().WithAll<EmergenceSignalEmitter>().WithEntityAccess())
            {
                for (int i = 0; i < broadcasts.Length; i++)
                {
                    if (broadcasts[i].SocietyRoot != member.SocietyRoot)
                        continue;

                    if (broadcasts[i].SignalId.Length == 0)
                        continue;

                    EmergenceSignalEvent signalEvent = new EmergenceSignalEvent
                    {
                        SignalId = broadcasts[i].SignalId,
                        Value = broadcasts[i].Value,
                        Target = entity,
                        LodTier = EmergenceLodTier.Full,
                        Time = 0d
                    };

                    signalBuffer.Add(signalEvent);
                }
            }

            broadcasts.Dispose();
        }
        #endregion
    }
}
