using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergentMechanics
{
    /// <summary>
    /// Helper methods for emergence library blob building.
    /// </summary>
    internal static partial class EM_BlobBuilder_Library
    {
        #region Nested Types
        private struct RuleBuildRecord
        {
            public int MetricIndex;
            public int EffectIndex;
            public int RuleSetIndex;
            public int CurveIndex;
            public float Weight;
            public float CooldownSeconds;
        }

        private struct MetricGroupRecord
        {
            public int SignalIndex;
            public int MetricIndex;
        }
        #endregion

        #region Helpers
        #region Getters
        private static string GetSignalId(EM_SignalDefinition signal)
        {
            if (signal == null)
                return string.Empty;

            return signal.SignalId;
        }

        private static string GetEffectId(EM_EffectDefinition effect)
        {
            if (effect == null)
                return string.Empty;

            return effect.EffectId;
        }

        private static string GetMetricId(EM_MetricDefinition metric)
        {
            if (metric == null)
                return string.Empty;

            return metric.MetricId;
        }

        private static string GetRuleSetId(EM_RuleSetDefinition ruleSet)
        {
            if (ruleSet == null)
                return string.Empty;

            return ruleSet.RuleSetId;
        }

        private static string GetDomainId(EM_DomainDefinition domain)
        {
            if (domain == null)
                return string.Empty;

            return domain.DomainId;
        }
        #endregion

        #region Builders
        private static List<T> BuildUniqueList<T>(T[] source, Func<T, string> idSelector) where T : UnityEngine.Object
        {
            List<T> result = new List<T>();

            if (source == null)
                return result;

            HashSet<string> usedIds = new HashSet<string>();

            for (int i = 0; i < source.Length; i++)
            {
                T item = source[i];

                if (item == null)
                    continue;

                string id = idSelector(item);

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (usedIds.Contains(id))
                    continue;

                usedIds.Add(id);
                result.Add(item);
            }

            return result;
        }

        private static Dictionary<T, int> BuildIndexDictionary<T>(List<T> source) where T : UnityEngine.Object
        {
            Dictionary<T, int> map = new Dictionary<T, int>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                map[source[i]] = i;
            }

            return map;
        }

        private static int[] BuildMetricGroupIndices(MetricGroupRecord[] records)
        {
            int[] indices = new int[records.Length];

            for (int i = 0; i < records.Length; i++)
            {
                indices[i] = records[i].MetricIndex;
            }

            return indices;
        }

        private static int AddCurveSamples(AnimationCurve curve, int sampleCount, List<float[]> curveSamples)
        {
            float[] samples = new float[sampleCount];
            float step = sampleCount > 1 ? 1f / (sampleCount - 1) : 1f;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = step * i;
                float value = curve != null ? curve.Evaluate(time) : 0f;
                samples[i] = Mathf.Clamp01(value);
            }

            int index = curveSamples.Count;
            curveSamples.Add(samples);
            return index;
        }
        #endregion
        #endregion

        #region Comparers
        private sealed class RuleBuildRecordComparer : IComparer<RuleBuildRecord>
        {
            public int Compare(RuleBuildRecord left, RuleBuildRecord right)
            {
                int metricComparison = left.MetricIndex.CompareTo(right.MetricIndex);

                if (metricComparison != 0)
                    return metricComparison;

                return left.EffectIndex.CompareTo(right.EffectIndex);
            }
        }

        private sealed class MetricGroupRecordComparer : IComparer<MetricGroupRecord>
        {
            public int Compare(MetricGroupRecord left, MetricGroupRecord right)
            {
                int signalComparison = left.SignalIndex.CompareTo(right.SignalIndex);

                if (signalComparison != 0)
                    return signalComparison;

                return left.MetricIndex.CompareTo(right.MetricIndex);
            }
        }
        #endregion
    }
}
