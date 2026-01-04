using System.Collections.Generic;
using Unity.Collections;

namespace EmergentMechanics
{
    internal static partial class EM_BlobBuilder_Library
    {
        #region Rule Builders
        private static RuleBuildRecord[] BuildRuleRecords(List<EM_RuleSetDefinition> ruleSets,
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices,
            Dictionary<EM_MetricDefinition, int> metricIndices,
            Dictionary<EM_EffectDefinition, int> effectIndices,
            List<RuleEffectRecord> effectRecords,
            List<float[]> curveSamples,
            int curveSampleCount)
        {
            List<RuleBuildRecord> records = new List<RuleBuildRecord>();

            for (int i = 0; i < ruleSets.Count; i++)
            {
                EM_RuleSetDefinition ruleSet = ruleSets[i];
                EM_RuleSetDefinition.RuleEntry[] rules = ruleSet.Rules;

                if (rules == null)
                    continue;

                int ruleSetIndex = ruleSetIndices[ruleSet];

                for (int j = 0; j < rules.Length; j++)
                {
                    EM_MetricDefinition metric = rules[j].Metric;

                    if (metric == null)
                        continue;

                    int metricIndex;
                    bool metricFound = metricIndices.TryGetValue(metric, out metricIndex);

                    if (!metricFound)
                        continue;

                    EM_RuleSetDefinition.RuleEffectEntry[] effects = rules[j].Effects;

                    if (effects == null || effects.Length == 0)
                        continue;

                    int effectStartIndex = effectRecords.Count;
                    int effectCount = 0;

                    for (int e = 0; e < effects.Length; e++)
                    {
                        EM_EffectDefinition effect = effects[e].Effect;

                        if (effect == null)
                            continue;

                        int effectIndex;
                        bool effectFound = effectIndices.TryGetValue(effect, out effectIndex);

                        if (!effectFound)
                            continue;

                        RuleEffectRecord effectRecord = new RuleEffectRecord
                        {
                            EffectIndex = effectIndex,
                            Weight = effects[e].Weight
                        };

                        effectRecords.Add(effectRecord);
                        effectCount++;
                    }

                    if (effectCount <= 0)
                        continue;

                    int curveIndex = AddCurveSamples(rules[j].ProbabilityCurve, curveSampleCount, curveSamples);

                    FixedString64Bytes contextId = default;

                    if (!string.IsNullOrWhiteSpace(rules[j].ContextIdFilter))
                        contextId = new FixedString64Bytes(rules[j].ContextIdFilter);

                    RuleBuildRecord record = new RuleBuildRecord
                    {
                        MetricIndex = metricIndex,
                        RuleSetIndex = ruleSetIndex,
                        ContextId = contextId,
                        CurveIndex = curveIndex,
                        Weight = rules[j].Weight,
                        CooldownSeconds = rules[j].CooldownSeconds,
                        EffectStartIndex = effectStartIndex,
                        EffectLength = effectCount
                    };

                    records.Add(record);
                }
            }

            RuleBuildRecord[] result = records.ToArray();
            System.Array.Sort(result, new RuleBuildRecordComparer());

            return result;
        }

        private static EM_Blob_RuleGroup[] BuildRuleGroups(RuleBuildRecord[] rules)
        {
            List<EM_Blob_RuleGroup> groups = new List<EM_Blob_RuleGroup>();
            int startIndex = 0;
            int index = 0;

            while (index < rules.Length)
            {
                int metricIndex = rules[index].MetricIndex;
                int length = 0;

                while (index + length < rules.Length && rules[index + length].MetricIndex == metricIndex)
                {
                    length++;
                }

                EM_Blob_RuleGroup group = new EM_Blob_RuleGroup
                {
                    MetricIndex = metricIndex,
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
