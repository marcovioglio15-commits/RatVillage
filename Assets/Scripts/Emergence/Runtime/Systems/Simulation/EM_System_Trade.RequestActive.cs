using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestActive
        private void UpdateActiveTradeRequest(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Blob_NpcScheduleEntry tradeEntry, ref EM_Component_RandomSeed seed,
            RefRO<EM_Component_NpcTradeInteraction> tradeInteraction, RefRO<EM_Component_NpcScheduleTarget> scheduleTarget,
            RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, RefRO<EM_Component_NpcLocationState> locationState,
            RefRO<LocalTransform> transform, RefRW<EM_Component_NpcNavigationState> navigationState,
            RefRW<EM_Component_TradeRequestState> tradeRequest, DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders,
            DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            DynamicBuffer<EM_BufferElement_Resource> resources, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            Entity provider = tradeRequest.ValueRO.Provider;

            if (provider == Entity.Null)
            {
                CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
                return;
            }

            EM_BufferElement_Intent requestIntent = new EM_BufferElement_Intent
            {
                IntentId = tradeRequest.ValueRO.IntentId,
                NeedId = tradeRequest.ValueRO.NeedId,
                ResourceId = tradeRequest.ValueRO.ResourceId,
                DesiredAmount = tradeRequest.ValueRO.DesiredAmount
            };

            NeedResolutionData needData;
            bool hasNeedData = ResolveNeedData(requestIntent, settings, out needData);

            if (!hasNeedData)
            {
                AbortTradeRequest(requester, tradeRequest, attemptedProviders, navigationState, intents);
                return;
            }

            float urgency = ResolveNeedUrgency(needs, needData);

            if (urgency < policy.MinUrgencyToKeep)
            {
                AbortTradeRequest(requester, tradeRequest, attemptedProviders, navigationState, intents);
                return;
            }

            float remainingAmount = tradeRequest.ValueRO.DesiredAmount > 0f ? tradeRequest.ValueRO.DesiredAmount : needData.RequestAmount;

            if (policy.ClampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                {
                    AbortTradeRequest(requester, tradeRequest, attemptedProviders, navigationState, intents);
                    return;
                }

                remainingAmount = math.min(remainingAmount, remainingNeedAmount);
            }

            if (remainingAmount <= 0f)
            {
                AbortTradeRequest(requester, tradeRequest, attemptedProviders, navigationState, intents);
                return;
            }

            tradeRequest.ValueRW.Urgency = urgency;
            tradeRequest.ValueRW.DesiredAmount = remainingAmount;

            if (provider == requester || provider.Index == requester.Index)
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                    maxEntries, ref debugLog, ReasonNoPartner, scheduleOverride, navigationState);
                return;
            }

            if (tradeRequest.ValueRO.Stage == EM_TradeRequestStage.Traveling)
            {
                UpdateTravelingRequest(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeInteraction, locationState, transform,
                    navigationState, scheduleOverride, tradeRequest, attemptedProviders, intents, needs, resources, needData, remainingAmount, signals,
                    candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
                return;
            }

            if (tradeRequest.ValueRO.Stage == EM_TradeRequestStage.Queued)
            {
                UpdateQueuedRequest(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeInteraction, locationState,
                    navigationState, scheduleOverride, tradeRequest, attemptedProviders, intents, needs, resources, needData, remainingAmount, signals,
                    candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
                return;
            }

            CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
        }

        #endregion
    }
}
