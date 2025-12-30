using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_MetricSample
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

        private static float GetInterval(float interval)
        {
            if (interval <= 0f)
                return 1f;

            return interval;
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
            if (metric.Aggregation == EmergenceMetricAggregation.Last)
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

        private static float Normalize(EmergenceMetricNormalization normalization, float value)
        {
            if (normalization == EmergenceMetricNormalization.Invert01)
                return 1f - math.clamp(value, 0f, 1f);

            if (normalization == EmergenceMetricNormalization.Signed01)
                return math.clamp((value * 0.5f) + 0.5f, 0f, 1f);

            if (normalization == EmergenceMetricNormalization.Abs01)
                return math.clamp(math.abs(value), 0f, 1f);

            return math.clamp(value, 0f, 1f);
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

        private static float SampleCurve(ref EM_Blob_ProbabilityCurve curve, float normalized)
        {
            int count = curve.Samples.Length;

            if (count == 0)
                return 0f;

            if (count == 1)
                return math.clamp(curve.Samples[0], 0f, 1f);

            float t = math.clamp(normalized, 0f, 1f);
            float scaled = t * (count - 1);
            int index = (int)math.floor(scaled);
            int next = math.min(index + 1, count - 1);
            float lerp = scaled - index;
            float value = math.lerp(curve.Samples[index], curve.Samples[next], lerp);

            return math.clamp(value, 0f, 1f);
        }

        private static float NextRandom01(ref EM_Component_RandomSeed seed)
        {
            uint current = seed.Value;

            if (current == 0)
                current = 1u;

            Random random = Random.CreateFromIndex(current);
            float value = random.NextFloat();
            seed.Value = random.NextUInt();

            return value;
        }

        private static bool TryGetProfileReference(Entity subject, Entity societyRoot,
            ComponentLookup<EM_Component_SocietyProfileReference> profileLookup,
            out BlobAssetReference<EM_Blob_SocietyProfile> profileReference)
        {
            profileReference = default;

            if (societyRoot != Entity.Null && profileLookup.HasComponent(societyRoot))
            {
                profileReference = profileLookup[societyRoot].Value;
                return profileReference.IsCreated;
            }

            if (subject == Entity.Null || !profileLookup.HasComponent(subject))
                return false;

            profileReference = profileLookup[subject].Value;
            return profileReference.IsCreated;
        }
        #endregion
    }
}
