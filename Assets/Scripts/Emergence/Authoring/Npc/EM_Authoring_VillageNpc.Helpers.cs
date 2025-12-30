using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Helpers
        // Schedule preset baking into ECS blobs.
        #region Schedule
        private static BlobAssetReference<EM_BlobDefinition_NpcSchedule> BuildScheduleBlob(EM_NpcSchedulePreset preset)
        {
            if (preset == null)
                return default;

            EM_NpcSchedulePreset.ScheduleEntry[] entries = preset.Entries;

            if (entries == null || entries.Length == 0)
                return default;

            int validCount = CountValidScheduleEntries(entries);

            if (validCount == 0)
                return default;

            int sampleCount = GetSampleCount(preset.CurveSamples);
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EM_BlobDefinition_NpcSchedule root = ref builder.ConstructRoot<EM_BlobDefinition_NpcSchedule>();
            BlobBuilderArray<EM_Blob_NpcScheduleEntry> blobEntries = builder.Allocate(ref root.Entries, validCount);
            int writeIndex = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                EM_NpcSchedulePreset.ScheduleEntry entry = entries[i];

                if (string.IsNullOrWhiteSpace(entry.ActivityId))
                    continue;

                ref EM_Blob_NpcScheduleEntry blobEntry = ref blobEntries[writeIndex];
                blobEntry.ActivityId = new FixedString64Bytes(entry.ActivityId);
                blobEntry.StartHour = entry.StartHour;
                blobEntry.EndHour = entry.EndHour;
                blobEntry.TickIntervalHours = entry.TickIntervalHours;
                blobEntry.StartSignalId = ToFixed(entry.StartSignalId);
                blobEntry.TickSignalId = ToFixed(entry.TickSignalId);

                BlobBuilderArray<float> curveSamples = builder.Allocate(ref blobEntry.CurveSamples, sampleCount);

                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    float t = sampleCount > 1 ? (float)sampleIndex / (sampleCount - 1) : 0f;
                    curveSamples[sampleIndex] = EvaluateCurve(entry.ActivityCurve, t);
                }

                writeIndex++;
            }

            BlobAssetReference<EM_BlobDefinition_NpcSchedule> scheduleBlob = builder.CreateBlobAssetReference<EM_BlobDefinition_NpcSchedule>(Allocator.Persistent);
            builder.Dispose();

            return scheduleBlob;
        }

        private static int CountValidScheduleEntries(EM_NpcSchedulePreset.ScheduleEntry[] entries)
        {
            int count = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(entries[i].ActivityId))
                    continue;

                count++;
            }

            return count;
        }

        private static int GetSampleCount(int value)
        {
            if (value < 4)
                return 4;

            if (value > 128)
                return 128;

            return value;
        }

        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            if (curve == null)
                return 1f;

            return Mathf.Clamp01(curve.Evaluate(t));
        }
        #endregion

        // Need and resource buffers initialization.
        #region NeedsAndResources
        // Need rate curve sampling for decay rules.
        #region NeedRateCurves
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

        private static void AddNeeds(NeedEntry[] source, ref DynamicBuffer<EM_BufferElement_Need> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].NeedId))
                    continue;

                EM_BufferElement_Need need = new EM_BufferElement_Need
                {
                    NeedId = new FixedString64Bytes(source[i].NeedId),
                    Value = source[i].Value
                };

                buffer.Add(need);
            }
        }

        private static void AddNeedRules(NeedRuleEntry[] source, ref DynamicBuffer<EM_BufferElement_NeedRule> ruleBuffer,
            ref DynamicBuffer<EM_BufferElement_NeedResolutionState> stateBuffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].NeedId))
                    continue;

                EM_BufferElement_NeedRule rule = new EM_BufferElement_NeedRule
                {
                    NeedId = new FixedString64Bytes(source[i].NeedId),
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    RatePerHourSamples = BuildNeedRateSamples(source[i].RatePerHour),
                    MinValue = source[i].MinValue,
                    MaxValue = source[i].MaxValue,
                    StartThreshold = source[i].StartThreshold,
                    MaxProbability = source[i].MaxProbability,
                    ProbabilityExponent = source[i].ProbabilityExponent,
                    CooldownSeconds = source[i].CooldownSeconds,
                    ResourceTransferAmount = source[i].ResourceTransferAmount,
                    NeedSatisfactionAmount = source[i].NeedSatisfactionAmount
                };

                EM_BufferElement_NeedResolutionState state = new EM_BufferElement_NeedResolutionState
                {
                    NeedId = rule.NeedId,
                    NextAttemptTime = 0d
                };

                ruleBuffer.Add(rule);
                stateBuffer.Add(state);
            }
        }

        private static void AddResources(ResourceEntry[] source, ref DynamicBuffer<EM_BufferElement_Resource> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].ResourceId))
                    continue;

                EM_BufferElement_Resource resource = new EM_BufferElement_Resource
                {
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    Amount = source[i].Amount
                };

                buffer.Add(resource);
            }
        }
        #endregion

        // Relationship buffer initialization.
        #region Relationships
        #region RelationshipsByEntity
        private static void AddRelationships(RelationshipEntry[] source, ref DynamicBuffer<EM_BufferElement_Relationship> buffer, Baker<EM_Authoring_VillageNpc> baker)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].OtherNpc == null)
                    continue;

                Entity otherEntity = baker.GetEntity(source[i].OtherNpc, TransformUsageFlags.None);

                EM_BufferElement_Relationship relationship = new EM_BufferElement_Relationship
                {
                    Other = otherEntity,
                    Affinity = math.clamp(source[i].Affinity, -1f, 1f)
                };

                buffer.Add(relationship);
            }
        }
        #endregion

        // Relationship type buffer initialization.
        #region RelationshipsByType
        private static void AddRelationshipTypes(RelationshipTypeEntry[] source, ref DynamicBuffer<EM_BufferElement_RelationshipType> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].TargetTypeId))
                    continue;

                EM_BufferElement_RelationshipType relationship = new EM_BufferElement_RelationshipType
                {
                    TypeId = new FixedString64Bytes(source[i].TargetTypeId),
                    Affinity = math.clamp(source[i].Affinity, -1f, 1f)
                };

                buffer.Add(relationship);
            }
        }
        #endregion
        #endregion

        // Generic utility helpers.
        #region Utility
        private static FixedString64Bytes ToFixed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new FixedString64Bytes(string.Empty);

            return new FixedString64Bytes(value);
        }

        private static uint GetSeed(uint seed, string name)
        {
            if (seed != 0u)
                return seed;

            FixedString64Bytes fixedName = new FixedString64Bytes(name);
            uint hashed = (uint)fixedName.GetHashCode();

            if (hashed == 0u)
                return 1u;

            return hashed;
        }
        #endregion
        #endregion
    }
}
