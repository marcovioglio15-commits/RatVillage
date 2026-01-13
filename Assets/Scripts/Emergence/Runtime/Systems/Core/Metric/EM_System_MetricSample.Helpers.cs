using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_MetricSample : ISystem
    {
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


        private static bool TryGetAccumulator(int metricIndex, int fallbackIndex,
            DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators,
            out EM_BufferElement_MetricAccumulator accumulator, out int accumulatorIndex)
        {
            accumulator = default;
            accumulatorIndex = -1;

            if (fallbackIndex >= 0 && fallbackIndex < accumulators.Length)
            {
                EM_BufferElement_MetricAccumulator entry = accumulators[fallbackIndex];

                if (entry.MetricIndex == metricIndex)
                {
                    accumulator = entry;
                    accumulatorIndex = fallbackIndex;
                    return true;
                }
            }

            for (int i = 0; i < accumulators.Length; i++)
            {
                if (accumulators[i].MetricIndex != metricIndex)
                    continue;

                accumulator = accumulators[i];
                accumulatorIndex = i;
                return true;
            }

            return false;
        }

        private static float SampleAccumulator(EM_Blob_Metric metric, EM_BufferElement_MetricAccumulator accumulator, float interval)
        {
            if (metric.Aggregation == EmergenceMetricAggregation.No_Aggregation)
                return accumulator.Count > 0 ? accumulator.Last : 0f;

            if (metric.Aggregation == EmergenceMetricAggregation.Sum)
                return accumulator.Sum;

            if (metric.Aggregation == EmergenceMetricAggregation.Min)
                return accumulator.Count > 0 ? accumulator.Min : 0f;

            if (metric.Aggregation == EmergenceMetricAggregation.Max)
                return accumulator.Count > 0 ? accumulator.Max : 0f;

            if (metric.Aggregation == EmergenceMetricAggregation.Count)
                return accumulator.Count;

            if (metric.Aggregation == EmergenceMetricAggregation.Rate)
                return accumulator.Count > 0 ? accumulator.Count / interval : 0f;

            if (accumulator.Count <= 0)
                return 0f;

            return accumulator.Sum / accumulator.Count;
        }


        private static void AppendSample(DynamicBuffer<EM_BufferElement_MetricSample> buffer,
            FixedString64Bytes metricId, float value, float normalized, double time, Entity subject, Entity societyRoot)
        {
            EM_BufferElement_MetricSample sample = new EM_BufferElement_MetricSample
            {
                MetricId = metricId,
                Value = value,
                NormalizedValue = normalized,
                Time = time,
                Subject = subject,
                SocietyRoot = societyRoot
            };

            buffer.Add(sample);
        }

        private static void ResetAccumulator(int index, DynamicBuffer<EM_BufferElement_MetricAccumulator> accumulators)
        {
            if (index < 0 || index >= accumulators.Length)
                return;

            EM_BufferElement_MetricAccumulator accumulator = accumulators[index];
            accumulator.Sum = 0f;
            accumulator.Min = float.MaxValue;
            accumulator.Max = float.MinValue;
            accumulator.Last = 0f;
            accumulator.Count = 0;
            accumulators[index] = accumulator;
        }


        #endregion
    }
}
