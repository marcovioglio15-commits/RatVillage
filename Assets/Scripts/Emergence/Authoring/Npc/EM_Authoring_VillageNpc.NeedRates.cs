using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region NeedRates
        // Need rate curve sampling for decay rules.
        #region RateSampling
        // FixedList128Bytes<float> capacity is 31 samples.
        private const int NeedRateCurveSamples = 31;

        private static FixedList128Bytes<float> BuildNeedRateSamples(AnimationCurve curve)
        {
            FixedList128Bytes<float> samples = new FixedList128Bytes<float>();
            int sampleCount = NeedRateCurveSamples;

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                float t = sampleCount > 1 ? (float)sampleIndex / (sampleCount - 1) : 0f;
                float value = EvaluateNeedRateCurve(curve, t);
                samples.Add(value);
            }

            return samples;
        }

        private static float EvaluateNeedRateCurve(AnimationCurve curve, float t)
        {
            if (curve == null || curve.length == 0)
                return 0f;

            return curve.Evaluate(t);
        }
        #endregion

        // Resolve default and activity-specific rate curves.
        #region ActivityRates
        private static FixedList128Bytes<float> ResolveDefaultRateSamples(NeedActivityRateEntry[] entries)
        {
            FixedList128Bytes<float> samples = default;

            if (entries == null)
                return samples;

            for (int i = 0; i < entries.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(entries[i].ActivityId))
                    continue;

                samples = BuildNeedRateSamples(entries[i].RatePerHour);
                return samples;
            }

            return samples;
        }

        private static void AddNeedActivityRates(FixedString64Bytes needId, NeedActivityRateEntry[] entries,
            ref DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates)
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(entries[i].ActivityId))
                    continue;

                FixedString64Bytes activityId = new FixedString64Bytes(entries[i].ActivityId);
                FixedList128Bytes<float> samples = BuildNeedRateSamples(entries[i].RatePerHour);
                int existingIndex = FindNeedActivityRateIndex(needId, activityId, activityRates);

                if (existingIndex >= 0)
                {
                    EM_BufferElement_NeedActivityRate existing = activityRates[existingIndex];
                    existing.RatePerHourSamples = samples;
                    activityRates[existingIndex] = existing;
                    continue;
                }

                EM_BufferElement_NeedActivityRate entry = new EM_BufferElement_NeedActivityRate
                {
                    NeedId = needId,
                    ActivityId = activityId,
                    RatePerHourSamples = samples
                };

                activityRates.Add(entry);
            }
        }

        private static int FindNeedActivityRateIndex(FixedString64Bytes needId, FixedString64Bytes activityId,
            DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates)
        {
            if (needId.Length == 0 || activityId.Length == 0)
                return -1;

            for (int i = 0; i < activityRates.Length; i++)
            {
                if (!activityRates[i].NeedId.Equals(needId))
                    continue;

                if (!activityRates[i].ActivityId.Equals(activityId))
                    continue;

                return i;
            }

            return -1;
        }
        #endregion
        #endregion
    }
}
