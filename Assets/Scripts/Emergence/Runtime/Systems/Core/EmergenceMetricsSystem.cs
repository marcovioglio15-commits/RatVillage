using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Samples emergence metrics for diagnostics and tuning.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceRuleEvaluateSystem))]
    public partial struct EmergenceMetricsSystem : ISystem
    {
        #region State
        private ComponentLookup<EmergenceCohesion> cohesionLookup;
        private ComponentLookup<EmergencePopulation> populationLookup;
        private BufferLookup<EmergenceNeed> needLookup;
        private BufferLookup<EmergenceResource> resourceLookup;
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceLibraryReference>();
            cohesionLookup = state.GetComponentLookup<EmergenceCohesion>(true);
            populationLookup = state.GetComponentLookup<EmergencePopulation>(true);
            needLookup = state.GetBufferLookup<EmergenceNeed>(true);
            resourceLookup = state.GetBufferLookup<EmergenceResource>(true);
        }

        /// <summary>
        /// Samples metrics and appends values to the metric buffer.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            EmergenceLibraryReference libraryReference = SystemAPI.GetSingleton<EmergenceLibraryReference>();

            if (!libraryReference.Value.IsCreated)
                return;

            ref BlobArray<EmergenceMetricBlob> metrics = ref libraryReference.Value.Value.Metrics;

            if (metrics.Length == 0)
                return;

            Entity libraryEntity = SystemAPI.GetSingletonEntity<EmergenceLibraryReference>();
            DynamicBuffer<EmergenceMetricSample> sampleBuffer = SystemAPI.GetSingletonBuffer<EmergenceMetricSample>();
            DynamicBuffer<EmergenceSignalEvent> signalQueue = state.EntityManager.GetBuffer<EmergenceSignalEvent>(libraryEntity);

            double time = SystemAPI.Time.ElapsedTime;
            int queueLength = signalQueue.Length;

            cohesionLookup.Update(ref state);
            populationLookup.Update(ref state);
            needLookup.Update(ref state);
            resourceLookup.Update(ref state);

            foreach ((RefRO<EmergenceSocietyProfileReference> profileRef, DynamicBuffer<EmergenceMetricTimer> timerBuffer, Entity entity)
                in SystemAPI.Query<RefRO<EmergenceSocietyProfileReference>, DynamicBuffer<EmergenceMetricTimer>>().WithAll<EmergenceSocietyRoot>().WithEntityAccess())
            {
                BlobAssetReference<EmergenceSocietyProfileBlob> profileBlob = profileRef.ValueRO.Value;
                DynamicBuffer<EmergenceMetricTimer> timers = timerBuffer;

                EnsureMetricTimers(metrics.Length, ref timers);

                if (profileBlob.IsCreated)
                {
                    ref BlobArray<byte> metricMask = ref profileBlob.Value.MetricMask;

                    for (int i = 0; i < metrics.Length; i++)
                    {
                        if (i < metricMask.Length && metricMask[i] == 0)
                            continue;

                        EmergenceMetricTimer timer = timers[i];

                        if (time < timer.NextSampleTime)
                            continue;

                        EmergenceMetricBlob metric = metrics[i];
                        float value = SampleMetric(metric, entity, queueLength, ref cohesionLookup, ref populationLookup, ref needLookup, ref resourceLookup);
                        timer.NextSampleTime = time + GetInterval(metric.SampleInterval);
                        timers[i] = timer;

                        EmergenceMetricSample sample = new EmergenceMetricSample
                        {
                            MetricId = metric.MetricId,
                            Value = value,
                            Time = time,
                            Society = entity
                        };

                        sampleBuffer.Add(sample);
                    }

                    continue;
                }

                for (int i = 0; i < metrics.Length; i++)
                {
                    EmergenceMetricTimer timer = timers[i];

                    if (time < timer.NextSampleTime)
                        continue;

                    EmergenceMetricBlob metric = metrics[i];
                    float value = SampleMetric(metric, entity, queueLength, ref cohesionLookup, ref populationLookup, ref needLookup, ref resourceLookup);
                    timer.NextSampleTime = time + GetInterval(metric.SampleInterval);
                    timers[i] = timer;

                    EmergenceMetricSample sample = new EmergenceMetricSample
                    {
                        MetricId = metric.MetricId,
                        Value = value,
                        Time = time,
                        Society = entity
                    };

                    sampleBuffer.Add(sample);
                }
            }
        }
        #endregion

        #region Helpers
        private static void EnsureMetricTimers(int metricCount, ref DynamicBuffer<EmergenceMetricTimer> timerBuffer)
        {
            if (timerBuffer.Length == metricCount)
                return;

            timerBuffer.Clear();

            for (int i = 0; i < metricCount; i++)
            {
                EmergenceMetricTimer timer = new EmergenceMetricTimer
                {
                    MetricIndex = i,
                    NextSampleTime = 0d
                };

                timerBuffer.Add(timer);
            }
        }

        private static float GetInterval(float sampleInterval)
        {
            if (sampleInterval <= 0f)
                return 1f;

            return sampleInterval;
        }

        private static float SampleMetric(EmergenceMetricBlob metric, Entity society, int queueLength,
            ref ComponentLookup<EmergenceCohesion> cohesionLookup, ref ComponentLookup<EmergencePopulation> populationLookup,
            ref BufferLookup<EmergenceNeed> needLookup, ref BufferLookup<EmergenceResource> resourceLookup)
        {
            if (metric.MetricType == EmergenceMetricType.PopulationCount)
            {
                if (populationLookup.HasComponent(society))
                    return populationLookup[society].Value;

                return 0f;
            }

            if (metric.MetricType == EmergenceMetricType.AverageNeed)
            {
                if (!needLookup.HasBuffer(society))
                    return 0f;

                DynamicBuffer<EmergenceNeed> needs = needLookup[society];

                if (needs.Length == 0)
                    return 0f;

                float sum = 0f;

                for (int i = 0; i < needs.Length; i++)
                {
                    sum += needs[i].Value;
                }

                return sum / needs.Length;
            }

            if (metric.MetricType == EmergenceMetricType.ResourceTotal)
            {
                if (!resourceLookup.HasBuffer(society))
                    return 0f;

                DynamicBuffer<EmergenceResource> resources = resourceLookup[society];
                float sum = 0f;

                for (int i = 0; i < resources.Length; i++)
                {
                    sum += resources[i].Amount;
                }

                return sum;
            }

            if (metric.MetricType == EmergenceMetricType.SignalRate)
                return queueLength;

            if (metric.MetricType == EmergenceMetricType.SocialCohesion)
            {
                if (cohesionLookup.HasComponent(society))
                    return cohesionLookup[society].Value;

                return 0f;
            }

            return 0f;
        }
        #endregion
    }
}
