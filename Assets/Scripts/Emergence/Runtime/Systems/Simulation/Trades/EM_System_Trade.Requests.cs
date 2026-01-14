using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequests
        private void ProcessTradeRequest(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            ref EM_Blob_NpcScheduleEntry tradeEntry, ref EM_Component_RandomSeed seed, RefRO<EM_Component_NpcTradeInteraction> tradeInteraction,
            RefRO<EM_Component_NpcScheduleTarget> scheduleTarget, RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
            RefRO<EM_Component_NpcLocationState> locationState, RefRO<LocalTransform> transform, RefRW<EM_Component_NpcNavigationState> navigationState,
            RefRW<EM_Component_TradeRequestState> tradeRequest, DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders,
            DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            DynamicBuffer<EM_BufferElement_Resource> resources, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (scheduleTarget.ValueRO.ActivityId.Length == 0)
            {
                if (tradeRequest.ValueRO.Stage != EM_TradeRequestStage.None)
                    CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);

                return;
            }

            EM_ScheduleTradePolicy schedulePolicy = (EM_ScheduleTradePolicy)tradeEntry.TradePolicy;

            bool isOverride = scheduleTarget.ValueRO.IsOverride != 0;

            if (schedulePolicy == EM_ScheduleTradePolicy.BlockAll || (!isOverride && scheduleTarget.ValueRO.TradeCapable == 0))
            {
                if (tradeRequest.ValueRO.Stage != EM_TradeRequestStage.None)
                    CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);

                return;
            }

            IntentPolicy policy = ResolveIntentPolicy(tradeSettings);

            if (tradeRequest.ValueRO.Stage != EM_TradeRequestStage.None)
            {
                if (tradeRequest.ValueRO.IsOverrideRequest != 0 && scheduleTarget.ValueRO.IsOverride == 0)
                {
                    CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
                    return;
                }

                UpdateActiveTradeRequest(requester, societyRoot, timeSeconds, tradeSettings, policy, ref tradeEntry, ref seed, tradeInteraction,
                    scheduleTarget, scheduleOverride, locationState, transform, navigationState, tradeRequest, attemptedProviders, intents, needs,
                    settings, resources, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
                return;
            }

            TryStartTradeRequest(requester, societyRoot, timeSeconds, tradeSettings, policy, ref tradeEntry, ref seed, tradeInteraction,
                scheduleTarget, scheduleOverride, navigationState, tradeRequest, attemptedProviders, intents, needs, settings, resources, signals,
                candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
        }
        #endregion
    }
}
