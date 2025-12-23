using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Authoring component that configures NPC data for Emergence systems.
    /// </summary>
    public sealed class EmergenceNpcAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct NeedEntry
        {
            [Tooltip("Need identifier matching Emergence need definitions.")]
            public string NeedId;

            [Tooltip("Initial need value.")]
            public float Value;
        }

        [Serializable]
        public struct ResourceEntry
        {
            [Tooltip("Resource identifier used for trade and consumption.")]
            public string ResourceId;

            [Tooltip("Initial resource amount.")]
            public float Amount;
        }

        [Serializable]
        public struct NeedRuleEntry
        {
            [Tooltip("Need identifier controlled by this rule.")]
            public string NeedId;

            [Tooltip("Resource identifier used to satisfy the need.")]
            public string ResourceId;

            [Tooltip("Need increase rate per hour.")]
            public float RatePerHour;

            [Tooltip("Minimum clamp value for the need.")]
            public float MinValue;

            [Tooltip("Maximum clamp value for the need.")]
            public float MaxValue;

            [Tooltip("Need value at which resolution probability starts.")]
            public float StartThreshold;

            [Tooltip("Maximum resolution probability.")]
            public float MaxProbability;

            [Tooltip("Exponent applied to probability growth.")]
            public float ProbabilityExponent;

            [Tooltip("Cooldown in seconds between trade attempts.")]
            public float CooldownSeconds;

            [Tooltip("Resource amount transferred on successful trade.")]
            public float ResourceTransferAmount;

            [Tooltip("Need reduction applied on successful trade.")]
            public float NeedSatisfactionAmount;
        }

        [Serializable]
        public struct RelationshipEntry
        {
            [Tooltip("Target NPC for relationship data.")]
            public GameObject OtherNpc;

            [Tooltip("Initial affinity from -1 (hostile) to 1 (friendly).")]
            [Range(-1f, 1f)]
            public float Affinity;
        }

        #region Serialized
        #region Serialized - Identity
        [Tooltip("Society root GameObject that owns EmergenceSocietyProfileAuthoring and EmergenceSocietySimulationAuthoring. Assign the SubScene root representing the NPC's society so membership, schedule, and resource sharing resolve correctly.")]
        [Header("Identity")]
        [SerializeField] private GameObject societyRoot;

        [Tooltip("LOD tier for this NPC.")]
        [SerializeField] private EmergenceLodTier lodTier = EmergenceLodTier.Full;

        [Tooltip("Initial reputation value.")]
        [SerializeField] private float initialReputation;

        [Tooltip("Random seed (0 to auto-generate).")]
        [SerializeField] private uint randomSeed;

        [Tooltip("Optional debug name used by the Emergence debug HUD. Leave empty to use the entity id.")]
        [SerializeField] private string debugName;
        #endregion

        #region Serialized - Needs
        [Tooltip("Initial need values for this NPC.")]
        [Header("Needs")]
        [SerializeField] private NeedEntry[] needs = new NeedEntry[0];

        [Tooltip("Need rules used by trade and decay systems.")]
        [SerializeField] private NeedRuleEntry[] needRules = new NeedRuleEntry[0];
        #endregion

        #region Serialized - Resources
        [Tooltip("Initial resource inventory for this NPC.")]
        [Header("Resources")]
        [SerializeField] private ResourceEntry[] resources = new ResourceEntry[0];
        #endregion

        #region Serialized - Relationships
        [Tooltip("Initial relationships for this NPC.")]
        [Header("Relationships")]
        [SerializeField] private RelationshipEntry[] relationships = new RelationshipEntry[0];
        #endregion
        #endregion

        #region Baker
        /// <summary>
        /// Bakes NPC data into ECS components.
        /// </summary>
        public sealed class Baker : Baker<EmergenceNpcAuthoring>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EmergenceNpcAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                Entity rootEntity = Entity.Null;

                if (authoring.societyRoot != null)
                    rootEntity = GetEntity(authoring.societyRoot, TransformUsageFlags.None);

                AddComponent(entity, new EmergenceSocietyMember { SocietyRoot = rootEntity });
                AddComponent(entity, new EmergenceSocietyLod { Tier = authoring.lodTier });
                AddComponent(entity, new EmergenceReputation { Value = authoring.initialReputation });
                AddComponent(entity, new EmergenceRandomSeed { Value = GetSeed(authoring.randomSeed, authoring.name) });
                AddComponent<EmergenceSignalEmitter>(entity);
                AddBuffer<EmergenceSignalEvent>(entity);

                if (!string.IsNullOrWhiteSpace(authoring.debugName))
                {
                    EmergenceDebugName debugNameComponent = new EmergenceDebugName
                    {
                        Value = new FixedString64Bytes(authoring.debugName)
                    };

                    AddComponent(entity, debugNameComponent);
                }

                DynamicBuffer<EmergenceNeed> needBuffer = AddBuffer<EmergenceNeed>(entity);
                DynamicBuffer<EmergenceNeedRule> ruleBuffer = AddBuffer<EmergenceNeedRule>(entity);
                DynamicBuffer<EmergenceNeedResolutionState> stateBuffer = AddBuffer<EmergenceNeedResolutionState>(entity);
                DynamicBuffer<EmergenceResource> resourceBuffer = AddBuffer<EmergenceResource>(entity);
                DynamicBuffer<EmergenceRelationship> relationshipBuffer = AddBuffer<EmergenceRelationship>(entity);

                AddNeeds(authoring.needs, ref needBuffer);
                AddNeedRules(authoring.needRules, ref ruleBuffer, ref stateBuffer);
                AddResources(authoring.resources, ref resourceBuffer);
                AddRelationships(authoring.relationships, ref relationshipBuffer, this);
            }
        }
        #endregion

        #region Helpers
        private static void AddNeeds(NeedEntry[] source, ref DynamicBuffer<EmergenceNeed> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].NeedId))
                    continue;

                EmergenceNeed need = new EmergenceNeed
                {
                    NeedId = new FixedString64Bytes(source[i].NeedId),
                    Value = source[i].Value
                };

                buffer.Add(need);
            }
        }

        private static void AddNeedRules(NeedRuleEntry[] source, ref DynamicBuffer<EmergenceNeedRule> ruleBuffer,
            ref DynamicBuffer<EmergenceNeedResolutionState> stateBuffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].NeedId))
                    continue;

                EmergenceNeedRule rule = new EmergenceNeedRule
                {
                    NeedId = new FixedString64Bytes(source[i].NeedId),
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    RatePerHour = source[i].RatePerHour,
                    MinValue = source[i].MinValue,
                    MaxValue = source[i].MaxValue,
                    StartThreshold = source[i].StartThreshold,
                    MaxProbability = source[i].MaxProbability,
                    ProbabilityExponent = source[i].ProbabilityExponent,
                    CooldownSeconds = source[i].CooldownSeconds,
                    ResourceTransferAmount = source[i].ResourceTransferAmount,
                    NeedSatisfactionAmount = source[i].NeedSatisfactionAmount
                };

                EmergenceNeedResolutionState state = new EmergenceNeedResolutionState
                {
                    NeedId = rule.NeedId,
                    NextAttemptTime = 0d
                };

                ruleBuffer.Add(rule);
                stateBuffer.Add(state);
            }
        }

        private static void AddResources(ResourceEntry[] source, ref DynamicBuffer<EmergenceResource> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].ResourceId))
                    continue;

                EmergenceResource resource = new EmergenceResource
                {
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    Amount = source[i].Amount
                };

                buffer.Add(resource);
            }
        }

        private static void AddRelationships(RelationshipEntry[] source, ref DynamicBuffer<EmergenceRelationship> buffer, Baker<EmergenceNpcAuthoring> baker)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].OtherNpc == null)
                    continue;

                Entity otherEntity = baker.GetEntity(source[i].OtherNpc, TransformUsageFlags.None);

                EmergenceRelationship relationship = new EmergenceRelationship
                {
                    Other = otherEntity,
                    Affinity = math.clamp(source[i].Affinity, -1f, 1f)
                };

                buffer.Add(relationship);
            }
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
    }
}
