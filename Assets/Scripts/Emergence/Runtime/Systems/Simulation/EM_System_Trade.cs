using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NeedDecay))]
    public partial struct EM_System_Trade : ISystem
    {
        #region Fields

        #region Lookup
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private BufferLookup<EM_BufferElement_Relationship> relationshipLookup;
        private BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup;
        private BufferLookup<EM_BufferElement_SignalEvent> signalLookup;
        private ComponentLookup<EM_Component_TradeSettings> tradeSettingsLookup;
        private ComponentLookup<EM_Component_RandomSeed> randomLookup;
        private ComponentLookup<EM_Component_NpcType> npcTypeLookup;
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_BufferElement_NeedRule>();
            state.RequireForUpdate<EM_Component_TradeSettings>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            relationshipLookup = state.GetBufferLookup<EM_BufferElement_Relationship>(false);
            relationshipTypeLookup = state.GetBufferLookup<EM_BufferElement_RelationshipType>(true);
            signalLookup = state.GetBufferLookup<EM_BufferElement_SignalEvent>(false);
            tradeSettingsLookup = state.GetComponentLookup<EM_Component_TradeSettings>(true);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            npcTypeLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            NativeParallelHashMap<Entity, byte> readyMap = BuildReadyMap(ref state, time);

            if (readyMap.Count() == 0)
            {
                readyMap.Dispose();
                return;
            }

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer<EM_Component_Event>(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                clockLookup.Update(ref state);
                EM_Component_Log debugLog = SystemAPI.GetSingleton<EM_Component_Log>();
                maxEntries = debugLog.MaxEntries;
            }

            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            relationshipTypeLookup.Update(ref state);
            signalLookup.Update(ref state);
            tradeSettingsLookup.Update(ref state);
            randomLookup.Update(ref state);
            npcTypeLookup.Update(ref state);

            NativeList<Entity> candidates = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> candidateSocieties = new NativeList<Entity>(Allocator.Temp);
            BuildCandidateLists(ref state, ref candidates, ref candidateSocieties);

            foreach ((DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedRule> rules,
                DynamicBuffer<EM_BufferElement_NeedResolutionState> states, EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Need>, DynamicBuffer<EM_BufferElement_NeedRule>, DynamicBuffer<EM_BufferElement_NeedResolutionState>, EM_Component_SocietyMember>()
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

                EM_Component_TradeSettings tradeSettings = tradeSettingsLookup[member.SocietyRoot];
                EM_Component_RandomSeed seed = randomLookup[entity];

                DynamicBuffer<EM_BufferElement_Need> needBuffer = needs;
                DynamicBuffer<EM_BufferElement_NeedResolutionState> stateBuffer = states;

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

                EM_BufferElement_NeedRule rule = rules[ruleIndex];
                stateBuffer[stateIndex] = new EM_BufferElement_NeedResolutionState
                {
                    NeedId = rule.NeedId,
                    NextAttemptTime = time + rule.CooldownSeconds
                };

                Entity partner;
                FixedString64Bytes reason;
                float transferAmount;
                bool tradeResolved = TryResolveTrade(entity, member.SocietyRoot, rule, tradeSettings, ref seed,
                    needBuffer, ref resourceLookup, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup,
                    ref signalLookup, candidates, candidateSocieties, out partner, out reason, out transferAmount);

                if (hasDebugBuffer)
                {
                    float timeOfDay = GetSocietyTime(member.SocietyRoot, ref clockLookup);
                    AppendTradeDebugEvent(debugBuffer, maxEntries, EM_DebugEventType.TradeAttempt, timeOfDay, member.SocietyRoot,
                        entity, partner, rule.NeedId, rule.ResourceId, default, probability);

                    if (tradeResolved)
                    {
                        AppendTradeDebugEvent(debugBuffer, maxEntries, EM_DebugEventType.TradeSuccess, timeOfDay, member.SocietyRoot,
                            entity, partner, rule.NeedId, rule.ResourceId, default, transferAmount);
                    }
                    else
                    {
                        AppendTradeDebugEvent(debugBuffer, maxEntries, EM_DebugEventType.TradeFail, timeOfDay, member.SocietyRoot,
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

            foreach ((RefRW<EM_Component_TradeTickState> tickState, RefRO<EM_Component_TradeSettings> settings, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_TradeTickState>, RefRO<EM_Component_TradeSettings>>().WithAll<EM_Component_SocietyRoot>().WithEntityAccess())
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

        #endregion
    }
}
