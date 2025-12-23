using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Updates needs based on decay rules at a controlled tick rate.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceSocietyClockSystem))]
    public partial struct EmergenceNeedDecaySystem : ISystem
    {
        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceNeedRule>();
        }

        /// <summary>
        /// Applies need decay on schedule.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            NativeParallelHashMap<Entity, float> deltaHoursMap = BuildDeltaHoursMap(ref state, time);

            if (deltaHoursMap.Count() == 0)
            {
                deltaHoursMap.Dispose();
                return;
            }

            foreach ((DynamicBuffer<EmergenceNeed> needs, DynamicBuffer<EmergenceNeedRule> rules, EmergenceSocietyMember member)
                in SystemAPI.Query<DynamicBuffer<EmergenceNeed>, DynamicBuffer<EmergenceNeedRule>, EmergenceSocietyMember>())
            {
                float deltaHours;
                bool found = deltaHoursMap.TryGetValue(member.SocietyRoot, out deltaHours);

                if (!found)
                    continue;

                ApplyNeedRules(needs, rules, deltaHours);
            }

            deltaHoursMap.Dispose();
        }
        #endregion

        #region Helpers
        private NativeParallelHashMap<Entity, float> BuildDeltaHoursMap(ref SystemState state, double time)
        {
            NativeParallelHashMap<Entity, float> deltaHoursMap = new NativeParallelHashMap<Entity, float>(8, Allocator.Temp);

            foreach ((RefRW<EmergenceNeedTickState> tickState, RefRO<EmergenceNeedTickSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EmergenceNeedTickState>, RefRO<EmergenceNeedTickSettings>>().WithAll<EmergenceSocietyRoot>().WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TickRate);

                if (time < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = time + intervalSeconds;
                deltaHoursMap.TryAdd(entity, intervalSeconds / 3600f);
            }

            return deltaHoursMap;
        }

        private static float GetIntervalSeconds(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        private static void ApplyNeedRules(DynamicBuffer<EmergenceNeed> needs, DynamicBuffer<EmergenceNeedRule> rules, float deltaHours)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                EmergenceNeedRule rule = rules[i];

                if (rule.NeedId.Length == 0)
                    continue;

                float minValue = math.min(rule.MinValue, rule.MaxValue);
                float maxValue = math.max(rule.MinValue, rule.MaxValue);

                int needIndex = FindNeedIndex(needs, rule.NeedId);

                if (needIndex < 0)
                {
                    EmergenceNeed newNeed = new EmergenceNeed
                    {
                        NeedId = rule.NeedId,
                        Value = math.clamp(minValue + rule.RatePerHour * deltaHours, minValue, maxValue)
                    };

                    needs.Add(newNeed);
                    continue;
                }

                EmergenceNeed need = needs[needIndex];
                float updatedValue = need.Value + rule.RatePerHour * deltaHours;
                need.Value = math.clamp(updatedValue, minValue, maxValue);
                needs[needIndex] = need;
            }
        }

        private static int FindNeedIndex(DynamicBuffer<EmergenceNeed> needs, FixedString64Bytes needId)
        {
            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }
        #endregion
    }
}
