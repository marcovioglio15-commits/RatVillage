using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Attempts trade between NPCs based on needs and social affinity.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceNeedDecaySystem))]
    public partial struct EmergenceTradeSystem : ISystem
    {
        #region State
        private ComponentLookup<EmergenceSocietyClock> clockLookup;
        private BufferLookup<EmergenceResource> resourceLookup;
        private BufferLookup<EmergenceRelationship> relationshipLookup;
        private BufferLookup<EmergenceSignalEvent> signalLookup;
        private ComponentLookup<EmergenceTradeSettings> tradeSettingsLookup;
        private ComponentLookup<EmergenceRandomSeed> randomLookup;
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceNeedRule>();
            state.RequireForUpdate<EmergenceTradeSettings>();
            clockLookup = state.GetComponentLookup<EmergenceSocietyClock>(true);
            resourceLookup = state.GetBufferLookup<EmergenceResource>(false);
            relationshipLookup = state.GetBufferLookup<EmergenceRelationship>(false);
            signalLookup = state.GetBufferLookup<EmergenceSignalEvent>(false);
            tradeSettingsLookup = state.GetComponentLookup<EmergenceTradeSettings>(true);
            randomLookup = state.GetComponentLookup<EmergenceRandomSeed>(false);
        }

        /// <summary>
        /// Resolves trade attempts when societies are ready.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            NativeParallelHashMap<Entity, byte> readyMap = BuildReadyMap(ref state, time);

            if (readyMap.Count() == 0)
            {
                readyMap.Dispose();
                return;
            }

            // Resolve debug log access
            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer<EmergenceDebugEvent>(out DynamicBuffer<EmergenceDebugEvent> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                clockLookup.Update(ref state);
                EmergenceDebugLog debugLog = SystemAPI.GetSingleton<EmergenceDebugLog>();
                maxEntries = debugLog.MaxEntries;
            }

            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            signalLookup.Update(ref state);
            tradeSettingsLookup.Update(ref state);
            randomLookup.Update(ref state);

            NativeList<Entity> candidates = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> candidateSocieties = new NativeList<Entity>(Allocator.Temp);
            BuildCandidateLists(ref state, ref candidates, ref candidateSocieties);

            foreach ((DynamicBuffer<EmergenceNeed> needs, DynamicBuffer<EmergenceNeedRule> rules,
                DynamicBuffer<EmergenceNeedResolutionState> states, EmergenceSocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EmergenceNeed>, DynamicBuffer<EmergenceNeedRule>, DynamicBuffer<EmergenceNeedResolutionState>, EmergenceSocietyMember>()
                    .WithEntityAccess())
            {
                byte ready;
                bool isReady = readyMap.TryGetValue(member.SocietyRoot, out ready);

                if (!isReady)
                    continue;

                if (ready == 0)
                    continue;

                if (!tradeSettingsLookup.HasComponent(member.SocietyRoot))
                    continue;

                if (!randomLookup.HasComponent(entity))
                    continue;

                EmergenceTradeSettings tradeSettings = tradeSettingsLookup[member.SocietyRoot];
                EmergenceRandomSeed seed = randomLookup[entity];

                DynamicBuffer<EmergenceNeed> needBuffer = needs;
                DynamicBuffer<EmergenceNeedResolutionState> stateBuffer = states;

                EnsureResolutionStates(rules, stateBuffer);

                int ruleIndex;
                float probability;
                int stateIndex;
                bool hasRule = SelectNeedRule(needBuffer, rules, stateBuffer, time, out ruleIndex, out probability, out stateIndex);

                if (!hasRule)
                {
                    randomLookup[entity] = seed;
                    continue;
                }

                float attemptRoll = NextRandom01(ref seed);

                if (attemptRoll > probability)
                {
                    randomLookup[entity] = seed;
                    continue;
                }

                EmergenceNeedRule rule = rules[ruleIndex];
                stateBuffer[stateIndex] = new EmergenceNeedResolutionState
                {
                    NeedId = rule.NeedId,
                    NextAttemptTime = time + rule.CooldownSeconds
                };

                Entity partner;
                FixedString64Bytes reason;
                float transferAmount;
                bool tradeResolved = TryResolveTrade(entity, member.SocietyRoot, rule, tradeSettings, ref seed,
                    needBuffer, ref resourceLookup, ref relationshipLookup, ref signalLookup, candidates, candidateSocieties,
                    out partner, out reason, out transferAmount);

                if (hasDebugBuffer)
                {
                    float timeOfDay = GetSocietyTime(member.SocietyRoot, ref clockLookup);
                    AppendTradeDebugEvent(debugBuffer, maxEntries, EmergenceDebugEventType.TradeAttempt, timeOfDay, member.SocietyRoot,
                        entity, partner, rule.NeedId, rule.ResourceId, default, probability);

                    if (tradeResolved)
                    {
                        AppendTradeDebugEvent(debugBuffer, maxEntries, EmergenceDebugEventType.TradeSuccess, timeOfDay, member.SocietyRoot,
                            entity, partner, rule.NeedId, rule.ResourceId, default, transferAmount);
                    }
                    else
                    {
                        AppendTradeDebugEvent(debugBuffer, maxEntries, EmergenceDebugEventType.TradeFail, timeOfDay, member.SocietyRoot,
                            entity, partner, rule.NeedId, rule.ResourceId, reason, 0f);
                    }
                }

                randomLookup[entity] = seed;

                if (!tradeResolved)
                    continue;
            }

            candidates.Dispose();
            candidateSocieties.Dispose();
            readyMap.Dispose();
        }
        #endregion

        #region Helpers
        private NativeParallelHashMap<Entity, byte> BuildReadyMap(ref SystemState state, double time)
        {
            NativeParallelHashMap<Entity, byte> readyMap = new NativeParallelHashMap<Entity, byte>(8, Allocator.Temp);

            foreach ((RefRW<EmergenceTradeTickState> tickState, RefRO<EmergenceTradeSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EmergenceTradeTickState>, RefRO<EmergenceTradeSettings>>().WithAll<EmergenceSocietyRoot>().WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TradeTickRate);

                if (time < tickState.ValueRO.NextTick)
                    continue;

                tickState.ValueRW.NextTick = time + intervalSeconds;
                readyMap.TryAdd(entity, 1);
            }

            return readyMap;
        }

        private static float GetIntervalSeconds(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        private void BuildCandidateLists(ref SystemState state, ref NativeList<Entity> candidates, ref NativeList<Entity> candidateSocieties)
        {
            foreach ((DynamicBuffer<EmergenceResource> resources, EmergenceSocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EmergenceResource>, EmergenceSocietyMember>().WithEntityAccess())
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
