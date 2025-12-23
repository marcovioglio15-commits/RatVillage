using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Emergence
{
    /// <summary>
    /// Helper methods for emergence library blob building.
    /// </summary>
    internal static partial class EmergenceLibraryBlobBuilder
    {
        #region Helpers
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

        private static Dictionary<T, int> BuildIndexMap<T>(List<T> source) where T : UnityEngine.Object
        {
            Dictionary<T, int> map = new Dictionary<T, int>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                map[source[i]] = i;
            }

            return map;
        }

        private static RuleBuildRecord[] BuildRuleRecords(List<EM_RuleSetDefinition> ruleSets, Dictionary<EM_RuleSetDefinition, int> ruleSetIndices,
            Dictionary<EM_SignalDefinition, int> signalIndices, Dictionary<EM_EffectDefinition, int> effectIndices)
        {
            List<RuleBuildRecord> records = new List<RuleBuildRecord>();

            for (int i = 0; i < ruleSets.Count; i++)
            {
                EM_RuleSetDefinition ruleSet = ruleSets[i];
                EM_RuleSetDefinition.SignalRuleEntry[] rules = ruleSet.Rules;

                if (rules == null)
                    continue;

                int ruleSetIndex = ruleSetIndices[ruleSet];

                for (int j = 0; j < rules.Length; j++)
                {
                    EM_SignalDefinition signal = rules[j].Signal;
                    EM_EffectDefinition effect = rules[j].Effect;

                    if (signal == null || effect == null)
                        continue;

                    int signalIndex;
                    bool signalFound = signalIndices.TryGetValue(signal, out signalIndex);

                    if (!signalFound)
                        continue;

                    int effectIndex;
                    bool effectFound = effectIndices.TryGetValue(effect, out effectIndex);

                    if (!effectFound)
                        continue;

                    RuleBuildRecord record = new RuleBuildRecord
                    {
                        SignalId = new FixedString64Bytes(signal.SignalId),
                        SignalIndex = signalIndex,
                        EffectIndex = effectIndex,
                        RuleSetIndex = ruleSetIndex,
                        Priority = rules[j].Priority,
                        Weight = rules[j].Weight,
                        MinimumSignalValue = rules[j].MinimumSignalValue,
                        CooldownSeconds = rules[j].CooldownSeconds
                    };

                    records.Add(record);
                }
            }

            RuleBuildRecord[] result = records.ToArray();
            Array.Sort(result, new RuleBuildRecordComparer());

            return result;
        }

        private static EmergenceRuleGroupBlob[] BuildRuleGroups(RuleBuildRecord[] rules)
        {
            List<EmergenceRuleGroupBlob> groups = new List<EmergenceRuleGroupBlob>();
            int startIndex = 0;
            int index = 0;

            while (index < rules.Length)
            {
                FixedString64Bytes signalId = rules[index].SignalId;
                int length = 0;

                while (index + length < rules.Length && rules[index + length].SignalId.Equals(signalId))
                {
                    length++;
                }

                EmergenceRuleGroupBlob group = new EmergenceRuleGroupBlob
                {
                    SignalId = signalId,
                    StartIndex = startIndex,
                    Length = length
                };

                groups.Add(group);
                startIndex += length;
                index += length;
            }

            return groups.ToArray();
        }
        #endregion
    }
}
