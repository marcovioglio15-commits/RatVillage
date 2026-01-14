using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestTravel
        private const float TravelTimeoutMultiplier = 3f;

        private void UpdateTravelingRequest(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Component_RandomSeed seed, RefRO<EM_Component_NpcTradeInteraction> tradeInteraction,
            RefRO<EM_Component_NpcLocationState> locationState, RefRO<LocalTransform> transform,
            RefRW<EM_Component_NpcNavigationState> navigationState, RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
            RefRW<EM_Component_TradeRequestState> tradeRequest, DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders,
            DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_Resource> resources, NeedResolutionData needData, float remainingAmount,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, NativeList<Entity> candidates, NativeList<Entity> candidateSocieties,
            ref NativeParallelHashSet<Entity> providerLock, bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries,
            ref EM_Component_Log debugLog)
        {
            Entity provider = tradeRequest.ValueRO.Provider;
            Entity targetAnchor = tradeRequest.ValueRO.TargetAnchor;

            if (targetAnchor == Entity.Null)
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderMissing, scheduleOverride, navigationState);
                return;
            }

            if (navigationState.ValueRO.DestinationKind != EM_NpcDestinationKind.TradeMeeting ||
                navigationState.ValueRO.DestinationAnchor != targetAnchor)
                SetTradeMeetingDestination(navigationState, targetAnchor);

            if (TryHandleMidwayTrade(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeInteraction, locationState, transform,
                navigationState, scheduleOverride, tradeRequest, attemptedProviders, intents, needs, resources, needData, remainingAmount, signals,
                candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog))
                return;

            FixedString64Bytes targetLocationId;
            bool hasTargetLocation = TryGetAnchorLocationId(targetAnchor, out targetLocationId);

            if (!hasTargetLocation)
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderMissing, scheduleOverride, navigationState);
                return;
            }

            if (!locationState.ValueRO.CurrentLocationId.Equals(targetLocationId))
            {
                if (IsTravelTimedOut(tradeRequest.ValueRO, navigationState.ValueRO, tradeInteraction.ValueRO, timeSeconds))
                {
                    HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders,
                        intents, needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer,
                        debugBuffer, maxEntries, ref debugLog, ReasonProviderTimeout, scheduleOverride, navigationState);
                }

                return;
            }

            if (!IsProviderAtLocation(provider, targetLocationId))
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderMissing, scheduleOverride, navigationState);
                return;
            }

            if (IsProviderBusy(provider, requester, timeSeconds))
            {
                bool queued = TryEnterQueue(requester, timeSeconds, tradeInteraction, tradeRequest, navigationState);

                if (!queued)
                {
                    HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                        needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                        maxEntries, ref debugLog, ReasonQueueFull, scheduleOverride, navigationState);
                }

                return;
            }

            DynamicBuffer<EM_BufferElement_TradeQueueEntry> providerQueue = tradeQueueLookup[provider];

            if (providerQueue.Length > 0 && !IsRequesterFirstInQueue(providerQueue, requester))
            {
                bool queued = TryEnterQueue(requester, timeSeconds, tradeInteraction, tradeRequest, navigationState);

                if (!queued)
                {
                    HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                        needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                        maxEntries, ref debugLog, ReasonQueueFull, scheduleOverride, navigationState);
                }

                return;
            }

            TryHandleTradeAttempt(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents, needs,
                resources, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                ref debugLog, scheduleOverride, navigationState, false);
        }

        private static bool IsTravelTimedOut(EM_Component_TradeRequestState tradeRequest, EM_Component_NpcNavigationState navigationState,
            EM_Component_NpcTradeInteraction tradeInteraction, double timeSeconds)
        {
            if (tradeRequest.StartTimeSeconds < 0d)
                return false;

            if (tradeInteraction.WaitSeconds <= 0f)
                return false;

            if (navigationState.IsMoving != 0)
                return false;

            double elapsedSeconds = timeSeconds - tradeRequest.StartTimeSeconds;
            float timeoutSeconds = math.max(tradeInteraction.WaitSeconds, 0f) * TravelTimeoutMultiplier;

            if (timeoutSeconds <= 0f)
                return false;

            return elapsedSeconds >= timeoutSeconds;
        }
        #endregion
    }
}
