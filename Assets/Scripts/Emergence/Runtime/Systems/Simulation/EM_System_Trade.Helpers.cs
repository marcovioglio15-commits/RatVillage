using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Helpers
        // Build the per-society readiness map for trade ticks.
        private NativeParallelHashMap<Entity, byte> BuildReadyMap(ref SystemState state, double time)
        {
            NativeParallelHashMap<Entity, byte> readyMap = new NativeParallelHashMap<Entity, byte>(8, Allocator.Temp);

            foreach ((RefRW<EM_Component_TradeTickState> tickState, RefRO<EM_Component_TradeSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_TradeTickState>, RefRO<EM_Component_TradeSettings>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TradeTickRate);

                if (time < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = time + intervalSeconds;
                readyMap.TryAdd(entity, 1);
            }

            return readyMap;
        }

        // Convert a tick rate (Hz) into interval seconds.
        private static float GetIntervalSeconds(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        // Collect candidate NPCs with available resources for trade.
        private void BuildCandidateLists(ref SystemState state, ref NativeList<Entity> candidates, ref NativeList<Entity> candidateSocieties)
        {
            foreach ((DynamicBuffer<EM_BufferElement_Resource> resources, EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Resource>, EM_Component_SocietyMember>().WithEntityAccess())
            {
                if (resources.Length == 0)
                    continue;

                candidates.Add(entity);
                candidateSocieties.Add(member.SocietyRoot);
            }
        }
        #endregion
    }
}
