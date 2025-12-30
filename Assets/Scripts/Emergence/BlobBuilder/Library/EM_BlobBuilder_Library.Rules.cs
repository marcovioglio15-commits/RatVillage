using System.Collections.Generic;

namespace EmergentMechanics
{
    internal static partial class EM_BlobBuilder_Library
    {
        #region Rule Builders
        private static RuleBuildRecord[] BuildRuleRecords(List<EM_RuleSetDefinition> ruleSets,
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices,
            Dictionary<EM_MetricDefinition, int> metricIndices,
            Dictionary<EM_EffectDefinition, int> effectIndices,
            List<float[]> curveSamples,
            int curveSampleCount)
        {
            List<RuleBuildRecord> records = new List<RuleBuildRecord>();

            for (int i = 0; i < ruleSets.Count; i++)
            {
                EM_RuleSetDefinition ruleSet = ruleSets[i];
                EM_RuleSetDefinition.MetricRuleEntry[] rules = ruleSet.Rules;

                if (rules == null)
                    continue;

                int ruleSetIndex = ruleSetIndices[ruleSet];

                for (int j = 0; j < rules.Length; j++)
                {
                    EM_MetricDefinition metric = rules[j].Metric;
                    EM_EffectDefinition effect = rules[j].Effect;

                    if (metric == null || effect == null)
                        continue;

                    int metricIndex;
                    bool metricFound = metricIndices.TryGetValue(metric, out metricIndex);

                    if (!metricFound)
                        continue;

                    int effectIndex;
                    bool effectFound = effectIndices.TryGetValue(effect, out effectIndex);

                    if (!effectFound)
                        continue;

                    int curveIndex = AddCurveSamples(rules[j].ProbabilityCurve, curveSampleCount, curveSamples);

                    RuleBuildRecord record = new RuleBuildRecord
                    {
                        MetricIndex = metricIndex,
                        EffectIndex = effectIndex,
                        RuleSetIndex = ruleSetIndex,
                        CurveIndex = curveIndex,
                        Weight = rules[j].Weight,
                        CooldownSeconds = rules[j].CooldownSeconds
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
