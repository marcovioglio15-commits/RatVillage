using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EmergentMechanics
{
    // Authoring component for NPC simulation data.
    public sealed partial class EM_Authoring_VillageNpc : MonoBehaviour
    {
        #region Nested Types
        // Serialized data blocks for NPC setup.
        [Serializable]
        public struct NeedActivityRateEntry
        {
            [Tooltip("Id definition matched against the current schedule activity. Leave empty to use as the default rate when no activity-specific entry matches.")]
            [EM_IdSelector(EM_IdCategory.Activity)]
            public EM_IdDefinition ActivityIdDefinition;

            [Tooltip("Legacy activity id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string ActivityId;

            [Tooltip("Need rate curve for this activity (X: normalized activity time 0-1 from activity start to max duration, Y: rate per hour). Sampled into 31 points at bake time.")]
            public AnimationCurve RatePerHour;
        }

        [Serializable]
        public struct NeedProfileEntry
        {
            [Tooltip("Id definition for the need matching Emergence need definitions.")]
            [EM_IdSelector(EM_IdCategory.Need)]
            public EM_IdDefinition NeedIdDefinition;

            [Tooltip("Legacy need id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string NeedId;

            [Tooltip("Initial need value.")]
            public float InitialValue;

            [Tooltip("Minimum clamp value for the need.")]
            public float MinValue;

            [Tooltip("Maximum clamp value for the need.")]
            public float MaxValue;

            [Tooltip("Per-activity need rate curves. Add one entry with an empty ActivityId to act as the default rate.")]
            public NeedActivityRateEntry[] ActivityRates;

            [Tooltip("Id definition for the resource used to satisfy the need.")]
            [EM_IdSelector(EM_IdCategory.Resource)]
            public EM_IdDefinition ResourceIdDefinition;

            [Tooltip("Legacy resource id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string ResourceId;

            [Tooltip("Base resource amount requested when resolving this need.")]
            public float RequestAmount;

            [Tooltip("Need reduction applied per resource unit transferred.")]
            public float NeedSatisfactionPerUnit;

            [Tooltip("Urgency threshold (0-1) above which damage is applied.")]
            public float DamageThreshold;

            [Tooltip("Damage dealt per simulated hour while above the threshold.")]
            public float DamagePerHour;
        }

        [Serializable]
        public struct ResourceEntry
        {
            [Tooltip("Id definition for the resource used for trade and consumption.")]
            [EM_IdSelector(EM_IdCategory.Resource)]
            public EM_IdDefinition ResourceIdDefinition;

            [Tooltip("Legacy resource id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string ResourceId;

            [Tooltip("Initial resource amount.")]
            public float Amount;
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
            [Tooltip("Id definition for the target NPC type used for affinity seeds.")]
            [EM_IdSelector(EM_IdCategory.NpcType)]
            public EM_IdDefinition TargetTypeIdDefinition;

            [Tooltip("Legacy NPC type id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
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

        [Tooltip("Initial reputation value.")]
        [SerializeField] private float initialReputation;

        [Tooltip("Id definition for the NPC type used for relationship defaults and grouping.")]
        [EM_IdSelector(EM_IdCategory.NpcType)]
        [SerializeField] private EM_IdDefinition npcTypeIdDefinition;

        [Tooltip("Legacy NPC type id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string npcTypeId;

        [Tooltip("Color used to format this NPC's log messages in the debug HUD.")]
        [SerializeField] private Color logMessageColor = Color.white;
        #endregion

        #region Schedule
        [Tooltip("Schedule preset defining this NPC's daily activities. If null, no schedule signals are emitted.")]
        [Header("Schedule")]
        [SerializeField] private EM_NpcSchedulePreset schedulePreset;
        #endregion

        #region Needs
        [Tooltip("Per-NPC multiplier variance applied to need rate curves. A value of 0.1 produces random multipliers between 0.9 and 1.1 per need.")]
        [Header("Needs")]
        [SerializeField] private float needRateVariance = 0.1f;

        [Tooltip("Need profiles defining initial values and simulation settings.")]
        [SerializeField] private NeedProfileEntry[] needs = new NeedProfileEntry[0];

        #endregion

        #region Resources
        [Tooltip("Initial resource inventory for this NPC.")]
        [Header("Resources")]
        [SerializeField] private ResourceEntry[] resources = new ResourceEntry[0];
        #endregion

        #region Relationships
        [Tooltip("Initial affinity seeds against NPC types.")]
        [Header("Relationship Types")]
        [SerializeField] private RelationshipTypeEntry[] relationshipTypes = new RelationshipTypeEntry[0];
        #endregion

        #region Trade Preferences
        [Tooltip("Affinity multiplier curve for resource transfers from this NPC to a requester. X is provider->requester affinity mapped to 0-1, Y is the transfer multiplier.")]
        [Header("Trade Preferences")]
        [SerializeField] private AnimationCurve affinityTransferMultiplier = AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f);

        [Tooltip("Minimum clamp for the affinity-based transfer multiplier.")]
        [SerializeField] private float affinityMultiplierMin = 0f;

        [Tooltip("Maximum clamp for the affinity-based transfer multiplier.")]
        [SerializeField] private float affinityMultiplierMax = 2f;
        #endregion

        #endregion
        #endregion

        #region Baker
        public sealed class Baker : Baker<EM_Authoring_VillageNpc>
        {
            // Bake authoring data into ECS components and buffers.
            public override void Bake(EM_Authoring_VillageNpc authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Entity rootEntity = Entity.Null;

                if (authoring.societyRoot != null)
                    rootEntity = GetEntity(authoring.societyRoot, TransformUsageFlags.None);

                AddComponent(entity, new EM_Component_SocietyMember { SocietyRoot = rootEntity });
                AddComponent(entity, new EM_Component_Reputation { Value = authoring.initialReputation });
                AddComponent(entity, new EM_Component_LogColor
                {
                    Value = new float4(authoring.logMessageColor.r, authoring.logMessageColor.g, authoring.logMessageColor.b, authoring.logMessageColor.a)
                });
                AddComponent(entity, new EM_Component_RandomSeed { Value = GetStableSeed(authoring.name) });
                AddComponent(entity, new EM_Component_NpcNeedTickState { NextTick = -1d });
                AddComponent(entity, new EM_Component_NpcNeedRateSettings { RateMultiplierVariance = math.max(0f, authoring.needRateVariance) });
                AddMovementComponents(authoring, entity, this);
                AddTradeInteractionComponents(authoring, entity, this);
                AddHealthComponents(authoring, entity, this);
                AddComponent<EM_Component_SignalEmitter>(entity);
                AddBuffer<EM_BufferElement_SignalEvent>(entity);
                AddBuffer<EM_BufferElement_MetricAccumulator>(entity);
                AddBuffer<EM_BufferElement_MetricTimer>(entity);
                AddBuffer<EM_BufferElement_MetricEventSample>(entity);
                AddBuffer<EM_BufferElement_RuleCooldown>(entity);

                string npcTypeIdValue = EM_IdUtility.ResolveId(authoring.npcTypeIdDefinition, authoring.npcTypeId);

                if (!string.IsNullOrWhiteSpace(npcTypeIdValue))
                {
                    EM_Component_NpcType npcType = new EM_Component_NpcType
                    {
                        TypeId = new FixedString64Bytes(npcTypeIdValue)
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
                        IsOverride = 0
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleTarget
                    {
                        EntryIndex = -1,
                        ActivityId = default,
                        LocationId = default,
                        IsOverride = 0,
                        TradeCapable = 0
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleOverride
                    {
                        ActivityId = default,
                        RemainingHours = 0f,
                        DurationHours = 0f,
                        EntryIndex = -1
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleOverrideGate
                    {
                        LastOverrideTimeSeconds = -1d,
                        LastOverridePriority = -1f,
                        LastOverrideActivityId = default
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleOverrideCooldownSettings
                    {
                        SameOverrideCooldownHours = math.max(0f, authoring.sameOverrideCooldownHours),
                        AnyOverrideCooldownHours = math.max(0f, authoring.anyOverrideCooldownHours)
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleOverrideCooldownState
                    {
                        LastOverrideEndTimeSeconds = -1d,
                        LastOverrideActivityId = default,
                        ActiveOverrideActivityId = default,
                        WasOverrideActive = 0
                    });
                    AddComponent(entity, new EM_Component_NpcScheduleDuration
                    {
                        ActivityId = default,
                        RemainingHours = 0f,
                        DurationHours = 0f,
                        EntryIndex = -1
                    });
                    AddBuffer<EM_BufferElement_NpcScheduleSignalState>(entity);
                }

                DynamicBuffer<EM_BufferElement_Need> needBuffer = AddBuffer<EM_BufferElement_Need>(entity);
                DynamicBuffer<EM_BufferElement_NeedSetting> ruleBuffer = AddBuffer<EM_BufferElement_NeedSetting>(entity);
                DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRateBuffer = AddBuffer<EM_BufferElement_NeedActivityRate>(entity);
                DynamicBuffer<EM_BufferElement_Resource> resourceBuffer = AddBuffer<EM_BufferElement_Resource>(entity);
                DynamicBuffer<EM_BufferElement_Relationship> relationshipBuffer = AddBuffer<EM_BufferElement_Relationship>(entity);
                DynamicBuffer<EM_BufferElement_RelationshipType> relationshipTypeBuffer = AddBuffer<EM_BufferElement_RelationshipType>(entity);
                AddBuffer<EM_BufferElement_Intent>(entity);

                AddNeedProfiles(authoring.needs, ref needBuffer, ref ruleBuffer, ref activityRateBuffer);
                AddResources(authoring.resources, ref resourceBuffer);
                AddRelationshipTypes(authoring.relationshipTypes, ref relationshipTypeBuffer);
                AddTradePreferences(authoring.affinityTransferMultiplier, authoring.affinityMultiplierMin, authoring.affinityMultiplierMax, entity, this);
            }
        }
        #endregion
    }
}
