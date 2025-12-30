using System.Collections.Generic;
using Unity.Collections;

namespace EmergentMechanics
{
    internal static partial class EM_BlobBuilder_Library
    {
        #region Metric and Domain Builders
        private static MetricGroupRecord[] BuildMetricGroupRecords(List<EM_MetricDefinition> metrics,
            Dictionary<EM_SignalDefinition, int> signalIndices)
        {
            List<MetricGroupRecord> records = new List<MetricGroupRecord>();

            for (int i = 0; i < metrics.Count; i++)
            {
                EM_MetricDefinition metric = metrics[i];

                if (metric.Signal == null)
                    continue;

                int signalIndex;
                bool signalFound = signalIndices.TryGetValue(metric.Signal, out signalIndex);

                if (!signalFound)
                    continue;

                MetricGroupRecord record = new MetricGroupRecord
                {
                    SignalIndex = signalIndex,
                    MetricIndex = i
                };

                records.Add(record);
            }

            MetricGroupRecord[] result = records.ToArray();
            System.Array.Sort(result, new MetricGroupRecordComparer());

            return result;
        }

        private static EM_Blob_MetricGroup[] BuildMetricGroups(MetricGroupRecord[] records)
        {
            List<EM_Blob_MetricGroup> groups = new List<EM_Blob_MetricGroup>();
            int startIndex = 0;
            int index = 0;

            while (index < records.Length)
            {
                int signalIndex = records[index].SignalIndex;
                int length = 0;

                while (index + length < records.Length && records[index + length].SignalIndex == signalIndex)
                {
                    length++;
                }

                EM_Blob_MetricGroup group = new EM_Blob_MetricGroup
                {
                    SignalIndex = signalIndex,
                    StartIndex = startIndex,
                    Length = length
                };

                groups.Add(group);
                startIndex += length;
                index += length;
            }

            return groups.ToArray();
        }

        private static int[] BuildDomainRuleSetIndices(List<EM_DomainDefinition> domains,
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices, out EM_Blob_Domain[] domainGroups)
        {
            List<int> indices = new List<int>();
            domainGroups = new EM_Blob_Domain[domains.Count];

            for (int i = 0; i < domains.Count; i++)
            {
                EM_DomainDefinition domain = domains[i];
                EM_RuleSetDefinition[] ruleSets = domain.RuleSets;
                HashSet<int> used = new HashSet<int>();
                int startIndex = indices.Count;

                if (ruleSets != null)
                {
                    for (int j = 0; j < ruleSets.Length; j++)
                    {
                        EM_RuleSetDefinition ruleSet = ruleSets[j];

                        if (ruleSet == null)
                            continue;

                        int ruleSetIndex;
                        bool found = ruleSetIndices.TryGetValue(ruleSet, out ruleSetIndex);

                        if (!found)
                            continue;

                        if (used.Contains(ruleSetIndex))
                            continue;

                        used.Add(ruleSetIndex);
                        indices.Add(ruleSetIndex);
                    }
                }

                EM_Blob_Domain group = new EM_Blob_Domain
                {
                    DomainId = new FixedString64Bytes(domain.DomainId),
                    StartIndex = startIndex,
                    Length = indices.Count - startIndex
                };

                domainGroups[i] = group;
            }

            return indices.ToArray();
        }
        #endregion
    }
}
