using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_MetricCollect))]
    public partial struct EM_System_MetricSample : ISystem
    {
        #region Fields
        // Rule group lookup cache.
        #region Caches
        private NativeParallelHashMap<int, int> ruleGroupLookup;
        private bool ruleGroupLookupReady;
        #endregion

        // Runtime lookups used during sampling.
        #region Lookups
        private BufferLookup<EM_BufferElement_RuleCooldown> cooldownLookup;
        private ComponentLookup<EM_Component_RandomSeed> randomLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        private ComponentLookup<EM_Component_SocietyRoot> rootLookup;
        private ComponentLookup<EM_Component_SocietyProfileReference> profileLookup;
        private ComponentLookup<EM_Component_ScheduleOverrideSettings> scheduleOverrideSettingsLookup;
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_NeedSetting> needSettingLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private BufferLookup<EM_BufferElement_Relationship> relationshipLookup;
        private BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup;
        private ComponentLookup<EM_Component_NpcType> npcTypeLookup;
        private ComponentLookup<EM_Component_Reputation> reputationLookup;
        private ComponentLookup<EM_Component_Cohesion> cohesionLookup;
        private ComponentLookup<EM_Component_NpcHealth> healthLookup;
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverrideGate> scheduleOverrideGateLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverrideCooldownSettings> scheduleOverrideCooldownSettingsLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverrideCooldownState> scheduleOverrideCooldownStateLookup;
        private BufferLookup<EM_BufferElement_Intent> intentLookup;
        private BufferLookup<EM_BufferElement_SignalEvent> signalLookup;
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        #endregion
        #endregion

        #region Unity Lifecycle
        // Setup persistent lookup caches.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LibraryReference>();
            cooldownLookup = state.GetBufferLookup<EM_BufferElement_RuleCooldown>(false);
            randomLookup = state.GetComponentLookup<EM_Component_RandomSeed>(false);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            rootLookup = state.GetComponentLookup<EM_Component_SocietyRoot>(true);
            profileLookup = state.GetComponentLookup<EM_Component_SocietyProfileReference>(true);
            scheduleOverrideSettingsLookup = state.GetComponentLookup<EM_Component_ScheduleOverrideSettings>(true);
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            needSettingLookup = state.GetBufferLookup<EM_BufferElement_NeedSetting>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            relationshipLookup = state.GetBufferLookup<EM_BufferElement_Relationship>(false);
            relationshipTypeLookup = state.GetBufferLookup<EM_BufferElement_RelationshipType>(true);
            npcTypeLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            reputationLookup = state.GetComponentLookup<EM_Component_Reputation>(false);
            cohesionLookup = state.GetComponentLookup<EM_Component_Cohesion>(false);
            healthLookup = state.GetComponentLookup<EM_Component_NpcHealth>(false);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleOverrideLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverride>(false);
            scheduleOverrideGateLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverrideGate>(false);
            scheduleOverrideCooldownSettingsLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverrideCooldownSettings>(true);
            scheduleOverrideCooldownStateLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverrideCooldownState>(true);
            intentLookup = state.GetBufferLookup<EM_BufferElement_Intent>(false);
            signalLookup = state.GetBufferLookup<EM_BufferElement_SignalEvent>(false);
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
        }

        // Dispose persistent caches when the system is destroyed.
        public void OnDestroy(ref SystemState state)
        {
            if (ruleGroupLookup.IsCreated)
                ruleGroupLookup.Dispose();
        }

        // Sample metrics, evaluate rules, and apply effects.
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

            bool hasSampleBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_BufferElement_MetricSample> sampleBuffer);
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

            cooldownLookup.Update(ref state);
            randomLookup.Update(ref state);
            memberLookup.Update(ref state);
            rootLookup.Update(ref state);
            profileLookup.Update(ref state);
            scheduleOverrideSettingsLookup.Update(ref state);
            needLookup.Update(ref state);
            needSettingLookup.Update(ref state);
            resourceLookup.Update(ref state);
            relationshipLookup.Update(ref state);
            relationshipTypeLookup.Update(ref state);
            npcTypeLookup.Update(ref state);
            reputationLookup.Update(ref state);
            cohesionLookup.Update(ref state);
            healthLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleOverrideLookup.Update(ref state);
            scheduleOverrideGateLookup.Update(ref state);
            scheduleOverrideCooldownSettingsLookup.Update(ref state);
            scheduleOverrideCooldownStateLookup.Update(ref state);
            intentLookup.Update(ref state);
            signalLookup.Update(ref state);
            clockLookup.Update(ref state);

            EM_RuleEvaluationLookups evaluationLookups = new EM_RuleEvaluationLookups
            {
                CooldownLookup = cooldownLookup,
                RandomLookup = randomLookup,
                MemberLookup = memberLookup,
                RootLookup = rootLookup,
                ScheduleOverrideSettingsLookup = scheduleOverrideSettingsLookup,
                NeedLookup = needLookup,
                NeedSettingLookup = needSettingLookup,
                ResourceLookup = resourceLookup,
                RelationshipLookup = relationshipLookup,
                RelationshipTypeLookup = relationshipTypeLookup,
                NpcTypeLookup = npcTypeLookup,
                ReputationLookup = reputationLookup,
                CohesionLookup = cohesionLookup,
                HealthLookup = healthLookup,
                ScheduleLookup = scheduleLookup,
                ScheduleOverrideLookup = scheduleOverrideLookup,
                ScheduleOverrideGateLookup = scheduleOverrideGateLookup,
                ScheduleOverrideCooldownSettingsLookup = scheduleOverrideCooldownSettingsLookup,
                ScheduleOverrideCooldownStateLookup = scheduleOverrideCooldownStateLookup,
                IntentLookup = intentLookup,
                SignalLookup = signalLookup
            };

            foreach ((DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators,
                DynamicBuffer<EM_BufferElement_MetricTimer> timers,
                Entity subject)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_MetricAccumulator>, DynamicBuffer<EM_BufferElement_MetricTimer>>()
                    .WithAll<EM_Component_MetricInitialized>()
                    .WithEntityAccess())
            {
                DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulatorBuffer = accumulators;
                DynamicBuffer<EM_BufferElement_MetricTimer> timerBuffer = timers;
                int entryCount = timerBuffer.Length;

                if (entryCount == 0 || accumulatorBuffer.Length == 0)
                    continue;

                Entity societyRoot = ResolveSocietyRoot(subject, memberLookup, rootLookup);

                if (societyRoot == Entity.Null)
                    continue;

                if (!clockLookup.HasComponent(societyRoot))
                    continue;

                double timeSeconds = clockLookup[societyRoot].SimulatedTimeSeconds;
                bool hasProfile = EM_Utility_SocietyProfile.TryGetProfileReference(subject, societyRoot, profileLookup, out BlobAssetReference<EM_Blob_SocietyProfile> profileBlob);

                for (int i = 0; i < entryCount; i++)
                {
                    EM_BufferElement_MetricTimer timer = timerBuffer[i];

                    if (timeSeconds < timer.NextSampleTime)
                        continue;

                    if (timer.MetricIndex < 0 || timer.MetricIndex >= metrics.Length)
                        continue;

                    if (!TryGetAccumulator(timer.MetricIndex, i, accumulatorBuffer, out EM_BufferElement_MetricAccumulator accumulator, out int accumulatorIndex))
                        continue;

                    EM_Blob_Metric metric = metrics[timer.MetricIndex];
                    float interval = EM_Utility_Metric.GetInterval(metric.SampleInterval);
                    float value = SampleAccumulator(metric, accumulator, interval);
                    float normalized = EM_Utility_Metric.Normalize(metric.Normalization, value);

                    timer.NextSampleTime = timeSeconds + interval;
                    timerBuffer[i] = timer;

                    ResetAccumulator(accumulatorIndex, accumulatorBuffer);

                    if (hasSampleBuffer)
                        AppendSample(sampleBuffer, metric.MetricId, value, normalized, timeSeconds, subject, societyRoot);

                    if (accumulator.Count <= 0)
                        continue;

                    if (!EM_RuleEvaluation.TryEvaluateRules(ref libraryBlob, ref ruleGroupLookup, timer.MetricIndex, normalized, timeSeconds,
                        subject, societyRoot, Entity.Null, default, hasProfile, profileBlob, ref evaluationLookups,
                        hasDebugBuffer, debugBuffer, maxEntries, ref debugLog))
                        continue;
                }
            }

            if (hasDebugBuffer)
                debugLogRef.ValueRW = debugLog;
        }
        #endregion
    }
}
