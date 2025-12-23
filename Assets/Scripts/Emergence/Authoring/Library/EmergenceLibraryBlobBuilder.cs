using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Builds a blob asset from emergence library definitions.
    /// </summary>
    internal static partial class EmergenceLibraryBlobBuilder
    {
        #region Types
        private struct RuleBuildRecord
        {
            public FixedString64Bytes SignalId;
            public int SignalIndex;
            public int EffectIndex;
            public int RuleSetIndex;
            public int Priority;
            public float Weight;
            public float MinimumSignalValue;
            public float CooldownSeconds;
        }

        private sealed class RuleBuildRecordComparer : IComparer<RuleBuildRecord>
        {
            /// <summary>
            /// Compares rule records by signal id, then by priority.
            /// </summary>
            public int Compare(RuleBuildRecord left, RuleBuildRecord right)
            {
                int signalComparison = left.SignalId.CompareTo(right.SignalId);

                if (signalComparison != 0)
                    return signalComparison;

                return left.Priority.CompareTo(right.Priority);
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Builds the library blob asset from the provided library.
        /// </summary>
        public static BlobAssetReference<EmergenceLibraryBlob> BuildLibraryBlob(EM_MechanicLibrary library)
        {
            if (library == null)
                return default;

            List<EM_SignalDefinition> signals = BuildUniqueList(library.Signals, GetSignalId);
            List<EM_EffectDefinition> effects = BuildUniqueList(library.Effects, GetEffectId);
            List<EM_MetricDefinition> metrics = BuildUniqueList(library.Metrics, GetMetricId);
            List<EM_RuleSetDefinition> ruleSets = BuildUniqueList(library.RuleSets, GetRuleSetId);

            Dictionary<EM_SignalDefinition, int> signalIndices = BuildIndexMap(signals);
            Dictionary<EM_EffectDefinition, int> effectIndices = BuildIndexMap(effects);
            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices = BuildIndexMap(ruleSets);

            RuleBuildRecord[] rules = BuildRuleRecords(ruleSets, ruleSetIndices, signalIndices, effectIndices);
            EmergenceRuleGroupBlob[] ruleGroups = BuildRuleGroups(rules);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EmergenceLibraryBlob root = ref builder.ConstructRoot<EmergenceLibraryBlob>();

            BlobBuilderArray<EmergenceSignalBlob> signalArray = builder.Allocate(ref root.Signals, signals.Count);
            BlobBuilderArray<EmergenceEffectBlob> effectArray = builder.Allocate(ref root.Effects, effects.Count);
            BlobBuilderArray<EmergenceMetricBlob> metricArray = builder.Allocate(ref root.Metrics, metrics.Count);
            BlobBuilderArray<EmergenceRuleSetBlob> ruleSetArray = builder.Allocate(ref root.RuleSets, ruleSets.Count);
            BlobBuilderArray<EmergenceRuleBlob> ruleArray = builder.Allocate(ref root.Rules, rules.Length);
            BlobBuilderArray<EmergenceRuleGroupBlob> groupArray = builder.Allocate(ref root.RuleGroups, ruleGroups.Length);

            for (int i = 0; i < signals.Count; i++)
            {
                EM_SignalDefinition signal = signals[i];
                FixedString64Bytes signalId = new FixedString64Bytes(signal.SignalId);
                FixedString64Bytes domainId = new FixedString64Bytes(string.Empty);

                if (signal.Domain != null)
                    domainId = new FixedString64Bytes(signal.Domain.DomainId);

                signalArray[i] = new EmergenceSignalBlob
                {
                    SignalId = signalId,
                    DomainId = domainId,
                    MinimumLod = signal.MinimumLod,
                    DefaultWeight = signal.DefaultWeight
                };
            }

            for (int i = 0; i < effects.Count; i++)
            {
                EM_EffectDefinition effect = effects[i];

                effectArray[i] = new EmergenceEffectBlob
                {
                    EffectId = new FixedString64Bytes(effect.EffectId),
                    EffectType = effect.EffectType,
                    Target = effect.Target,
                    ParameterId = new FixedString64Bytes(effect.ParameterId),
                    Magnitude = effect.Magnitude,
                    UseClamp = effect.UseClamp ? (byte)1 : (byte)0,
                    MinValue = effect.MinValue,
                    MaxValue = effect.MaxValue
                };
            }

            for (int i = 0; i < metrics.Count; i++)
            {
                EM_MetricDefinition metric = metrics[i];
                int thresholdSignalIndex = -1;

                if (metric.ThresholdSignal != null)
                {
                    int mappedIndex;
                    bool found = signalIndices.TryGetValue(metric.ThresholdSignal, out mappedIndex);

                    if (found)
                        thresholdSignalIndex = mappedIndex;
                }

                metricArray[i] = new EmergenceMetricBlob
                {
                    MetricId = new FixedString64Bytes(metric.MetricId),
                    MetricType = metric.MetricType,
                    SampleInterval = metric.SampleInterval,
                    ParameterId = new FixedString64Bytes(metric.ParameterId),
                    WarningThreshold = metric.WarningThreshold,
                    CriticalThreshold = metric.CriticalThreshold,
                    ThresholdSignalIndex = thresholdSignalIndex
                };
            }

            for (int i = 0; i < ruleSets.Count; i++)
            {
                EM_RuleSetDefinition ruleSet = ruleSets[i];
                FixedString64Bytes domainId = new FixedString64Bytes(string.Empty);

                if (ruleSet.Domain != null)
                    domainId = new FixedString64Bytes(ruleSet.Domain.DomainId);

                ruleSetArray[i] = new EmergenceRuleSetBlob
                {
                    RuleSetId = new FixedString64Bytes(ruleSet.RuleSetId),
                    DomainId = domainId,
                    IsEnabled = ruleSet.IsEnabled ? (byte)1 : (byte)0
                };
            }

            for (int i = 0; i < rules.Length; i++)
            {
                ruleArray[i] = new EmergenceRuleBlob
                {
                    SignalIndex = rules[i].SignalIndex,
                    EffectIndex = rules[i].EffectIndex,
                    RuleSetIndex = rules[i].RuleSetIndex,
                    Priority = rules[i].Priority,
                    Weight = rules[i].Weight,
                    MinimumSignalValue = rules[i].MinimumSignalValue,
                    CooldownSeconds = rules[i].CooldownSeconds
                };
            }

            for (int i = 0; i < ruleGroups.Length; i++)
            {
                groupArray[i] = ruleGroups[i];
            }

            BlobAssetReference<EmergenceLibraryBlob> blobAsset = builder.CreateBlobAssetReference<EmergenceLibraryBlob>(Allocator.Persistent);
            builder.Dispose();

            return blobAsset;
        }
        #endregion
    }
}
