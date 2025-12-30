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
                float dayLength = math.max(clock.ValueRO.DayLengthSeconds, 0.01f);
                float deltaHours = (deltaTime / dayLength) * 24f;
                float updatedTime = clock.ValueRO.TimeOfDay + deltaHours;
                float wrappedTime = math.fmod(updatedTime, 24f);

                if (wrappedTime < 0f)
                    wrappedTime += 24f;

                clock.ValueRW.TimeOfDay = wrappedTime;
            }
        }
        #endregion
    }
}
