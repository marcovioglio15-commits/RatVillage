using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_MetricSample))]
    public partial struct EM_System_NpcHealth : ISystem
    {
        #region Fields
        private BufferLookup<Child> childLookup;
        private ComponentLookup<Disabled> disabledLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcHealth>();
            childLookup = state.GetBufferLookup<Child>(true);
            disabledLookup = state.GetComponentLookup<Disabled>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            childLookup.Update(ref state);
            disabledLookup.Update(ref state);

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
                {
                    DisableChildren(entity, ref childLookup, ref disabledLookup, ref ecb);
                    ecb.AddComponent<Disabled>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        #endregion

        #region Helpers
        private static void DisableChildren(Entity entity, ref BufferLookup<Child> childLookup,
            ref ComponentLookup<Disabled> disabledLookup, ref EntityCommandBuffer ecb)
        {
            if (!childLookup.HasBuffer(entity))
                return;

            DynamicBuffer<Child> children = childLookup[entity];

            for (int i = 0; i < children.Length; i++)
            {
                Entity child = children[i].Value;

                if (child == Entity.Null || disabledLookup.HasComponent(child))
                    continue;

                ecb.AddComponent<Disabled>(child);
            }
        }
        #endregion
    }
}
