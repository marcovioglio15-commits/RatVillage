using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_SocietyClock))]
    [UpdateBefore(typeof(EM_System_NeedUpdate))]
    public partial struct EM_System_NpcNeedInitialization : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private ComponentLookup<EM_Component_NeedTickSettings> tickSettingsLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcNeedTickState>();
            state.RequireForUpdate<EM_Component_RandomSeed>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            tickSettingsLookup = state.GetComponentLookup<EM_Component_NeedTickSettings>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            clockLookup.Update(ref state);
            tickSettingsLookup.Update(ref state);

            foreach ((RefRW<EM_Component_NpcNeedTickState> tickState,
                RefRO<EM_Component_NpcNeedRateSettings> rateSettings,
                RefRW<EM_Component_RandomSeed> randomSeed,
                DynamicBuffer<EM_BufferElement_NeedSetting> needSettings,
                EM_Component_SocietyMember member)
                in SystemAPI.Query<RefRW<EM_Component_NpcNeedTickState>, RefRO<EM_Component_NpcNeedRateSettings>,
                    RefRW<EM_Component_RandomSeed>, DynamicBuffer<EM_BufferElement_NeedSetting>, EM_Component_SocietyMember>())
            {
                Entity societyRoot = member.SocietyRoot;

                if (societyRoot == Entity.Null)
                    continue;

                if (!clockLookup.HasComponent(societyRoot))
                    continue;

                if (!tickSettingsLookup.HasComponent(societyRoot))
                    continue;

                bool needsTickInit = tickState.ValueRO.NextTick < 0d;
                bool needsRateInit = NeedsRateInitialization(needSettings);

                if (!needsTickInit && !needsRateInit)
                    continue;

                EM_Component_SocietyClock clock = clockLookup[societyRoot];
                EM_Component_NeedTickSettings tickSettings = tickSettingsLookup[societyRoot];
                float intervalSeconds = GetIntervalSeconds(tickSettings.TickIntervalHours);
                double timeSeconds = clock.SimulatedTimeSeconds;
                EM_Component_RandomSeed seed = randomSeed.ValueRO;

                if (needsTickInit)
                {
                    float offsetSeconds = intervalSeconds * NextRandom01(ref seed);
                    EM_Component_NpcNeedTickState updatedTick = tickState.ValueRO;
                    updatedTick.NextTick = timeSeconds + offsetSeconds;
                    tickState.ValueRW = updatedTick;
                }

                if (needsRateInit)
                {
                    float variance = math.max(0f, rateSettings.ValueRO.RateMultiplierVariance);
                    float minMultiplier = math.max(0f, 1f - variance);
                    float maxMultiplier = math.max(minMultiplier, 1f + variance);
                    DynamicBuffer<EM_BufferElement_NeedSetting> settingsBuffer = needSettings;

                    for (int i = 0; i < settingsBuffer.Length; i++)
                    {
                        EM_BufferElement_NeedSetting setting = settingsBuffer[i];

                        if (setting.RateMultiplier > 0f)
                            continue;

                        float t = NextRandom01(ref seed);
                        setting.RateMultiplier = math.lerp(minMultiplier, maxMultiplier, t);
                        settingsBuffer[i] = setting;
                    }
                }

                randomSeed.ValueRW = seed;
            }
        }
        #endregion

        #region Helpers
        private static bool NeedsRateInitialization(DynamicBuffer<EM_BufferElement_NeedSetting> settings)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i].RateMultiplier > 0f)
                    continue;

                return true;
            }

            return false;
        }

        private static float GetIntervalSeconds(float intervalHours)
        {
            if (intervalHours <= 0f)
                return 1f;

            return intervalHours * 3600f;
        }

        private static float NextRandom01(ref EM_Component_RandomSeed seed)
        {
            uint current = seed.Value;

            if (current == 0u)
                current = 1u;

            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(current);
            float value = random.NextFloat();
            seed.Value = random.NextUInt();

            return value;
        }
        #endregion
    }
}
