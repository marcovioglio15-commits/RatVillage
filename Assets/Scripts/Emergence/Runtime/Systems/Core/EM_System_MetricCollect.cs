using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_Trade))]
    [UpdateAfter(typeof(EM_System_NpcSchedule))]
    public partial struct EM_System_MetricCollect : ISystem
    {
        #region Fields
        // Runtime lookup tables.
        #region Lookups
        private NativeParallelHashMap<FixedString64Bytes, int> signalLookup;
        private bool signalLookupReady;
        private NativeParallelHashMap<int, int> metricGroupLookup;
        private bool metricGroupLookupReady;
        private BufferLookup<EM_BufferElement_MetricAccumulator> accumulatorLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        private ComponentLookup<EM_Component_SocietyRoot> rootLookup;
        #endregion
        #endregion

        #region Unity Lifecycle
        // Setup caches and component lookups.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LibraryReference>();
            accumulatorLookup = state.GetBufferLookup<EM_BufferElement_MetricAccumulator>(false);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            rootLookup = state.GetComponentLookup<EM_Component_SocietyRoot>(true);
        }

        // Dispose persistent hash maps.
        public void OnDestroy(ref SystemState state)
        {
            if (signalLookup.IsCreated)
                signalLookup.Dispose();

            if (metricGroupLookup.IsCreated)
                metricGroupLookup.Dispose();
        }

        // Collect signal events into metric accumulators.
        public void OnUpdate(ref SystemState state)
        {
            EM_Component_LibraryReference libraryReference = SystemAPI.GetSingleton<EM_Component_LibraryReference>();

            if (!libraryReference.Value.IsCreated)
                return;

            ref EM_Blob_Library libraryBlob = ref libraryReference.Value.Value;
            ref BlobArray<EM_Blob_Metric> metrics = ref libraryBlob.Metrics;
            ref BlobArray<EM_Blob_MetricGroup> metricGroups = ref libraryBlob.MetricGroups;
            ref BlobArray<int> metricGroupIndices = ref libraryBlob.MetricGroupMetricIndices;

            if (metrics.Length == 0 || metricGroups.Length == 0 || metricGroupIndices.Length == 0)
                return;

            EnsureSignalLookup(ref libraryBlob);
            EnsureMetricGroupLookup(ref libraryBlob);

            accumulatorLookup.Update(ref state);
            memberLookup.Update(ref state);
            rootLookup.Update(ref state);

            foreach (DynamicBuffer<EM_BufferElement_SignalEvent> emitterBuffer in SystemAPI.Query<DynamicBuffer<EM_BufferElement_SignalEvent>>()
                .WithAll<EM_Component_SignalEmitter>())
            {
                int eventCount = emitterBuffer.Length;

                if (eventCount == 0)
                    continue;

                for (int i = 0; i < eventCount; i++)
                {
                    EM_BufferElement_SignalEvent signalEvent = emitterBuffer[i];

                    if (signalEvent.SignalId.Length == 0)
                        continue;

                    if (signalEvent.Subject == Entity.Null)
                        continue;

                    int signalIndex;
                    bool signalFound = signalLookup.TryGetValue(signalEvent.SignalId, out signalIndex);

                    if (!signalFound)
                        continue;

                    int groupIndex;
                    bool groupFound = metricGroupLookup.TryGetValue(signalIndex, out groupIndex);

                    if (!groupFound)
                        continue;

                    Entity societyRoot = ResolveSocietyRoot(signalEvent.Subject, signalEvent.SocietyRoot, memberLookup, rootLookup);
                    EM_Blob_MetricGroup group = metricGroups[groupIndex];
                    int groupEnd = group.StartIndex + group.Length;

                    if (group.StartIndex < 0 || groupEnd > metricGroupIndices.Length)
                        continue;

                    for (int m = group.StartIndex; m < groupEnd; m++)
                    {
                        int metricIndex = metricGroupIndices[m];

                        if (metricIndex < 0 || metricIndex >= metrics.Length)
                            continue;

                        EM_Blob_Metric metric = metrics[metricIndex];

                        if (metric.Scope == EmergenceMetricScope.Member)
                            UpdateAccumulator(signalEvent.Subject, metricIndex, signalEvent.Value);
                        else
                            UpdateAccumulator(societyRoot, metricIndex, signalEvent.Value);
                    }
                }

                emitterBuffer.Clear();
            }
        }
        #endregion

        #region Helpers
        // Build signal lookup tables from the library blob.
        private void EnsureSignalLookup(ref EM_Blob_Library libraryBlob)
        {
            if (signalLookupReady)
                return;

            ref BlobArray<EM_Blob_Signal> signals = ref libraryBlob.Signals;
            int count = math.max(signals.Length, 1);

            signalLookup = new NativeParallelHashMap<FixedString64Bytes, int>(count, Allocator.Persistent);

            for (int i = 0; i < signals.Length; i++)
            {
                signalLookup.TryAdd(signals[i].SignalId, i);
            }

            signalLookupReady = true;
        }

        // Build metric group lookup tables from the library blob.
        private void EnsureMetricGroupLookup(ref EM_Blob_Library libraryBlob)
        {
            if (metricGroupLookupReady)
                return;

            ref BlobArray<EM_Blob_MetricGroup> groups = ref libraryBlob.MetricGroups;
            int count = math.max(groups.Length, 1);

            metricGroupLookup = new NativeParallelHashMap<int, int>(count, Allocator.Persistent);

            for (int i = 0; i < groups.Length; i++)
            {
                metricGroupLookup.TryAdd(groups[i].SignalIndex, i);
            }

            metricGroupLookupReady = true;
        }

        // Resolve society root for member or root-scoped metrics.
        private static Entity ResolveSocietyRoot(Entity subject, Entity explicitRoot,
            ComponentLookup<EM_Component_SocietyMember> memberLookup, ComponentLookup<EM_Component_SocietyRoot> rootLookup)
        {
            if (explicitRoot != Entity.Null)
                return explicitRoot;

            if (rootLookup.HasComponent(subject))
                return subject;

            if (!memberLookup.HasComponent(subject))
                return Entity.Null;

            return memberLookup[subject].SocietyRoot;
        }

        // Update the accumulator corresponding to the metric index.
        private void UpdateAccumulator(Entity subject, int metricIndex, float value)
        {
            if (subject == Entity.Null)
                return;

            if (!accumulatorLookup.HasBuffer(subject))
                return;

            DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators = accumulatorLookup[subject];

            for (int i = 0; i < accumulators.Length; i++)
            {
                if (accumulators[i].MetricIndex != metricIndex)
                    continue;

                EM_BufferElement_MetricAccumulator accumulator = accumulators[i];
                accumulator.Count += 1;
                accumulator.Last = value;
                accumulator.Sum += value;
                accumulator.Min = math.min(accumulator.Min, value);
                accumulator.Max = math.max(accumulator.Max, value);
                accumulators[i] = accumulator;
                return;
            }
        }
        #endregion
    }
}
