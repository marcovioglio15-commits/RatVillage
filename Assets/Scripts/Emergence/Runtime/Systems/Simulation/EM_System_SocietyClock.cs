using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EM_System_SocietyClock : ISystem
    {
        #region Unity Lifecycle
        // Require a society clock to run.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_SocietyClock>();
        }

        // Advance society time-of-day values.
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            if (deltaTime <= 0f)
                return;

            foreach (RefRW<EM_Component_SocietyClock> clock
                in SystemAPI.Query<RefRW<EM_Component_SocietyClock>>().WithAll<EM_Component_SocietyRoot>())
            {
                float dayLengthSeconds = math.max(clock.ValueRO.DayLengthSeconds, 0.01f);
                double simulatedDeltaSeconds = deltaTime * 86400d / dayLengthSeconds;
                double simulatedTimeSeconds = clock.ValueRO.SimulatedTimeSeconds + simulatedDeltaSeconds;
                double totalHours = simulatedTimeSeconds / 3600d;
                double wrappedHours = totalHours % 24d;

                if (wrappedHours < 0d)
                    wrappedHours += 24d;

                clock.ValueRW.SimulatedTimeSeconds = simulatedTimeSeconds;
                clock.ValueRW.TimeOfDay = (float)wrappedHours;
            }
        }
        #endregion
    }
}
