using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

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
        private ComponentLookup<EM_Component_NpcLocationState> locationStateLookup;
        private ComponentLookup<EM_Component_LocationAnchor> locationAnchorLookup;
        private ComponentLookup<EM_Component_NpcNavigationState> navigationLookup;
        private ComponentLookup<EM_Component_NpcTradePreferences> tradePreferencesLookup;
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleTarget> scheduleTargetLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup;
        private ComponentLookup<EM_Component_TradeProviderState> tradeProviderLookup;
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        private BufferLookup<EM_BufferElement_Intent> intentLookup;
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_NeedSetting> needSettingLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private BufferLookup<EM_BufferElement_Relationship> relationshipLookup;
        private BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup;
        private BufferLookup<EM_BufferElement_TradeAttemptedProvider> attemptedProviderLookup;
        private BufferLookup<EM_BufferElement_TradeQueueEntry> tradeQueueLookup;
        private BufferLookup<EM_BufferElement_LocationOccupancy> locationOccupancyLookup;
        private BufferLookup<EM_BufferElement_LocationReservation> locationReservationLookup;
        private BufferLookup<EM_BufferElement_SignalEvent> signalLookup;
        #endregion

        #region Grid Cache
        private Entity currentGridEntity;
        private EM_Component_LocationGrid currentGrid;
        #endregion
        #endregion

        #region Unity Lifecycle
        // Prepare caches for intent resolution and trade evaluation.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_TradeSettings>();
            state.RequireForUpdate<EM_Component_LocationGrid>();
            state.RequireForUpdate<EM_BufferElement_Intent>();
            tradeSettingsLookup = state.GetComponentLookup<EM_Component_TradeSettings>(true);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            npcTypeLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            locationStateLookup = state.GetComponentLookup<EM_Component_NpcLocationState>(true);
            locationAnchorLookup = state.GetComponentLookup<EM_Component_LocationAnchor>(true);
            navigationLookup = state.GetComponentLookup<EM_Component_NpcNavigationState>(false);
            tradePreferencesLookup = state.GetComponentLookup<EM_Component_NpcTradePreferences>(true);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleTargetLookup = state.GetComponentLookup<EM_Component_NpcScheduleTarget>(true);
            scheduleOverrideLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverride>(false);
            tradeProviderLookup = state.GetComponentLookup<EM_Component_TradeProviderState>(false);
            transformLookup = state.GetComponentLookup<LocalTransform>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            intentLookup = state.GetBufferLookup<EM_BufferElement_Intent>(false);
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            needSettingLookup = state.GetBufferLookup<EM_BufferElement_NeedSetting>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            relationshipLookup = state.GetBufferLookup<EM_BufferElement_Relationship>(false);
            relationshipTypeLookup = state.GetBufferLookup<EM_BufferElement_RelationshipType>(true);
            attemptedProviderLookup = state.GetBufferLookup<EM_BufferElement_TradeAttemptedProvider>(false);
            tradeQueueLookup = state.GetBufferLookup<EM_BufferElement_TradeQueueEntry>(false);
            locationOccupancyLookup = state.GetBufferLookup<EM_BufferElement_LocationOccupancy>(false);
            locationReservationLookup = state.GetBufferLookup<EM_BufferElement_LocationReservation>(false);
            signalLookup = state.GetBufferLookup<EM_BufferElement_SignalEvent>(false);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!TryResolveGrid(ref state, out currentGridEntity, out currentGrid))
                return;

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
            locationStateLookup.Update(ref state);
            locationAnchorLookup.Update(ref state);
            navigationLookup.Update(ref state);
            tradePreferencesLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleTargetLookup.Update(ref state);
            scheduleOverrideLookup.Update(ref state);
            tradeProviderLookup.Update(ref state);
            transformLookup.Update(ref state);
            memberLookup.Update(ref state);
            intentLookup.Update(ref state);
            needLookup.Update(ref state);
            needSettingLookup.Update(ref state);
            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            relationshipTypeLookup.Update(ref state);
            attemptedProviderLookup.Update(ref state);
            tradeQueueLookup.Update(ref state);
            locationOccupancyLookup.Update(ref state);
            locationReservationLookup.Update(ref state);
            signalLookup.Update(ref state);

            NativeList<Entity> candidates = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> candidateSocieties = new NativeList<Entity>(Allocator.Temp);
            BuildCandidateLists(ref state, ref candidates, ref candidateSocieties);
            int providerCapacity = candidates.Length;

            if (providerCapacity < 1)
                providerCapacity = 1;

            NativeParallelHashSet<Entity> providerLock = new NativeParallelHashSet<Entity>(providerCapacity, Allocator.Temp);

            foreach ((RefRO<EM_Component_NpcTradeInteraction> tradeInteraction, RefRW<EM_Component_TradeRequestState> tradeRequest,
                RefRO<EM_Component_NpcLocationState> locationState, RefRO<EM_Component_NpcScheduleTarget> scheduleTarget,
                RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, RefRW<EM_Component_NpcNavigationState> navigationState,
                RefRO<LocalTransform> transform, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcTradeInteraction>, RefRW<EM_Component_TradeRequestState>,
                    RefRO<EM_Component_NpcLocationState>, RefRO<EM_Component_NpcScheduleTarget>,
                    RefRW<EM_Component_NpcScheduleOverride>, RefRW<EM_Component_NpcNavigationState>, RefRO<LocalTransform>>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithEntityAccess())
            {
                if (!memberLookup.HasComponent(entity))
                    continue;

                EM_Component_SocietyMember member = memberLookup[entity];
                double timeSeconds;
                bool isReady = readyMap.TryGetValue(member.SocietyRoot, out timeSeconds);

                if (!isReady)
                    continue;

                if (!tradeSettingsLookup.HasComponent(member.SocietyRoot))
                    continue;

                if (!randomLookup.HasComponent(entity))
                    continue;

                if (!attemptedProviderLookup.HasBuffer(entity))
                    continue;

                if (!intentLookup.HasBuffer(entity))
                    continue;

                if (!needLookup.HasBuffer(entity))
                    continue;

                if (!needSettingLookup.HasBuffer(entity))
                    continue;

                if (!resourceLookup.HasBuffer(entity))
                    continue;

                if (!signalLookup.HasBuffer(entity))
                    continue;

                BlobAssetReference<EM_BlobDefinition_NpcSchedule> tradeSchedule;
                int tradeEntryIndex;

                if (!TryGetTradeEntry(entity, ref scheduleLookup, ref scheduleTargetLookup, out tradeSchedule, out tradeEntryIndex))
                    continue;

                ref EM_BlobDefinition_NpcSchedule scheduleDefinition = ref tradeSchedule.Value;
                ref BlobArray<EM_Blob_NpcScheduleEntry> tradeEntries = ref scheduleDefinition.Entries;
                ref EM_Blob_NpcScheduleEntry tradeEntry = ref tradeEntries[tradeEntryIndex];

                EM_Component_TradeSettings tradeSettings = tradeSettingsLookup[member.SocietyRoot];
                EM_Component_RandomSeed seed = randomLookup[entity];
                DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders = attemptedProviderLookup[entity];
                DynamicBuffer<EM_BufferElement_Intent> intents = intentLookup[entity];
                DynamicBuffer<EM_BufferElement_Need> needs = needLookup[entity];
                DynamicBuffer<EM_BufferElement_NeedSetting> settings = needSettingLookup[entity];
                DynamicBuffer<EM_BufferElement_Resource> resources = resourceLookup[entity];
                DynamicBuffer<EM_BufferElement_SignalEvent> signals = signalLookup[entity];

                ProcessTradeRequest(entity, member.SocietyRoot, timeSeconds, tradeSettings, ref tradeEntry, ref seed, tradeInteraction, scheduleTarget,
                    scheduleOverride, locationState, transform, navigationState, tradeRequest, attemptedProviders, intents, needs, settings, resources,
                    signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

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
