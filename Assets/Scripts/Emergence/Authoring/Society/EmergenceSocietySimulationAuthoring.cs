using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Authoring component that configures society simulation settings.
    /// </summary>
    public sealed partial class EmergenceSocietySimulationAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct ResourceEntry
        {
            [Tooltip("Resource identifier stored by the society.")]
            public string ResourceId;

            [Tooltip("Initial amount stored in the society pool.")]
            public float Amount;
        }

        #region Serialized
        // Serialized clock
        #region Serialized - Clock
        [Tooltip("Length of a full simulated day in seconds.")]
        [Header("Clock")]
        [SerializeField] private float dayLengthSeconds = 120f;

        [Tooltip("Initial time of day in hours (0-24). Used to align schedules at startup.")]
        [SerializeField] private float startTimeOfDay = 6f;
        #endregion

        // Serialized schedule
        #region Serialized - Schedule
        [Tooltip("Start hour for the Sleep schedule window (daily activity category). NPCs interpret this window as sleep time and systems emit the Sleep window signal when it begins.")]
        [Header("Schedule")]
        [SerializeField] private float sleepStartHour = 22f;

        [Tooltip("End hour for the Sleep window. Windows can wrap over midnight (e.g., 22 -> 6).")]
        [SerializeField] private float sleepEndHour = 6f;

        [Tooltip("Start hour for the Work schedule window (daily activity category). Use this to drive work-related rules and behaviors.")]
        [SerializeField] private float workStartHour = 8f;

        [Tooltip("End hour for the Work window. Work is active between WorkStartHour and WorkEndHour unless it wraps.")]
        [SerializeField] private float workEndHour = 17f;

        [Tooltip("Interval in hours for schedule tick signals. Each tick is scaled by the schedule curves and broadcast to NPCs to drive time-of-day behavior.")]
        [SerializeField] private float tickIntervalHours = 1f;

        [Tooltip("Signal emitted when the Sleep window starts. Use this schedule category signal to trigger rule sets or animation states.")]
        [SerializeField] private string sleepSignalId = "Time.SleepWindow";

        [Tooltip("Signal emitted when the Work window starts. Use this schedule category signal to drive work-related rules.")]
        [SerializeField] private string workSignalId = "Time.WorkWindow";

        [Tooltip("Signal emitted when the Leisure window starts. Leisure covers all hours outside Sleep and Work windows.")]
        [SerializeField] private string leisureSignalId = "Time.LeisureWindow";

        [Tooltip("Signal emitted each tick during the Sleep window. Value is scaled by the Sleep curve to shape the intensity over time.")]
        [SerializeField] private string sleepTickSignalId = "Time.SleepTick";

        [Tooltip("Signal emitted each tick during the Work window. Value is scaled by the Work curve to shape intensity over the shift.")]
        [SerializeField] private string workTickSignalId = "Time.WorkTick";

        [Tooltip("Signal emitted each tick during the Leisure window. Value is scaled by the Leisure curve for evening/morning tuning.")]
        [SerializeField] private string leisureTickSignalId = "Time.LeisureTick";

        [Tooltip("Sample count used to bake schedule curves. Higher values are smoother but use more memory.")]
        [Header("Schedule Curves")]
        [SerializeField] private int scheduleCurveSamples = 32;

        [Tooltip("Curve applied across the sleep window (0-1 time). Value scales tick signal magnitude.")]
        [SerializeField] private AnimationCurve sleepCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));

        [Tooltip("Curve applied across the work window (0-1 time). Value scales tick signal magnitude.")]
        [SerializeField] private AnimationCurve workCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Curve applied across the leisure window (0-1 time). Value scales tick signal magnitude.")]
        [SerializeField] private AnimationCurve leisureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.6f);
        #endregion

        // Serialized society resources
        #region Serialized - Society Resources
        [Tooltip("Initial society resource pool. Used by distribution before NPC trade.")]
        [Header("Society Resources")]
        [SerializeField] private ResourceEntry[] societyResources = new ResourceEntry[0];
        #endregion

        // Serialized needs
        #region Serialized - Needs
        [Tooltip("Tick rate in Hz for need decay updates.")]
        [Header("Needs")]
        [SerializeField] private float needTickRate = 1f;
        #endregion

        // Serialized trade
        #region Serialized - Trade
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

        // Serialized distribution
        #region Serialized - Resource Distribution
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

        #region Baker
        /// <summary>
        /// Bakes society simulation settings into ECS components.
        /// </summary>
        public sealed class Baker : Baker<EmergenceSocietySimulationAuthoring>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EmergenceSocietySimulationAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                EmergenceSocietyClock clock = new EmergenceSocietyClock
                {
                    DayLengthSeconds = authoring.dayLengthSeconds,
                    TimeOfDay = Mathf.Clamp(authoring.startTimeOfDay, 0f, 24f)
                };

                BlobAssetReference<EmergenceScheduleCurveBlob> curveBlob = BuildScheduleCurveBlob(authoring);
                AddBlobAsset(ref curveBlob, out Unity.Entities.Hash128 _);

                EmergenceSocietySchedule schedule = new EmergenceSocietySchedule
                {
                    SleepStartHour = authoring.sleepStartHour,
                    SleepEndHour = authoring.sleepEndHour,
                    WorkStartHour = authoring.workStartHour,
                    WorkEndHour = authoring.workEndHour,
                    TickIntervalHours = authoring.tickIntervalHours,
                    SleepSignalId = ToFixed(authoring.sleepSignalId),
                    WorkSignalId = ToFixed(authoring.workSignalId),
                    LeisureSignalId = ToFixed(authoring.leisureSignalId),
                    SleepTickSignalId = ToFixed(authoring.sleepTickSignalId),
                    WorkTickSignalId = ToFixed(authoring.workTickSignalId),
                    LeisureTickSignalId = ToFixed(authoring.leisureTickSignalId),
                    Curve = curveBlob
                };

                EmergenceSocietyScheduleState scheduleState = new EmergenceSocietyScheduleState
                {
                    CurrentWindow = -1,
                    TickAccumulatorHours = 0f
                };

                EmergenceNeedTickSettings needSettings = new EmergenceNeedTickSettings
                {
                    TickRate = authoring.needTickRate
                };

                EmergenceNeedTickState needState = new EmergenceNeedTickState
                {
                    NextTick = 0d
                };

                EmergenceTradeSettings tradeSettings = new EmergenceTradeSettings
                {
                    TradeTickRate = authoring.tradeTickRate,
                    BaseAcceptance = authoring.tradeBaseAcceptance,
                    AffinityWeight = authoring.tradeAffinityWeight,
                    AffinityChangeOnSuccess = authoring.affinityChangeOnSuccess,
                    AffinityChangeOnFail = authoring.affinityChangeOnFail,
                    TradeSuccessSignalId = ToFixed(authoring.tradeSuccessSignalId),
                    TradeFailSignalId = ToFixed(authoring.tradeFailSignalId)
                };

                EmergenceTradeTickState tradeState = new EmergenceTradeTickState
                {
                    NextTick = 0d
                };

                EmergenceSocietyResourceDistributionSettings distributionSettings = new EmergenceSocietyResourceDistributionSettings
                {
                    DistributionTickRate = authoring.distributionTickRate,
                    MaxTransfersPerMember = authoring.distributionMaxTransfersPerMember,
                    DefaultTransferAmount = authoring.distributionDefaultTransferAmount,
                    DefaultNeedSatisfaction = authoring.distributionDefaultNeedSatisfaction
                };

                EmergenceSocietyResourceDistributionState distributionState = new EmergenceSocietyResourceDistributionState
                {
                    NextTick = 0d
                };

                AddComponent(entity, clock);
                AddComponent(entity, schedule);
                AddComponent(entity, scheduleState);
                AddComponent(entity, needSettings);
                AddComponent(entity, needState);
                AddComponent(entity, tradeSettings);
                AddComponent(entity, tradeState);
                AddComponent(entity, distributionSettings);
                AddComponent(entity, distributionState);
                AddComponent<EmergenceSignalEmitter>(entity);
                AddBuffer<EmergenceSignalEvent>(entity);
                AddBuffer<EmergenceScheduleSignal>(entity);
                DynamicBuffer<EmergenceResource> societyResourceBuffer = AddBuffer<EmergenceResource>(entity);
                AddSocietyResources(authoring.societyResources, ref societyResourceBuffer);
            }
        }
        #endregion
    }
}
