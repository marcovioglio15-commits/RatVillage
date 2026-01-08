using System;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_SocietySimulation : MonoBehaviour
    {
        #region Nested Types
        // Serialized society resource entry.
        [Serializable]
        public struct ResourceEntry
        {
            [Tooltip("Id definition for the resource stored by the society.")]
            [EM_IdSelector(EM_IdCategory.Resource)]
            public EM_IdDefinition ResourceIdDefinition;

            [Tooltip("Legacy resource id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string ResourceId;

            [Tooltip("Initial amount stored in the society pool.")]
            public float Amount;
        }

        [Serializable]
        public struct NeedSignalOverrideEntry
        {
            [Tooltip("Id definition for the need that this override applies to.")]
            [EM_IdSelector(EM_IdCategory.Need)]
            public EM_IdDefinition NeedIdDefinition;

            [Tooltip("Legacy need id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string NeedId;

            [Tooltip("Id definition for the signal emitted with the raw need value. Leave empty to use the default value signal id.")]
            [EM_IdSelector(EM_IdCategory.Signal)]
            public EM_IdDefinition ValueSignalIdDefinition;

            [Tooltip("Legacy value signal id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string ValueSignalId;

            [Tooltip("Id definition for the signal emitted with the normalized need urgency. Leave empty to use the default urgency signal id.")]
            [EM_IdSelector(EM_IdCategory.Signal)]
            public EM_IdDefinition UrgencySignalIdDefinition;

            [Tooltip("Legacy urgency signal id string (auto-migrated when missing an id definition).")]
            [HideInInspector]
            public string UrgencySignalId;
        }
        #endregion

        #region Fields

        // Serialized authoring inputs.
        #region Serialized
        #region Clock
        [Tooltip("Length of a full simulated day in seconds.")]
        [Header("Clock")]
        [SerializeField] private float dayLengthSeconds = 120f;

        [Tooltip("Initial time of day in hours (0-24). Used to align schedules at startup.")]
        [SerializeField] private float startTimeOfDay = 6f;

        [Tooltip("Base simulation speed multiplier applied to the clock. Use the runtime slider to scale this value.")]
        [SerializeField] private float simulationSpeed = 1f;
        #endregion

        #region Society Resources
        [Tooltip("Initial society resource pool. Used by distribution before NPC trade.")]
        [Header("Society Resources")]
        [SerializeField] private ResourceEntry[] societyResources = new ResourceEntry[0];
        #endregion

        #region Needs
        [Tooltip("Tick interval in simulated hours for need decay updates.")]
        [Header("Needs")]
        [SerializeField] private float needTickIntervalHours = 0.5f;

        [Tooltip("Id definition for the default signal emitted with the raw need value when no override is configured. Leave empty to disable.")]
        [EM_IdSelector(EM_IdCategory.Signal)]
        [SerializeField] private EM_IdDefinition needValueSignalIdDefinition;

        [Tooltip("Legacy need value signal id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string needValueSignalId = "Need.Value";

        [Tooltip("Id definition for the default signal emitted with the normalized need urgency when no override is configured. Leave empty to disable.")]
        [EM_IdSelector(EM_IdCategory.Signal)]
        [SerializeField] private EM_IdDefinition needUrgencySignalIdDefinition;

        [Tooltip("Legacy need urgency signal id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string needUrgencySignalId = "Need.Urgency";

        [Tooltip("Per-need overrides for emitted signal ids. Leave ids empty to use the defaults.")]
        [SerializeField] private NeedSignalOverrideEntry[] needSignalOverrides = new NeedSignalOverrideEntry[0];
        #endregion

        #region Trade
        [Tooltip("Tick interval in simulated hours for trade evaluation.")]
        [Header("Trade")]
        [SerializeField] private float tradeTickIntervalHours = 0.5f;

        [Tooltip("Base acceptance probability for trade.")]
        [Range(0f, 1f)]
        [SerializeField] private float tradeBaseAcceptance = 0.5f;

        [Tooltip("Multiplier applied to affinity when computing acceptance.")]
        [SerializeField] private float tradeAffinityWeight = 0.5f;

        [Tooltip("Id definition for the signal emitted on trade success.")]
        [EM_IdSelector(EM_IdCategory.Signal)]
        [SerializeField] private EM_IdDefinition tradeSuccessSignalIdDefinition;

        [Tooltip("Legacy trade success signal id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string tradeSuccessSignalId = "Trade.Success";

        [Tooltip("Id definition for the signal emitted on trade failure.")]
        [EM_IdSelector(EM_IdCategory.Signal)]
        [SerializeField] private EM_IdDefinition tradeFailSignalIdDefinition;

        [Tooltip("Legacy trade fail signal id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string tradeFailSignalId = "Trade.Fail";
        #endregion

        #region Schedule Overrides
        [Tooltip("When enabled, schedule overrides can only start if the NPC is not already in an override activity.")]
        [Header("Schedule Overrides")]
        [SerializeField] private bool blockOverrideWhileOverridden = true;
        #endregion

        #endregion

        #endregion

        #region Baker
        // Entity conversion pipeline.
        public sealed class Baker : Baker<EM_Authoring_SocietySimulation>
        {
            public override void Bake(EM_Authoring_SocietySimulation authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                float startTime = Mathf.Clamp(authoring.startTimeOfDay, 0f, 24f);

                EM_Component_SocietyClock clock = new EM_Component_SocietyClock
                {
                    DayLengthSeconds = authoring.dayLengthSeconds,
                    TimeOfDay = startTime,
                    SimulatedTimeSeconds = startTime * 3600d,
                    BaseSimulationSpeed = Mathf.Max(0f, authoring.simulationSpeed),
                    SimulationSpeedMultiplier = 1f
                };

                EM_Component_NeedTickSettings needSettings = new EM_Component_NeedTickSettings
                {
                    TickIntervalHours = authoring.needTickIntervalHours
                };

                EM_Component_NeedSignalSettings needSignalSettings = new EM_Component_NeedSignalSettings
                {
                    NeedValueSignalId = EM_IdUtility.ToFixed(authoring.needValueSignalIdDefinition, authoring.needValueSignalId),
                    NeedUrgencySignalId = EM_IdUtility.ToFixed(authoring.needUrgencySignalIdDefinition, authoring.needUrgencySignalId)
                };

                EM_Component_NeedTickState needState = new EM_Component_NeedTickState
                {
                    NextTick = 0d
                };

                EM_Component_TradeSettings tradeSettings = new EM_Component_TradeSettings
                {
                    TradeTickIntervalHours = authoring.tradeTickIntervalHours,
                    BaseAcceptance = authoring.tradeBaseAcceptance,
                    AffinityWeight = authoring.tradeAffinityWeight,
                    TradeSuccessSignalId = EM_IdUtility.ToFixed(authoring.tradeSuccessSignalIdDefinition, authoring.tradeSuccessSignalId),
                    TradeFailSignalId = EM_IdUtility.ToFixed(authoring.tradeFailSignalIdDefinition, authoring.tradeFailSignalId)
                };

                EM_Component_ScheduleOverrideSettings scheduleOverrideSettings = new EM_Component_ScheduleOverrideSettings
                {
                    BlockOverrideWhileOverridden = (byte)(authoring.blockOverrideWhileOverridden ? 1 : 0)
                };

                EM_Component_TradeTickState tradeState = new EM_Component_TradeTickState
                {
                    NextTick = 0d
                };

                AddComponent(entity, clock);
                AddComponent(entity, needSettings);
                AddComponent(entity, needSignalSettings);
                AddComponent(entity, needState);
                AddComponent(entity, tradeSettings);
                AddComponent(entity, scheduleOverrideSettings);
                AddComponent(entity, tradeState);
                AddComponent<EM_Component_SignalEmitter>(entity);
                AddBuffer<EM_BufferElement_SignalEvent>(entity);
                DynamicBuffer<EM_BufferElement_NeedSignalOverride> needSignalBuffer = AddBuffer<EM_BufferElement_NeedSignalOverride>(entity);
                DynamicBuffer<EM_BufferElement_Resource> societyResourceBuffer = AddBuffer<EM_BufferElement_Resource>(entity);
                AddNeedSignalOverrides(authoring.needSignalOverrides, ref needSignalBuffer);
                AddSocietyResources(authoring.societyResources, ref societyResourceBuffer);
            }
        }
        #endregion
    }
}
