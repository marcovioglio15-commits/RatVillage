using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Helpers
        // Build the per-society readiness map for trade ticks.
        private NativeParallelHashMap<Entity, double> BuildReadyMap(ref SystemState state)
        {
            NativeParallelHashMap<Entity, double> readyMap = new NativeParallelHashMap<Entity, double>(8, Allocator.Temp);

            foreach ((RefRW<EM_Component_TradeTickState> tickState, RefRO<EM_Component_TradeSettings> settings,
                RefRO<EM_Component_SocietyClock> clock, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_TradeTickState>, RefRO<EM_Component_TradeSettings>, RefRO<EM_Component_SocietyClock>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                double timeSeconds = clock.ValueRO.SimulatedTimeSeconds;
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TradeTickIntervalHours);

                if (timeSeconds < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = timeSeconds + intervalSeconds;
                readyMap.TryAdd(entity, timeSeconds);
            }

            return readyMap;
        }

        private void ForceReadySocietiesWithOverrides(ref SystemState state, ref NativeParallelHashMap<Entity, double> readyMap)
        {
            foreach ((RefRO<EM_Component_SocietyMember> member, RefRO<EM_Component_NpcScheduleTarget> scheduleTarget)
                in SystemAPI.Query<RefRO<EM_Component_SocietyMember>, RefRO<EM_Component_NpcScheduleTarget>>())
            {
                if (scheduleTarget.ValueRO.IsOverride == 0)
                    continue;

                Entity societyRoot = member.ValueRO.SocietyRoot;

                if (societyRoot == Entity.Null || readyMap.ContainsKey(societyRoot))
                    continue;

                if (!clockLookup.HasComponent(societyRoot))
                    continue;

                readyMap.TryAdd(societyRoot, clockLookup[societyRoot].SimulatedTimeSeconds);
            }
        }

        // Convert a tick interval in simulated hours into seconds.
        private static float GetIntervalSeconds(float intervalHours)
        {
            if (intervalHours <= 0f)
                return 1f;

            return intervalHours * 3600f;
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
