using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    // Authoring component for NPC simulation data.
    public sealed partial class EM_Authoring_VillageNpc : MonoBehaviour
    {
        #region Nested Types
        // Serialized data blocks for NPC setup.
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

            [Tooltip("Need increase rate curve (X: normalized need 0-1 between MinValue and MaxValue, Y: rate per hour). Sampled into 31 points at bake time.")]
            public AnimationCurve RatePerHour;

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

        [Serializable]
        public struct RelationshipTypeEntry
        {
            [Tooltip("Target NPC type identifier for the affinity seed.")]
            public string TargetTypeId;

            [Tooltip("Initial affinity from -1 (hostile) to 1 (friendly).")]
            [Range(-1f, 1f)]
            public float Affinity;
        }
        #endregion

        #region Fields

        #region Serialized
        #region Identity
        [Tooltip("Society root GameObject that owns EM_Authoring_SocietyProfile and EM_Authoring_SocietySimulation. Assign the SubScene root representing the NPC's society so membership, schedule, and resource sharing resolve correctly.")]
        [Header("Identity")]
        [SerializeField] private GameObject societyRoot;

        [Tooltip("LOD tier for this NPC.")]
        [SerializeField] private EmergenceLodTier lodTier = EmergenceLodTier.Full;

        [Tooltip("Initial reputation value.")]
        [SerializeField] private float initialReputation;

        [Tooltip("Random seed (0 to auto-generate).")]
        [SerializeField] private uint randomSeed;

        [Tooltip("NPC type identifier used for relationship defaults and grouping.")]
        [SerializeField] private string npcTypeId;
        #endregion

        #region Schedule
        [Tooltip("Schedule preset defining this NPC's daily activities. If null, no schedule signals are emitted.")]
        [Header("Schedule")]
        [SerializeField] private EM_NpcSchedulePreset schedulePreset;
        #endregion

        #region Needs
        [Tooltip("Initial need values for this NPC.")]
        [Header("Needs")]
        [SerializeField] private NeedEntry[] needs = new NeedEntry[0];

        [Tooltip("Need rules used by trade and decay systems.")]
        [SerializeField] private NeedRuleEntry[] needRules = new NeedRuleEntry[0];
        #endregion

        #region Resources
        [Tooltip("Initial resource inventory for this NPC.")]
        [Header("Resources")]
        [SerializeField] private ResourceEntry[] resources = new ResourceEntry[0];
        #endregion

        #region Relationships
        [Tooltip("Initial relationships for this NPC.")]
        [Header("Relationships")]
        [SerializeField] private RelationshipEntry[] relationships = new RelationshipEntry[0];

        [Tooltip("Initial affinity seeds against NPC types.")]
        [Header("Relationship Types")]
        [SerializeField] private RelationshipTypeEntry[] relationshipTypes = new RelationshipTypeEntry[0];
        #endregion
        #endregion
        #endregion

        #region Baker
        public sealed class Baker : Baker<EM_Authoring_VillageNpc>
        {
            // Bake authoring data into ECS components and buffers.
            public override void Bake(EM_Authoring_VillageNpc authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                Entity rootEntity = Entity.Null;

                if (authoring.societyRoot != null)
                    rootEntity = GetEntity(authoring.societyRoot, TransformUsageFlags.None);

                AddComponent(entity, new EM_Component_SocietyMember { SocietyRoot = rootEntity });
                AddComponent(entity, new EM_Component_SocietyLod { Tier = authoring.lodTier });
                AddComponent(entity, new EM_Component_Reputation { Value = authoring.initialReputation });
                AddComponent(entity, new EM_Component_RandomSeed { Value = GetSeed(authoring.randomSeed, authoring.name) });
                AddComponent<EM_Component_SignalEmitter>(entity);
                AddBuffer<EM_BufferElement_SignalEvent>(entity);
                AddBuffer<EM_BufferElement_MetricAccumulator>(entity);
                AddBuffer<EM_BufferElement_MetricTimer>(entity);
                AddBuffer<EM_BufferElement_RuleCooldown>(entity);

                if (!string.IsNullOrWhiteSpace(authoring.npcTypeId))
                {
                    EM_Component_NpcType npcType = new EM_Component_NpcType
                    {
                        TypeId = new FixedString64Bytes(authoring.npcTypeId)
                    };

                    AddComponent(entity, npcType);
                }

                BlobAssetReference<EM_BlobDefinition_NpcSchedule> scheduleBlob = BuildScheduleBlob(authoring.schedulePreset);

                if (scheduleBlob.IsCreated)
                {
                    AddBlobAsset(ref scheduleBlob, out Unity.Entities.Hash128 _);
                    AddComponent(entity, new EM_Component_NpcSchedule { Schedule = scheduleBlob });
                    AddComponent(entity, new EM_Component_NpcScheduleState
                    {
                        CurrentEntryIndex = -1,
                        CurrentActivityId = default,
                        TickAccumulatorHours = 0f,
                        IsOverride = 0
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleOverride
                    {
                        ActivityId = default,
                        RemainingHours = 0f,
                        DurationHours = 0f,
                        EntryIndex = -1
                    });
                }

                DynamicBuffer<EM_BufferElement_Need> needBuffer = AddBuffer<EM_BufferElement_Need>(entity);
                DynamicBuffer<EM_BufferElement_NeedRule> ruleBuffer = AddBuffer<EM_BufferElement_NeedRule>(entity);
                DynamicBuffer<EM_BufferElement_NeedResolutionState> stateBuffer = AddBuffer<EM_BufferElement_NeedResolutionState>(entity);
                DynamicBuffer<EM_BufferElement_Resource> resourceBuffer = AddBuffer<EM_BufferElement_Resource>(entity);
                DynamicBuffer<EM_BufferElement_Relationship> relationshipBuffer = AddBuffer<EM_BufferElement_Relationship>(entity);
                DynamicBuffer<EM_BufferElement_RelationshipType> relationshipTypeBuffer = AddBuffer<EM_BufferElement_RelationshipType>(entity);

                AddNeeds(authoring.needs, ref needBuffer);
                AddNeedRules(authoring.needRules, ref ruleBuffer, ref stateBuffer);
                AddResources(authoring.resources, ref resourceBuffer);
                AddRelationships(authoring.relationships, ref relationshipBuffer, this);
                AddRelationshipTypes(authoring.relationshipTypes, ref relationshipTypeBuffer);
            }
        }
        #endregion
    }
}
