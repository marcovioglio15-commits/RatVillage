using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EM_System_MetricCollect))]
    public partial struct EM_System_MetricInitialization : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyProfileReference> profileLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LibraryReference>();
            profileLookup = state.GetComponentLookup<EM_Component_SocietyProfileReference>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            EM_Component_LibraryReference libraryReference = SystemAPI.GetSingleton<EM_Component_LibraryReference>();

            if (!libraryReference.Value.IsCreated)
                return;

            ref EM_Blob_Library libraryBlob = ref libraryReference.Value.Value;
            ref BlobArray<EM_Blob_Metric> metrics = ref libraryBlob.Metrics;

            if (metrics.Length == 0)
                return;

            profileLookup.Update(ref state);
            memberLookup.Update(ref state);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // Initialize society-scope metric buffers.
            foreach ((DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators,
                DynamicBuffer<EM_BufferElement_MetricTimer> timers,
                RefRO<EM_Component_SocietyProfileReference> profileRef,
                Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_MetricAccumulator>, DynamicBuffer<EM_BufferElement_MetricTimer>,
                    RefRO<EM_Component_SocietyProfileReference>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithNone<EM_Component_MetricInitialized>()
                    .WithEntityAccess())
            {
                BlobAssetReference<EM_Blob_SocietyProfile> profileBlob = profileRef.ValueRO.Value;

                if (!profileBlob.IsCreated)
                    continue;

                DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulatorBuffer = accumulators;
                DynamicBuffer<EM_BufferElement_MetricTimer> timerBuffer = timers;

                InitializeMetrics(ref metrics, profileBlob, EmergenceMetricScope.Society, accumulatorBuffer, timerBuffer);
                ecb.AddComponent<EM_Component_MetricInitialized>(entity);
            }

            // Initialize member-scope metric buffers.
            foreach ((DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators,
                DynamicBuffer<EM_BufferElement_MetricTimer> timers,
                EM_Component_SocietyMember member,
                Entity entity)
                in SystemAPI.Query<DynamicBuffer<EM_BufferElement_MetricAccumulator>, DynamicBuffer<EM_BufferElement_MetricTimer>,
                    EM_Component_SocietyMember>()
                    .WithNone<EM_Component_MetricInitialized>()
                    .WithEntityAccess())
            {
                Entity societyRoot = member.SocietyRoot;

                if (societyRoot == Entity.Null)
                    continue;

                if (!profileLookup.HasComponent(societyRoot))
                    continue;

                BlobAssetReference<EM_Blob_SocietyProfile> profileBlob = profileLookup[societyRoot].Value;

                if (!profileBlob.IsCreated)
                    continue;

                DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulatorBuffer = accumulators;
                DynamicBuffer<EM_BufferElement_MetricTimer> timerBuffer = timers;

                InitializeMetrics(ref metrics, profileBlob, EmergenceMetricScope.Member, accumulatorBuffer, timerBuffer);
                ecb.AddComponent<EM_Component_MetricInitialized>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        #endregion

        #region Helpers
        private static void InitializeMetrics(ref BlobArray<EM_Blob_Metric> metrics,
            BlobAssetReference<EM_Blob_SocietyProfile> profileBlob, EmergenceMetricScope scope,
            DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators,
            DynamicBuffer<EM_BufferElement_MetricTimer> timers)
        {
            accumulators.Clear();
            timers.Clear();

            ref BlobArray<byte> metricMask = ref profileBlob.Value.MetricMask;

            for (int i = 0; i < metrics.Length; i++)
            {
                if (i < metricMask.Length && metricMask[i] == 0)
                    continue;

                EM_Blob_Metric metric = metrics[i];

                if (metric.Scope != scope)
                    continue;

                accumulators.Add(BuildAccumulator(i));
                timers.Add(BuildTimer(i));
            }
        }

        private static EM_BufferElement_MetricAccumulator BuildAccumulator(int metricIndex)
        {
            return new EM_BufferElement_MetricAccumulator
            {
                MetricIndex = metricIndex,
                Sum = 0f,
                Min = float.MaxValue,
                Max = float.MinValue,
                Last = 0f,
                Count = 0
            };
        }

        private static EM_BufferElement_MetricTimer BuildTimer(int metricIndex)
        {
            return new EM_BufferElement_MetricTimer
            {
                MetricIndex = metricIndex,
                NextSampleTime = 0d
            };
        }
        #endregion
    }
}
