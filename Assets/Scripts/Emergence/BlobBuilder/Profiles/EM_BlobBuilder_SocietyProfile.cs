using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    internal static class EM_BlobBuilder_SocietyProfile
    {
        #region Builder
        public static BlobAssetReference<EM_Blob_SocietyProfile> BuildProfileBlob(EM_SocietyProfile profile, EM_MechanicLibrary library)
        {
            if (profile == null || library == null)
                return default;

            List<EM_SignalDefinition> signals = BuildUniqueList(library.Signals, GetSignalId);
            Dictionary<EM_SignalDefinition, int> signalIndices = BuildIndexDictionary(signals);
            List<EM_MetricDefinition> metrics = BuildUniqueList(library.Metrics, GetMetricId);
            List<EM_MetricDefinition> validMetrics = FilterMetrics(metrics, signalIndices);
            List<EM_RuleSetDefinition> ruleSets = BuildUniqueList(library.RuleSets, GetRuleSetId);
            List<EM_DomainDefinition> domains = BuildUniqueList(library.Domains, GetDomainId);

            Dictionary<EM_DomainDefinition, int> domainIndices = BuildIndexDictionary(domains);
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices = BuildIndexDictionary(ruleSets);
            Dictionary<EM_MetricDefinition, int> metricIndices = BuildIndexDictionary(validMetrics);

            byte[] domainMask = BuildMask(profile.Domains, domainIndices, domains.Count);
            byte[] ruleSetMask = BuildRuleSetMask(profile.Domains, ruleSetIndices, ruleSets.Count);
            byte[] metricMask = BuildMetricMask(profile.Domains, ruleSetIndices, metricIndices, validMetrics.Count);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EM_Blob_SocietyProfile root = ref builder.ConstructRoot<EM_Blob_SocietyProfile>();

            root.ProfileId = new FixedString64Bytes(profile.ProfileId);
            
            BlobBuilderArray<byte> domainMaskArray = builder.Allocate(ref root.DomainMask, domainMask.Length);
            BlobBuilderArray<byte> ruleMaskArray = builder.Allocate(ref root.RuleSetMask, ruleSetMask.Length);
            BlobBuilderArray<byte> metricMaskArray = builder.Allocate(ref root.MetricMask, metricMask.Length);

            for (int i = 0; i < domainMask.Length; i++)
            {
                domainMaskArray[i] = domainMask[i];
            }

            for (int i = 0; i < ruleSetMask.Length; i++)
            {
                ruleMaskArray[i] = ruleSetMask[i];
            }

            for (int i = 0; i < metricMask.Length; i++)
            {
                metricMaskArray[i] = metricMask[i];
            }

            BlobAssetReference<EM_Blob_SocietyProfile> blobAsset = builder.CreateBlobAssetReference<EM_Blob_SocietyProfile>(Allocator.Persistent);
            builder.Dispose();

            return blobAsset;
        }
        #endregion

        #region Helpers
        private static List<T> BuildUniqueList<T>(T[] source, System.Func<T, string> idSelector) where T : UnityEngine.Object
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
            Dictionary<T, int> map = new Dictionary<T, int>();

            for (int i = 0; i < source.Count; i++)
            {
                T item = source[i];

                map[item] = i;
            }

            return map;
        }

        private static List<EM_MetricDefinition> FilterMetrics(List<EM_MetricDefinition> metrics,
            Dictionary<EM_SignalDefinition, int> signalIndices)
        {
            List<EM_MetricDefinition> result = new List<EM_MetricDefinition>();

            for (int i = 0; i < metrics.Count; i++)
            {
                EM_MetricDefinition metric = metrics[i];

                if (metric.Signal == null)
                    continue;

                if (!signalIndices.ContainsKey(metric.Signal))
                    continue;

                result.Add(metric);
            }

            return result;
        }

        private static string GetSignalId(EM_SignalDefinition signal)
        {
            if (signal == null)
                return string.Empty;

            return signal.SignalId;
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

        private static byte[] BuildMask<T>(T[] activeItems, Dictionary<T, int> indexMap, int totalCount)
            where T : UnityEngine.Object
        {
            byte[] mask = new byte[totalCount];

            if (activeItems == null)
                return mask;

            for (int i = 0; i < activeItems.Length; i++)
            {
                T item = activeItems[i];

                if (item == null)
                    continue;

                int index;
                bool found = indexMap.TryGetValue(item, out index);

                if (!found)
                    continue;

                mask[index] = 1;
            }

            return mask;
        }

        private static byte[] BuildRuleSetMask(EM_DomainDefinition[] activeDomains,
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices, int totalCount)
        {
            byte[] mask = new byte[totalCount];

            if (activeDomains == null)
                return mask;

            for (int i = 0; i < activeDomains.Length; i++)
            {
                EM_DomainDefinition domain = activeDomains[i];

                if (domain == null)
                    continue;

                EM_RuleSetDefinition[] ruleSets = domain.RuleSets;

                if (ruleSets == null)
                    continue;

                for (int j = 0; j < ruleSets.Length; j++)
                {
                    EM_RuleSetDefinition ruleSet = ruleSets[j];

                    if (ruleSet == null)
                        continue;

                    int index;
                    bool found = ruleSetIndices.TryGetValue(ruleSet, out index);

                    if (!found)
                        continue;

                    mask[index] = 1;
                }
            }

            return mask;
        }

        private static byte[] BuildMetricMask(EM_DomainDefinition[] activeDomains,
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices,
            Dictionary<EM_MetricDefinition, int> metricIndices,
            int totalCount)
        {
            byte[] mask = new byte[totalCount];

            if (activeDomains == null)
                return mask;

            for (int i = 0; i < activeDomains.Length; i++)
            {
                EM_DomainDefinition domain = activeDomains[i];

                if (domain == null)
                    continue;

                EM_RuleSetDefinition[] ruleSets = domain.RuleSets;

                if (ruleSets == null)
                    continue;

                for (int j = 0; j < ruleSets.Length; j++)
                {
                    EM_RuleSetDefinition ruleSet = ruleSets[j];

                    if (ruleSet == null)
                        continue;

                    bool found = ruleSetIndices.TryGetValue(ruleSet, out int _);

                    if (!found)
                        continue;

                    EM_RuleSetDefinition.MetricRuleEntry[] rules = ruleSet.Rules;

                    if (rules == null)
                        continue;

                    for (int r = 0; r < rules.Length; r++)
                    {
                        EM_MetricDefinition metric = rules[r].Metric;

                        if (metric == null)
                            continue;

                        int index;
                        bool metricFound = metricIndices.TryGetValue(metric, out index);

                        if (!metricFound)
                            continue;

                        mask[index] = 1;
                    }
                }
            }

            return mask;
        }
        #endregion
    }
}
