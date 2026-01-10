using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NeedUpdate))]
    public partial struct EM_System_Trade : ISystem
    {
        #region Fields
        #region Lookups
        private ComponentLookup<EM_Component_TradeSettings> tradeSettingsLookup;
        private ComponentLookup<EM_Component_RandomSeed> randomLookup;
        private ComponentLookup<EM_Component_NpcType> npcTypeLookup;
        private ComponentLookup<EM_Component_NpcTradePreferences> tradePreferencesLookup;
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleState> scheduleStateLookup;
        private BufferLookup<EM_BufferElement_Intent> intentLookup;
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_NeedSetting> needSettingLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private BufferLookup<EM_BufferElement_Relationship> relationshipLookup;
        private BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup;
        #endregion
        #endregion

        #region Unity Lifecycle
        // Prepare caches for intent resolution and trade evaluation.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_TradeSettings>();
            state.RequireForUpdate<EM_BufferElement_Intent>();
            tradeSettingsLookup = state.GetComponentLookup<EM_Component_TradeSettings>(true);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            npcTypeLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            tradePreferencesLookup = state.GetComponentLookup<EM_Component_NpcTradePreferences>(true);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleStateLookup = state.GetComponentLookup<EM_Component_NpcScheduleState>(true);
            intentLookup = state.GetBufferLookup<EM_BufferElement_Intent>(false);
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            needSettingLookup = state.GetBufferLookup<EM_BufferElement_NeedSetting>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            relationshipLookup = state.GetBufferLookup<EM_BufferElement_Relationship>(false);
            relationshipTypeLookup = state.GetBufferLookup<EM_BufferElement_RelationshipType>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<Entity, double> readyMap = BuildReadyMap(ref state);

            if (readyMap.Count() == 0)
            {
                readyMap.Dispose();
                return;
            }

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;
            EM_Component_Log debugLog = default;
            RefRW<EM_Component_Log> debugLogRef = default;

            if (hasDebugBuffer)
            {
                debugLogRef = SystemAPI.GetSingletonRW<EM_Component_Log>();
                debugLog = debugLogRef.ValueRO;
                maxEntries = debugLog.MaxEntries;
            }

            tradeSettingsLookup.Update(ref state);
            randomLookup.Update(ref state);
            npcTypeLookup.Update(ref state);
            tradePreferencesLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleStateLookup.Update(ref state);
            intentLookup.Update(ref state);
            needLookup.Update(ref state);
            needSettingLookup.Update(ref state);
            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            relationshipTypeLookup.Update(ref state);

            NativeList<Entity> candidates = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> candidateSocieties = new NativeList<Entity>(Allocator.Temp);
            BuildCandidateLists(ref state, ref candidates, ref candidateSocieties);
            int providerCapacity = candidates.Length;

            if (providerCapacity < 1)
                providerCapacity = 1;

            NativeParallelHashSet<Entity> providerLock = new NativeParallelHashSet<Entity>(providerCapacity, Allocator.Temp);

            foreach ((DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs,
                DynamicBuffer<EM_BufferElement_NeedSetting> settings, DynamicBuffer<EM_BufferElement_Resource> resources,
                DynamicBuffer<EM_BufferElement_SignalEvent> signals, EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_Intent>, DynamicBuffer<EM_BufferElement_Need>,
                    DynamicBuffer<EM_BufferElement_NeedSetting>, DynamicBuffer<EM_BufferElement_Resource>,
                    DynamicBuffer<EM_BufferElement_SignalEvent>, EM_Component_SocietyMember>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                double timeSeconds;
                bool isReady = readyMap.TryGetValue(member.SocietyRoot, out timeSeconds);

                if (!isReady)
                    continue;

                if (!tradeSettingsLookup.HasComponent(member.SocietyRoot))
                    continue;

                if (!randomLookup.HasComponent(entity))
                    continue;

                BlobAssetReference<EM_BlobDefinition_NpcSchedule> tradeSchedule;
                int tradeEntryIndex;

                if (!TryGetTradeEntry(entity, ref scheduleLookup, ref scheduleStateLookup, out tradeSchedule, out tradeEntryIndex))
                    continue;

                ref EM_BlobDefinition_NpcSchedule scheduleDefinition = ref tradeSchedule.Value;
                ref BlobArray<EM_Blob_NpcScheduleEntry> tradeEntries = ref scheduleDefinition.Entries;
                ref EM_Blob_NpcScheduleEntry tradeEntry = ref tradeEntries[tradeEntryIndex];

                if ((EM_ScheduleTradePolicy)tradeEntry.TradePolicy == EM_ScheduleTradePolicy.BlockAll)
                    continue;

                EM_Component_TradeSettings tradeSettings = tradeSettingsLookup[member.SocietyRoot];
                EM_Component_RandomSeed seed = randomLookup[entity];

                TryResolveIntent(entity, member.SocietyRoot, tradeSettings, timeSeconds, ref tradeEntry, ref seed, intents, needs, settings, resources, signals,
                    ref resourceLookup, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup, ref tradePreferencesLookup,
                    candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                randomLookup[entity] = seed;
            }

            if (hasDebugBuffer)
                debugLogRef.ValueRW = debugLog;

            candidates.Dispose();
            candidateSocieties.Dispose();
            providerLock.Dispose();
            readyMap.Dispose();
        }
        #endregion
    }
}
