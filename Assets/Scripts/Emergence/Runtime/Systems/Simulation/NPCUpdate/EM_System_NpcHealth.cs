using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_MetricSample))]
    public partial struct EM_System_NpcHealth : ISystem
    {
        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcHealth>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRW<EM_Component_NpcHealth> health, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_NpcHealth>>()
                    .WithNone<Disabled>()
                    .WithEntityAccess())
            {
                float maxHealth = math.max(health.ValueRO.Max, 0f);
                float currentHealth = math.clamp(health.ValueRO.Current, 0f, maxHealth);

                if (health.ValueRO.Max != maxHealth)
                    health.ValueRW.Max = maxHealth;

                if (health.ValueRO.Current != currentHealth)
                    health.ValueRW.Current = currentHealth;

                if (currentHealth <= 0f)
                    ecb.AddComponent<Disabled>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        #endregion
    }
}
