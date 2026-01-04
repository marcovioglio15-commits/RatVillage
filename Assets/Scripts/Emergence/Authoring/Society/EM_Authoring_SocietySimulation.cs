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
            [Tooltip("Resource identifier stored by the society.")]
            public string ResourceId;

            [Tooltip("Initial amount stored in the society pool.")]
            public float Amount;
        }

        [Serializable]
        public struct NeedSignalOverrideEntry
        {
            [Tooltip("Need identifier that this override applies to.")]
            public string NeedId;

            [Tooltip("Signal id emitted with the raw need value. Leave empty to use the default value signal id.")]
            public string ValueSignalId;

            [Tooltip("Signal id emitted with the normalized need urgency. Leave empty to use the default urgency signal id.")]
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
        #endregion

        #region Society Resources
        [Tooltip("Initial society resource pool. Used by distribution before NPC trade.")]
        [Header("Society Resources")]
        [SerializeField] private ResourceEntry[] societyResources = new ResourceEntry[0];
        #endregion

        #region Needs
        [Tooltip("Tick rate in Hz for need decay updates.")]
        [Header("Needs")]
        [SerializeField] private float needTickRate = 1f;

        [Tooltip("Default signal id emitted with the raw need value when no override is configured. Leave empty to disable.")]
        [SerializeField] private string needValueSignalId = "Need.Value";

        [Tooltip("Default signal id emitted with the normalized need urgency when no override is configured. Leave empty to disable.")]
        [SerializeField] private string needUrgencySignalId = "Need.Urgency";

        [Tooltip("Per-need overrides for emitted signal ids. Leave ids empty to use the defaults.")]
        [SerializeField] private NeedSignalOverrideEntry[] needSignalOverrides = new NeedSignalOverrideEntry[0];
        #endregion

        #region Trade
        [Tooltip("Tick rate in Hz for trade evaluation.")]
        [Header("Trade")]
        [SerializeField] private float tradeTickRate = 1f;

        [Tooltip("Base acceptance probability for trade.")]
        [Range(0f, 1f)]
        [SerializeField] private float tradeBaseAcceptance = 0.5f;

        [Tooltip("Multiplier applied to affinity when computing acceptance.")]
        [SerializeField] private float tradeAffinityWeight = 0.5f;

        [Tooltip("Signal emitted on trade success.")]
        [SerializeField] private string tradeSuccessSignalId = "Trade.Success";

        [Tooltip("Signal emitted on trade failure.")]
        [SerializeField] private string tradeFailSignalId = "Trade.Fail";
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

                EM_Component_SocietyClock clock = new EM_Component_SocietyClock
                {
                    DayLengthSeconds = authoring.dayLengthSeconds,
                    TimeOfDay = Mathf.Clamp(authoring.startTimeOfDay, 0f, 24f)
                };

                EM_Component_NeedTickSettings needSettings = new EM_Component_NeedTickSettings
                {
                    TickRate = authoring.needTickRate
                };

                EM_Component_NeedSignalSettings needSignalSettings = new EM_Component_NeedSignalSettings
                {
                    NeedValueSignalId = ToFixed(authoring.needValueSignalId),
                    NeedUrgencySignalId = ToFixed(authoring.needUrgencySignalId)
                };

                EM_Component_NeedTickState needState = new EM_Component_NeedTickState
                {
                    NextTick = 0d
                };

                EM_Component_TradeSettings tradeSettings = new EM_Component_TradeSettings
                {
                    TradeTickRate = authoring.tradeTickRate,
                    BaseAcceptance = authoring.tradeBaseAcceptance,
                    AffinityWeight = authoring.tradeAffinityWeight,
                    TradeSuccessSignalId = ToFixed(authoring.tradeSuccessSignalId),
                    TradeFailSignalId = ToFixed(authoring.tradeFailSignalId)
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
