using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    /// <summary>
    /// Builds the Emergence library blob asset for runtime lookup.
    /// </summary>
    internal static partial class EM_BlobBuilder_Library
    {
        #region Constants
        private const int DefaultCurveSamples = 32;
        #endregion

        #region Static Builder
        public static BlobAssetReference<EM_Blob_Library> BuildLibraryBlob(EM_MechanicLibrary library)
        {
            if (library == null)
                return default;

            // Build unique lists for stable indexing.
            List<EM_SignalDefinition> signals = BuildUniqueList(library.Signals, GetSignalId);
            List<EM_EffectDefinition> effects = BuildUniqueList(library.Effects, GetEffectId);
            List<EM_MetricDefinition> metrics = BuildUniqueList(library.Metrics, GetMetricId);
            List<EM_RuleSetDefinition> ruleSets = BuildUniqueList(library.RuleSets, GetRuleSetId);
            List<EM_DomainDefinition> domains = BuildUniqueList(library.Domains, GetDomainId);

            Dictionary<EM_SignalDefinition, int> signalIndices = BuildIndexDictionary(signals);
            Dictionary<EM_EffectDefinition, int> effectIndices = BuildIndexDictionary(effects);
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices = BuildIndexDictionary(ruleSets);

            List<EM_MetricDefinition> validMetrics = new List<EM_MetricDefinition>();

            for (int i = 0; i < metrics.Count; i++)
            {
                EM_MetricDefinition metric = metrics[i];

                if (metric.Signal == null)
                    continue;

                if (!signalIndices.ContainsKey(metric.Signal))
                    continue;

                validMetrics.Add(metric);
            }

            metrics = validMetrics;

            Dictionary<EM_MetricDefinition, int> metricIndices = BuildIndexDictionary(metrics);
            List<float[]> curveSamples = new List<float[]>();
            List<RuleEffectRecord> effectRecords = new List<RuleEffectRecord>();

            RuleBuildRecord[] rules = BuildRuleRecords(ruleSets, ruleSetIndices, metricIndices, effectIndices, effectRecords, curveSamples, DefaultCurveSamples);
            EM_Blob_RuleGroup[] ruleGroups = BuildRuleGroups(rules);
            MetricGroupRecord[] metricGroupRecords = BuildMetricGroupRecords(metrics, signalIndices);
            EM_Blob_MetricGroup[] metricGroups = BuildMetricGroups(metricGroupRecords);
            int[] metricGroupMetricIndices = BuildMetricGroupIndices(metricGroupRecords);

            EM_Blob_Domain[] domainGroups;
            int[] domainRuleSetIndices = BuildDomainRuleSetIndices(domains, ruleSetIndices, out domainGroups);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EM_Blob_Library root = ref builder.ConstructRoot<EM_Blob_Library>();

            BlobBuilderArray<EM_Blob_Signal> signalArray = builder.Allocate(ref root.Signals, signals.Count);
            BlobBuilderArray<EM_Blob_Metric> metricArray = builder.Allocate(ref root.Metrics, metrics.Count);
            BlobBuilderArray<EM_Blob_Effect> effectArray = builder.Allocate(ref root.Effects, effects.Count);
            BlobBuilderArray<EM_Blob_RuleSet> ruleSetArray = builder.Allocate(ref root.RuleSets, ruleSets.Count);
            BlobBuilderArray<EM_Blob_Rule> ruleArray = builder.Allocate(ref root.Rules, rules.Length);
            BlobBuilderArray<EM_Blob_RuleEffect> ruleEffectArray = builder.Allocate(ref root.RuleEffects, effectRecords.Count);
            BlobBuilderArray<EM_Blob_RuleGroup> groupArray = builder.Allocate(ref root.RuleGroups, ruleGroups.Length);
            BlobBuilderArray<EM_Blob_MetricGroup> metricGroupArray = builder.Allocate(ref root.MetricGroups, metricGroups.Length);
            BlobBuilderArray<int> metricGroupIndexArray = builder.Allocate(ref root.MetricGroupMetricIndices, metricGroupMetricIndices.Length);
            BlobBuilderArray<EM_Blob_ProbabilityCurve> curveArray = builder.Allocate(ref root.Curves, curveSamples.Count);
            BlobBuilderArray<EM_Blob_Domain> domainArray = builder.Allocate(ref root.Domains, domainGroups.Length);
            BlobBuilderArray<int> domainRuleSetArray = builder.Allocate(ref root.DomainRuleSetIndices, domainRuleSetIndices.Length);

            // Signals.
            for (int i = 0; i < signals.Count; i++)
            {
                EM_SignalDefinition signal = signals[i];
                FixedString64Bytes domainId = new FixedString64Bytes(string.Empty);

                signalArray[i] = new EM_Blob_Signal
                {
                    SignalId = new FixedString64Bytes(signal.SignalId),
                    DomainId = domainId
                };
            }

            // Metrics.
            for (int i = 0; i < metrics.Count; i++)
            {
                EM_MetricDefinition metric = metrics[i];
                int signalIndex = signalIndices[metric.Signal];

                metricArray[i] = new EM_Blob_Metric
                {
                    MetricId = new FixedString64Bytes(metric.MetricId),
                    SignalIndex = signalIndex,
                    SampleInterval = metric.SampleIntervalSeconds,
                    SamplingMode = metric.SamplingMode,
                    Scope = metric.Scope,
                    Aggregation = metric.Aggregation,
                    Normalization = metric.Normalization
                };
            }

            // Effects.
            for (int i = 0; i < effects.Count; i++)
            {
                EM_EffectDefinition effect = effects[i];

                effectArray[i] = new EM_Blob_Effect
                {
                    EffectId = new FixedString64Bytes(effect.EffectId),
                    EffectType = effect.EffectType,
                    Target = effect.Target,
                    ParameterId = new FixedString64Bytes(effect.ParameterId),
                    SecondaryId = new FixedString64Bytes(effect.SecondaryId),
                    Magnitude = effect.Magnitude,
                    UseClamp = effect.UseClamp ? (byte)1 : (byte)0,
                    MinValue = effect.MinValue,
                    MaxValue = effect.MaxValue
                };
            }

            // Rule sets.
            for (int i = 0; i < ruleSets.Count; i++)
            {
                EM_RuleSetDefinition ruleSet = ruleSets[i];

                ruleSetArray[i] = new EM_Blob_RuleSet
                {
                    RuleSetId = new FixedString64Bytes(ruleSet.RuleSetId)
                };
            }

            // Rules.
            for (int i = 0; i < rules.Length; i++)
            {
                ruleArray[i] = new EM_Blob_Rule
                {
                    MetricIndex = rules[i].MetricIndex,
                    RuleSetIndex = rules[i].RuleSetIndex,
                    ContextId = rules[i].ContextId,
                    CurveIndex = rules[i].CurveIndex,
                    Weight = rules[i].Weight,
                    CooldownSeconds = rules[i].CooldownSeconds,
                    EffectStartIndex = rules[i].EffectStartIndex,
                    EffectLength = rules[i].EffectLength
                };
            }

            for (int i = 0; i < effectRecords.Count; i++)
            {
                ruleEffectArray[i] = new EM_Blob_RuleEffect
                {
                    EffectIndex = effectRecords[i].EffectIndex,
                    Weight = effectRecords[i].Weight
                };
            }

            for (int i = 0; i < ruleGroups.Length; i++)
            {
                groupArray[i] = ruleGroups[i];
            }

            for (int i = 0; i < metricGroups.Length; i++)
            {
                metricGroupArray[i] = metricGroups[i];
            }

            for (int i = 0; i < metricGroupMetricIndices.Length; i++)
            {
                metricGroupIndexArray[i] = metricGroupMetricIndices[i];
            }

            // Curves.
            for (int i = 0; i < curveSamples.Count; i++)
            {
                float[] samples = curveSamples[i];
                ref EM_Blob_ProbabilityCurve curve = ref curveArray[i];
                BlobBuilderArray<float> sampleArray = builder.Allocate(ref curve.Samples, samples.Length);

                for (int s = 0; s < samples.Length; s++)
                {
                    sampleArray[s] = samples[s];
                }
            }

            // Domains and domain rule set indices.
            for (int i = 0; i < domainGroups.Length; i++)
            {
                domainArray[i] = domainGroups[i];
            }

            for (int i = 0; i < domainRuleSetIndices.Length; i++)
            {
                domainRuleSetArray[i] = domainRuleSetIndices[i];
            }

            BlobAssetReference<EM_Blob_Library> blobAsset = builder.CreateBlobAssetReference<EM_Blob_Library>(Allocator.Persistent);
            builder.Dispose();

            return blobAsset;
        }
        #endregion
    }
}
