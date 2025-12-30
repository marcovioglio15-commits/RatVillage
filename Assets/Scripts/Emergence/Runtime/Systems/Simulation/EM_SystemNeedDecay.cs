using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_SocietyClock))]
    public partial struct EM_System_NeedDecay : ISystem
    {
        #region Unity LyfeCycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_BufferElement_NeedRule>();
        }

        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            NativeParallelHashMap<Entity, float> deltaHoursMap = BuildDeltaHoursMap(ref state, time);

            if (deltaHoursMap.Count() == 0)
            {
                deltaHoursMap.Dispose();
                return;
            }

            foreach ((DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedRule> rules, EM_Component_SocietyMember member)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Need>, DynamicBuffer<EM_BufferElement_NeedRule>, EM_Component_SocietyMember>())
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

            foreach ((RefRW<EM_Component_NeedTickState> tickState, RefRO<EM_Component_NeedTickSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_NeedTickState>, RefRO<EM_Component_NeedTickSettings>>().WithAll<EM_Component_SocietyRoot>().WithEntityAccess())
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

        private static void ApplyNeedRules(DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedRule> rules, float deltaHours)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                EM_BufferElement_NeedRule rule = rules[i];

                if (rule.NeedId.Length == 0)
                    continue;

                float minValue = math.min(rule.MinValue, rule.MaxValue);
                float maxValue = math.max(rule.MinValue, rule.MaxValue);
                float range = maxValue - minValue;

                int needIndex = FindNeedIndex(needs, rule.NeedId);
                float currentValue = minValue;

                if (needIndex >= 0)
                    currentValue = needs[needIndex].Value;

                float normalized = 0f;

                if (range > 0f)
                    normalized = math.saturate((currentValue - minValue) / range);

                float ratePerHour = SampleRatePerHour(in rule.RatePerHourSamples, normalized);
                float delta = ratePerHour * deltaHours;

                if (needIndex < 0)
                {
                    EM_BufferElement_Need newNeed = new EM_BufferElement_Need
                    {
                        NeedId = rule.NeedId,
                        Value = math.clamp(minValue + delta, minValue, maxValue)
                    };

                    needs.Add(newNeed);
                    continue;
                }

                EM_BufferElement_Need need = needs[needIndex];
                need.Value = math.clamp(need.Value + delta, minValue, maxValue);
                needs[needIndex] = need;
            }
        }

        // Need rate curve sampling for decay.
        private static float SampleRatePerHour(in FixedList128Bytes<float> samples, float normalized)
        {
            int count = samples.Length;

            if (count <= 0)
                return 0f;

            if (count == 1)
                return samples[0];

            float t = normalized;
            float scaled = t * (count - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, count - 1);
            float lerp = scaled - index;

            return math.lerp(samples[index], samples[nextIndex], lerp);
        }

        private static int FindNeedIndex(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId)
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
