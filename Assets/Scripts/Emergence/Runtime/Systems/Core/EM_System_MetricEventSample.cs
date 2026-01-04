using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_MetricCollect))]
    [UpdateBefore(typeof(EM_System_MetricSample))]
    public partial struct EM_System_MetricEventSample : ISystem
    {
        #region Fields
        #region Caches
        private NativeParallelHashMap<int, int> ruleGroupLookup;
        private bool ruleGroupLookupReady;
        #endregion

        #region Lookups
        private BufferLookup<EM_BufferElement_RuleCooldown> cooldownLookup;
        private ComponentLookup<EM_Component_RandomSeed> randomLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        private ComponentLookup<EM_Component_SocietyRoot> rootLookup;
        private ComponentLookup<EM_Component_SocietyProfileReference> profileLookup;
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_NeedSetting> needSettingLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private BufferLookup<EM_BufferElement_Relationship> relationshipLookup;
        private BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup;
        private ComponentLookup<EM_Component_NpcType> npcTypeLookup;
        private ComponentLookup<EM_Component_Reputation> reputationLookup;
        private ComponentLookup<EM_Component_Cohesion> cohesionLookup;
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup;
        private BufferLookup<EM_BufferElement_Intent> intentLookup;
        private BufferLookup<EM_BufferElement_SignalEvent> signalLookup;
        #endregion
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LibraryReference>();
            cooldownLookup = state.GetBufferLookup<EM_BufferElement_RuleCooldown>(false);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            rootLookup = state.GetComponentLookup<EM_Component_SocietyRoot>(true);
            profileLookup = state.GetComponentLookup<EM_Component_SocietyProfileReference>(true);
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            needSettingLookup = state.GetBufferLookup<EM_BufferElement_NeedSetting>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            relationshipLookup = state.GetBufferLookup<EM_BufferElement_Relationship>(false);
            relationshipTypeLookup = state.GetBufferLookup<EM_BufferElement_RelationshipType>(true);
            npcTypeLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            reputationLookup = state.GetComponentLookup<EM_Component_Reputation>(false);
            cohesionLookup = state.GetComponentLookup<EM_Component_Cohesion>(false);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleOverrideLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverride>(false);
            intentLookup = state.GetBufferLookup<EM_BufferElement_Intent>(false);
            signalLookup = state.GetBufferLookup<EM_BufferElement_SignalEvent>(false);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (ruleGroupLookup.IsCreated)
                ruleGroupLookup.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            EM_Component_LibraryReference libraryReference = SystemAPI.GetSingleton<EM_Component_LibraryReference>();

            if (!libraryReference.Value.IsCreated)
                return;

            ref EM_Blob_Library libraryBlob = ref libraryReference.Value.Value;
            ref BlobArray<EM_Blob_Metric> metrics = ref libraryBlob.Metrics;
            ref BlobArray<EM_Blob_RuleGroup> ruleGroups = ref libraryBlob.RuleGroups;

            if (metrics.Length == 0 || ruleGroups.Length == 0)
                return;

            EnsureRuleGroupLookup(ref libraryBlob);

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                EM_Component_Log debugLog = SystemAPI.GetSingleton<EM_Component_Log>();
                maxEntries = debugLog.MaxEntries;
            }

            cooldownLookup.Update(ref state);
            randomLookup.Update(ref state);
            memberLookup.Update(ref state);
            rootLookup.Update(ref state);
            profileLookup.Update(ref state);
            needLookup.Update(ref state);
            needSettingLookup.Update(ref state);
            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            relationshipTypeLookup.Update(ref state);
            npcTypeLookup.Update(ref state);
            reputationLookup.Update(ref state);
            cohesionLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleOverrideLookup.Update(ref state);
            intentLookup.Update(ref state);
            signalLookup.Update(ref state);

            EM_RuleEvaluationLookups evaluationLookups = new EM_RuleEvaluationLookups
            {
                CooldownLookup = cooldownLookup,
                RandomLookup = randomLookup,
                MemberLookup = memberLookup,
                RootLookup = rootLookup,
                NeedLookup = needLookup,
                NeedSettingLookup = needSettingLookup,
                ResourceLookup = resourceLookup,
                RelationshipLookup = relationshipLookup,
                RelationshipTypeLookup = relationshipTypeLookup,
                NpcTypeLookup = npcTypeLookup,
                ReputationLookup = reputationLookup,
                CohesionLookup = cohesionLookup,
                ScheduleLookup = scheduleLookup,
                ScheduleOverrideLookup = scheduleOverrideLookup,
                IntentLookup = intentLookup,
                SignalLookup = signalLookup
            };

            foreach ((DynamicBuffer<EM_BufferElement_MetricEventSample> eventSamples, Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_MetricEventSample>>()
                    .WithEntityAccess())
            {
                if (eventSamples.Length == 0)
                    continue;

                for (int i = 0; i < eventSamples.Length; i++)
                {
                    EM_BufferElement_MetricEventSample sample = eventSamples[i];

                    if (sample.MetricIndex < 0 || sample.MetricIndex >= metrics.Length)
                        continue;

                    Entity societyRoot = sample.SocietyRoot;

                    if (societyRoot == Entity.Null)
                        societyRoot = ResolveSocietyRoot(sample.Subject, memberLookup, rootLookup);

                    BlobAssetReference<EM_Blob_SocietyProfile> profileBlob;
                    bool hasProfile = EM_Utility_SocietyProfile.TryGetProfileReference(sample.Subject, societyRoot, profileLookup, out profileBlob);

                    EM_RuleEvaluation.TryEvaluateRules(ref libraryBlob, ref ruleGroupLookup, sample.MetricIndex, sample.NormalizedValue,
                        sample.Time, sample.Subject, societyRoot, sample.Target, sample.ContextId, hasProfile, profileBlob,
                        ref evaluationLookups, hasDebugBuffer, debugBuffer, maxEntries);
                }

                eventSamples.Clear();
            }
        }
        #endregion

        #region Helpers
        private void EnsureRuleGroupLookup(ref EM_Blob_Library libraryBlob)
        {
            if (ruleGroupLookupReady)
                return;

            ref BlobArray<EM_Blob_RuleGroup> groups = ref libraryBlob.RuleGroups;
            int count = math.max(groups.Length, 1);

            ruleGroupLookup = new NativeParallelHashMap<int, int>(count, Allocator.Persistent);

            for (int i = 0; i < groups.Length; i++)
            {
                ruleGroupLookup.TryAdd(groups[i].MetricIndex, i);
            }

            ruleGroupLookupReady = true;
        }

        private static Entity ResolveSocietyRoot(Entity subject,
            ComponentLookup<EM_Component_SocietyMember> memberLookup,
            ComponentLookup<EM_Component_SocietyRoot> rootLookup)
        {
            if (subject == Entity.Null)
                return Entity.Null;

            if (rootLookup.HasComponent(subject))
                return subject;

            if (memberLookup.HasComponent(subject))
                return memberLookup[subject].SocietyRoot;

            return Entity.Null;
        }
        #endregion
    }
}
