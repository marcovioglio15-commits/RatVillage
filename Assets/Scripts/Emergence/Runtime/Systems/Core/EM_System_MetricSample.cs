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
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private ComponentLookup<EM_Component_Reputation> reputationLookup;
        private ComponentLookup<EM_Component_Cohesion> cohesionLookup;
        private ComponentLookup<EM_Component_NpcSchedule> scheduleLookup;
        private ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup;
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
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            reputationLookup = state.GetComponentLookup<EM_Component_Reputation>(false);
            cohesionLookup = state.GetComponentLookup<EM_Component_Cohesion>(false);
            scheduleLookup = state.GetComponentLookup<EM_Component_NpcSchedule>(true);
            scheduleOverrideLookup = state.GetComponentLookup<EM_Component_NpcScheduleOverride>(false);
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

            double time = SystemAPI.Time.ElapsedTime;
            bool hasSampleBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_BufferElement_MetricSample> sampleBuffer);

            cooldownLookup.Update(ref state);
            randomLookup.Update(ref state);
            memberLookup.Update(ref state);
            rootLookup.Update(ref state);
            profileLookup.Update(ref state);
            needLookup.Update(ref state);
            resourceLookup.Update(ref state);
            reputationLookup.Update(ref state);
            cohesionLookup.Update(ref state);
            scheduleLookup.Update(ref state);
            scheduleOverrideLookup.Update(ref state);

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
                bool hasProfile = TryGetProfileReference(subject, societyRoot, profileLookup, out BlobAssetReference<EM_Blob_SocietyProfile> profileBlob);

                for (int i = 0; i < entryCount; i++)
                {
                    EM_BufferElement_MetricTimer timer = timerBuffer[i];

                    if (time < timer.NextSampleTime)
                        continue;

                    if (timer.MetricIndex < 0 || timer.MetricIndex >= metrics.Length)
                        continue;

                    if (!TryGetAccumulator(timer.MetricIndex, i, accumulatorBuffer, out EM_BufferElement_MetricAccumulator accumulator, out int accumulatorIndex))
                        continue;

                    EM_Blob_Metric metric = metrics[timer.MetricIndex];
                    float interval = GetInterval(metric.SampleInterval);
                    float value = SampleAccumulator(metric, accumulator, interval);
                    float normalized = Normalize(metric.Normalization, value);

                    timer.NextSampleTime = time + interval;
                    timerBuffer[i] = timer;

                    ResetAccumulator(accumulatorIndex, accumulatorBuffer);

                    if (hasSampleBuffer)
                        AppendSample(sampleBuffer, metric.MetricId, value, normalized, time, subject, societyRoot);

                    if (!randomLookup.HasComponent(subject))
                        continue;

                    if (!TryEvaluateRules(ref libraryBlob, timer.MetricIndex, normalized, time, subject, societyRoot, hasProfile, profileBlob))
                        continue;
                }
            }
        }
        #endregion
    }
}
