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

        [Tooltip("Affinity delta applied after a successful trade.")]
        [SerializeField] private float affinityChangeOnSuccess = 0.05f;

        [Tooltip("Affinity delta applied after a failed trade.")]
        [SerializeField] private float affinityChangeOnFail = -0.05f;

        [Tooltip("Signal emitted on trade success.")]
        [SerializeField] private string tradeSuccessSignalId = "Trade.Success";

        [Tooltip("Signal emitted on trade failure.")]
        [SerializeField] private string tradeFailSignalId = "Trade.Fail";
        #endregion

        #region Resource Distribution
        [Tooltip("Tick rate in Hz for society resource distribution to members.")]
        [Header("Resource Distribution")]
        [SerializeField] private float distributionTickRate = 0.5f;

        [Tooltip("Max number of transfers per member on each distribution tick.")]
        [SerializeField] private int distributionMaxTransfersPerMember = 1;

        [Tooltip("Fallback transfer amount when a need rule does not specify one.")]
        [SerializeField] private float distributionDefaultTransferAmount = 1f;

        [Tooltip("Fallback need satisfaction when a need rule does not specify one.")]
        [SerializeField] private float distributionDefaultNeedSatisfaction = 1f;
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

                EM_Component_NeedTickState needState = new EM_Component_NeedTickState
                {
                    NextTick = 0d
                };

                EM_Component_TradeSettings tradeSettings = new EM_Component_TradeSettings
                {
                    TradeTickRate = authoring.tradeTickRate,
                    BaseAcceptance = authoring.tradeBaseAcceptance,
                    AffinityWeight = authoring.tradeAffinityWeight,
                    AffinityChangeOnSuccess = authoring.affinityChangeOnSuccess,
                    AffinityChangeOnFail = authoring.affinityChangeOnFail,
                    TradeSuccessSignalId = ToFixed(authoring.tradeSuccessSignalId),
                    TradeFailSignalId = ToFixed(authoring.tradeFailSignalId)
                };

                EM_Component_TradeTickState tradeState = new EM_Component_TradeTickState
                {
                    NextTick = 0d
                };

                EM_Component_SocietyResourceDistributionSettings distributionSettings = new EM_Component_SocietyResourceDistributionSettings
                {
                    DistributionTickRate = authoring.distributionTickRate,
                    MaxTransfersPerMember = authoring.distributionMaxTransfersPerMember,
                    DefaultTransferAmount = authoring.distributionDefaultTransferAmount,
                    DefaultNeedSatisfaction = authoring.distributionDefaultNeedSatisfaction
                };

                EM_Component_SocietyResourceDistributionState distributionState = new EM_Component_SocietyResourceDistributionState
                {
                    NextTick = 0d
                };

                AddComponent(entity, clock);
                AddComponent(entity, needSettings);
                AddComponent(entity, needState);
                AddComponent(entity, tradeSettings);
                AddComponent(entity, tradeState);
                AddComponent(entity, distributionSettings);
                AddComponent(entity, distributionState);
                AddComponent<EM_Component_SignalEmitter>(entity);
                AddBuffer<EM_BufferElement_SignalEvent>(entity);
                DynamicBuffer<EM_BufferElement_Resource> societyResourceBuffer = AddBuffer<EM_BufferElement_Resource>(entity);
                AddSocietyResources(authoring.societyResources, ref societyResourceBuffer);
            }
        }
        #endregion
    }
}
