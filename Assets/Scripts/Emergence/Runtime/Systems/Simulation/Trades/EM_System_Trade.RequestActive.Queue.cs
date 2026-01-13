using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestQueue
        private void UpdateQueuedRequest(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Component_RandomSeed seed, RefRO<EM_Component_NpcTradeInteraction> tradeInteraction,
            RefRO<EM_Component_NpcLocationState> locationState, RefRW<EM_Component_NpcNavigationState> navigationState,
            RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, DynamicBuffer<EM_BufferElement_Intent> intents,
            DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_Resource> resources, NeedResolutionData needData,
            float remainingAmount, DynamicBuffer<EM_BufferElement_SignalEvent> signals, NativeList<Entity> candidates, NativeList<Entity> candidateSocieties,
            ref NativeParallelHashSet<Entity> providerLock, bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries,
            ref EM_Component_Log debugLog)
        {
            Entity provider = tradeRequest.ValueRO.Provider;
            Entity targetAnchor = tradeRequest.ValueRO.TargetAnchor;

            if (provider == Entity.Null || targetAnchor == Entity.Null)
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderMissing, scheduleOverride, navigationState);
                return;
            }

            FixedString64Bytes targetLocationId;
            bool hasTargetLocation = TryGetAnchorLocationId(targetAnchor, out targetLocationId);

            if (!hasTargetLocation || !IsProviderAtLocation(provider, targetLocationId))
            {
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderMissing, scheduleOverride, navigationState);
                return;
            }

            DynamicBuffer<EM_BufferElement_TradeQueueEntry> providerQueue = tradeQueueLookup[provider];
            int queueIndex;
            bool queued = TryGetQueueEntryIndex(providerQueue, requester, out queueIndex);

            if (!queued)
            {
                bool enqueued = TryEnterQueue(requester, timeSeconds, tradeInteraction, tradeRequest, navigationState);

                if (!enqueued)
                {
                    HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                        needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                        maxEntries, ref debugLog, ReasonQueueFull, scheduleOverride, navigationState);
                }

                return;
            }

            if (tradeRequest.ValueRO.WaitStartTimeSeconds >= 0d &&
                tradeInteraction.ValueRO.WaitSeconds > 0f &&
                timeSeconds - tradeRequest.ValueRO.WaitStartTimeSeconds >= tradeInteraction.ValueRO.WaitSeconds)
            {
                RemoveRequesterFromQueue(providerQueue, requester);
                HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                    needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                    ref debugLog, ReasonProviderTimeout, scheduleOverride, navigationState);
                return;
            }

            int slotIndex = providerQueue[queueIndex].SlotIndex;
            int slotNodeIndex;
            bool hasSlotNodeIndex = TryResolveQueueSlotNodeIndex(targetAnchor, slotIndex, out slotNodeIndex);

            if (hasSlotNodeIndex)
            {
                tradeRequest.ValueRW.QueueSlotNodeIndex = slotNodeIndex;
                float reservationSeconds = ResolveQueueReservationTimeout(tradeInteraction.ValueRO);
                TryReserveQueueSlot(requester, slotNodeIndex, timeSeconds, reservationSeconds);
            }
            else
            {
                tradeRequest.ValueRW.QueueSlotNodeIndex = -1;
            }

            float3 queuePosition;
            bool hasQueuePosition = TryGetQueueSlotPosition(targetAnchor, slotIndex, out queuePosition);

            if (hasQueuePosition)
                SetTradeQueueDestination(navigationState, queuePosition);

            if (!IsRequesterFirstInQueue(providerQueue, requester))
                return;

            if (IsProviderBusy(provider, requester, timeSeconds))
                return;

            TryHandleTradeAttempt(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents, needs,
                resources, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                maxEntries, ref debugLog, scheduleOverride, navigationState, false);
        }

        private bool TryEnterQueue(Entity requester, double timeSeconds, RefRO<EM_Component_NpcTradeInteraction> tradeInteraction,
            RefRW<EM_Component_TradeRequestState> tradeRequest, RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            Entity provider = tradeRequest.ValueRO.Provider;
            Entity targetAnchor = tradeRequest.ValueRO.TargetAnchor;

            if (provider == Entity.Null || targetAnchor == Entity.Null)
                return false;

            if (!tradeQueueLookup.HasBuffer(provider))
                return false;

            if (!locationAnchorLookup.HasComponent(targetAnchor))
                return false;

            DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue = tradeQueueLookup[provider];
            int queueIndex;
            bool queued = TryGetQueueEntryIndex(queue, requester, out queueIndex);
            int slotIndex;

            if (queued)
            {
                slotIndex = queue[queueIndex].SlotIndex;
            }
            else
            {
                EM_Component_LocationAnchor anchor = locationAnchorLookup[targetAnchor];
                int slotCount = math.max(anchor.QueueSlotCount, 0);
                slotIndex = FindAvailableQueueSlot(queue, slotCount);

                if (slotIndex < 0)
                    return false;

                queue.Add(new EM_BufferElement_TradeQueueEntry
                {
                    Requester = requester,
                    EnqueueTimeSeconds = timeSeconds,
                    SlotIndex = slotIndex
                });
            }

            tradeRequest.ValueRW.Stage = EM_TradeRequestStage.Queued;
            tradeRequest.ValueRW.QueueSlotIndex = slotIndex;

            if (tradeRequest.ValueRO.WaitStartTimeSeconds < 0d)
                tradeRequest.ValueRW.WaitStartTimeSeconds = timeSeconds;

            int slotNodeIndex;
            bool hasSlotNodeIndex = TryResolveQueueSlotNodeIndex(targetAnchor, slotIndex, out slotNodeIndex);

            if (hasSlotNodeIndex)
            {
                tradeRequest.ValueRW.QueueSlotNodeIndex = slotNodeIndex;
                float reservationSeconds = ResolveQueueReservationTimeout(tradeInteraction.ValueRO);
                TryReserveQueueSlot(requester, slotNodeIndex, timeSeconds, reservationSeconds);
            }
            else
            {
                tradeRequest.ValueRW.QueueSlotNodeIndex = -1;
            }

            float3 queuePosition;
            bool hasQueuePosition = TryGetQueueSlotPosition(targetAnchor, slotIndex, out queuePosition);

            if (hasQueuePosition)
                SetTradeQueueDestination(navigationState, queuePosition);

            return true;
        }
        #endregion
    }
}
