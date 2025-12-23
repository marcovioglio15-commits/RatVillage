using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Builds a blob asset from a society profile and library mapping.
    /// </summary>
    internal static class EmergenceSocietyProfileBlobBuilder
    {
        #region Public
        /// <summary>
        /// Builds the society profile blob asset from the provided profile and library.
        /// </summary>
        public static BlobAssetReference<EmergenceSocietyProfileBlob> BuildProfileBlob(EM_SocietyProfile profile, EM_MechanicLibrary library)
        {
            if (profile == null)
                return default;

            EM_RuleSetDefinition[] libraryRuleSets = library != null ? library.RuleSets : null;
            EM_MetricDefinition[] libraryMetrics = library != null ? library.Metrics : null;

            Dictionary<EM_RuleSetDefinition, int> ruleSetIndices = BuildIndexDictionary(libraryRuleSets);
            Dictionary<EM_MetricDefinition, int> metricIndices = BuildIndexDictionary(libraryMetrics);

            byte[] ruleSetMask = BuildMask(profile.RuleSets, ruleSetIndices, libraryRuleSets != null ? libraryRuleSets.Length : 0);
            byte[] metricMask = BuildMask(profile.Metrics, metricIndices, libraryMetrics != null ? libraryMetrics.Length : 0);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EmergenceSocietyProfileBlob root = ref builder.ConstructRoot<EmergenceSocietyProfileBlob>();

            root.ProfileId = new FixedString64Bytes(profile.ProfileId);
            root.Volatility = profile.Volatility;
            root.ShockAbsorption = profile.ShockAbsorption;
            root.NoiseAmplitude = profile.NoiseAmplitude;
            root.CrisisThreshold = profile.CrisisThreshold;
            root.FullSimTickRate = profile.FullSimTickRate;
            root.SimplifiedSimTickRate = profile.SimplifiedSimTickRate;
            root.AggregatedSimTickRate = profile.AggregatedSimTickRate;
            root.MaxSignalQueue = profile.MaxSignalQueue;
            root.RegionSize = new float2(profile.RegionSize.x, profile.RegionSize.y);

            BlobBuilderArray<byte> ruleMaskArray = builder.Allocate(ref root.RuleSetMask, ruleSetMask.Length);
            BlobBuilderArray<byte> metricMaskArray = builder.Allocate(ref root.MetricMask, metricMask.Length);

            for (int i = 0; i < ruleSetMask.Length; i++)
            {
                ruleMaskArray[i] = ruleSetMask[i];
            }

            for (int i = 0; i < metricMask.Length; i++)
            {
                metricMaskArray[i] = metricMask[i];
            }

            BlobAssetReference<EmergenceSocietyProfileBlob> blobAsset = builder.CreateBlobAssetReference<EmergenceSocietyProfileBlob>(Allocator.Persistent);
            builder.Dispose();

            return blobAsset;
        }
        #endregion

        #region Helpers
        private static Dictionary<T, int> BuildIndexDictionary<T>(T[] source) where T : Object
        {
            Dictionary<T, int> map = new Dictionary<T, int>();

            if (source == null)
                return map;

            for (int i = 0; i < source.Length; i++)
            {
                T item = source[i];

                if (item == null)
                    continue;

                map[item] = i;
            }

            return map;
        }

        private static byte[] BuildMask<T>(T[] activeItems, Dictionary<T, int> indexMap, int totalCount) where T : UnityEngine.Object
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
        #endregion
    }
}
