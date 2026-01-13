using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestHelpers
        private void CancelTradeRequest(Entity requester, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            Entity provider = tradeRequest.ValueRO.Provider;

            if (provider != Entity.Null && tradeRequest.ValueRO.Stage == EM_TradeRequestStage.Queued)
            {
                if (tradeQueueLookup.HasBuffer(provider))
                {
                    DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue = tradeQueueLookup[provider];
                    RemoveRequesterFromQueue(queue, requester);
                }
            }

            ClearQueueSlotReservation(requester, tradeRequest.ValueRO.QueueSlotNodeIndex);
            attemptedProviders.Clear();
            ResetTradeRequest(tradeRequest);
            ClearTradeDestination(navigationState);
        }

        private static void ResetTradeRequest(RefRW<EM_Component_TradeRequestState> tradeRequest)
        {
            tradeRequest.ValueRW.IntentId = default;
            tradeRequest.ValueRW.NeedId = default;
            tradeRequest.ValueRW.ResourceId = default;
            tradeRequest.ValueRW.DesiredAmount = 0f;
            tradeRequest.ValueRW.Urgency = 0f;
            tradeRequest.ValueRW.Provider = Entity.Null;
            tradeRequest.ValueRW.TargetAnchor = Entity.Null;
            tradeRequest.ValueRW.StartTimeSeconds = -1d;
            tradeRequest.ValueRW.WaitStartTimeSeconds = -1d;
            tradeRequest.ValueRW.QueueSlotIndex = -1;
            tradeRequest.ValueRW.QueueSlotNodeIndex = -1;
            tradeRequest.ValueRW.IsOverrideRequest = 0;
            tradeRequest.ValueRW.Stage = EM_TradeRequestStage.None;
        }

        private static void EndOverride(RefRW<EM_Component_NpcScheduleOverride> scheduleOverride)
        {
            scheduleOverride.ValueRW.ActivityId = default;
            scheduleOverride.ValueRW.RemainingHours = 0f;
            scheduleOverride.ValueRW.DurationHours = 0f;
            scheduleOverride.ValueRW.EntryIndex = -1;
        }

        private static void AddAttemptedProvider(DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, Entity provider)
        {
            for (int i = 0; i < attemptedProviders.Length; i++)
            {
                if (attemptedProviders[i].Provider == provider)
                    return;
            }

            attemptedProviders.Add(new EM_BufferElement_TradeAttemptedProvider
            {
                Provider = provider
            });
        }

        private void AbortTradeRequest(Entity requester, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, RefRW<EM_Component_NpcNavigationState> navigationState,
            DynamicBuffer<EM_BufferElement_Intent> intents)
        {
            int intentIndex;
            EM_BufferElement_Intent intent;
            bool hasIntent = TryFindIntentIndex(intents, tradeRequest.ValueRO, out intentIndex, out intent);

            if (hasIntent)
                RemoveIntentAt(intentIndex, intents);

            CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
        }

        private void HandleTradeSuccess(Entity requester, Entity societyRoot, double timeSeconds, IntentPolicy policy,
            RefRW<EM_Component_TradeRequestState> tradeRequest, DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders,
            DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs, NeedResolutionData needData,
            float remainingAmount, float transferAmount, RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
            RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            float updatedRemaining = math.max(0f, remainingAmount - transferAmount);
            float urgency = ResolveNeedUrgency(needs, needData);

            int intentIndex;
            EM_BufferElement_Intent intent;
            bool hasIntent = TryFindIntentIndex(intents, tradeRequest.ValueRO, out intentIndex, out intent);

            if (hasIntent)
            {
                if (updatedRemaining <= 0f || urgency < policy.MinUrgencyToKeep)
                {
                    RemoveIntentAt(intentIndex, intents);
                }
                else
                {
                    UpdateIntentAfterProgress(intentIndex, intents, updatedRemaining, timeSeconds, urgency);
                }
            }

            if (tradeRequest.ValueRO.IsOverrideRequest != 0)
                EndOverride(scheduleOverride);

            CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
        }

        private void HandleProviderFailure(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Component_RandomSeed seed, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, DynamicBuffer<EM_BufferElement_Intent> intents,
            DynamicBuffer<EM_BufferElement_Need> needs, NeedResolutionData needData, float remainingAmount,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, NativeList<Entity> candidates, NativeList<Entity> candidateSocieties,
            ref NativeParallelHashSet<Entity> providerLock, bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries,
            ref EM_Component_Log debugLog, FixedString64Bytes reason, RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
            RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            Entity provider = tradeRequest.ValueRO.Provider;

            EmitTradeSignal(signals, tradeSettings.TradeFailSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, 0f,
                hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

            if (hasDebugBuffer)
            {
                EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, reason,
                    requester, provider, societyRoot, needData.NeedId, needData.ResourceId, 0f, timeSeconds);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, failEvent);
            }

            if (tradeQueueLookup.HasBuffer(provider))
            {
                DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue = tradeQueueLookup[provider];
                RemoveRequesterFromQueue(queue, requester);
            }

            if (provider != Entity.Null)
                AddAttemptedProvider(attemptedProviders, provider);

            Entity nextProvider;
            Entity nextAnchor;
            bool found = TrySelectProvider(requester, societyRoot, needData.ResourceId, attemptedProviders, candidates, candidateSocieties,
                ref providerLock, policy, out nextProvider, out nextAnchor);

            if (found)
            {
                ClearQueueSlotReservation(requester, tradeRequest.ValueRO.QueueSlotNodeIndex);
                tradeRequest.ValueRW.QueueSlotNodeIndex = -1;
                tradeRequest.ValueRW.Provider = nextProvider;
                tradeRequest.ValueRW.TargetAnchor = nextAnchor;
                tradeRequest.ValueRW.StartTimeSeconds = timeSeconds;
                tradeRequest.ValueRW.WaitStartTimeSeconds = -1d;
                tradeRequest.ValueRW.QueueSlotIndex = -1;
                tradeRequest.ValueRW.Stage = EM_TradeRequestStage.Traveling;

                SetTradeMeetingDestination(navigationState, nextAnchor);

                if (policy.LockProviderPerTick)
                    providerLock.Add(nextProvider);

                return;
            }

            float urgency = ResolveNeedUrgency(needs, needData);

            if (urgency < policy.MinUrgencyToKeep)
            {
                int intentIndex;
                EM_BufferElement_Intent intent;
                bool hasIntent = TryFindIntentIndex(intents, tradeRequest.ValueRO, out intentIndex, out intent);

                if (hasIntent)
                    RemoveIntentAt(intentIndex, intents);
            }
            else
            {
                int intentIndex;
                EM_BufferElement_Intent intent;
                bool hasIntent = TryFindIntentIndex(intents, tradeRequest.ValueRO, out intentIndex, out intent);

                if (hasIntent)
                    UpdateIntentAfterFailure(intentIndex, intents, remainingAmount, urgency, policy, timeSeconds, ref seed);
            }

            if (tradeRequest.ValueRO.IsOverrideRequest != 0)
                EndOverride(scheduleOverride);

            CancelTradeRequest(requester, tradeRequest, attemptedProviders, navigationState);
        }

        private bool TryGetAnchorLocationId(Entity anchorEntity, out FixedString64Bytes locationId)
        {
            locationId = default;

            if (anchorEntity == Entity.Null)
                return false;

            if (!locationAnchorLookup.HasComponent(anchorEntity))
                return false;

            locationId = locationAnchorLookup[anchorEntity].LocationId;
            return locationId.Length > 0;
        }

        private bool IsProviderAtLocation(Entity provider, FixedString64Bytes locationId)
        {
            if (locationId.Length == 0)
                return false;

            if (!locationStateLookup.HasComponent(provider))
                return false;

            EM_Component_NpcLocationState providerLocation = locationStateLookup[provider];
            return providerLocation.CurrentLocationId.Equals(locationId);
        }

        private bool IsProviderBusy(Entity provider, Entity requester, double timeSeconds)
        {
            if (!tradeProviderLookup.HasComponent(provider))
                return false;

            EM_Component_TradeProviderState providerState = tradeProviderLookup[provider];

            if (providerState.BusyUntilSeconds <= 0d || timeSeconds >= providerState.BusyUntilSeconds)
            {
                if (providerState.ActiveRequester != Entity.Null)
                {
                    providerState.ActiveRequester = Entity.Null;
                    tradeProviderLookup[provider] = providerState;
                }

                return false;
            }

            if (providerState.ActiveRequester == Entity.Null)
                return false;

            return providerState.ActiveRequester != requester;
        }

        private void SetProviderBusy(Entity provider, Entity requester, EM_Component_TradeSettings tradeSettings, double timeSeconds)
        {
            if (!tradeProviderLookup.HasComponent(provider))
                return;

            EM_Component_TradeProviderState providerState = tradeProviderLookup[provider];
            providerState.ActiveRequester = requester;
            double lockSeconds = GetIntervalSeconds(tradeSettings.TradeTickIntervalHours);

            if (lockSeconds <= 0d)
                lockSeconds = 1d;

            providerState.BusyUntilSeconds = timeSeconds + lockSeconds;
            tradeProviderLookup[provider] = providerState;
        }

        private bool TryPauseProviderNavigation(Entity provider, out EM_Component_NpcNavigationState previousState)
        {
            previousState = default;

            if (!navigationLookup.HasComponent(provider))
                return false;

            previousState = navigationLookup[provider];
            EM_Component_NpcNavigationState paused = previousState;
            paused.DestinationKind = EM_NpcDestinationKind.None;
            paused.DestinationAnchor = Entity.Null;
            paused.DestinationPosition = float3.zero;
            paused.DestinationNodeIndex = -1;
            paused.PathIndex = 0;
            paused.HasPath = 0;
            navigationLookup[provider] = paused;
            return true;
        }

        private void RestoreProviderNavigation(Entity provider, EM_Component_NpcNavigationState previousState)
        {
            if (!navigationLookup.HasComponent(provider))
                return;

            navigationLookup[provider] = previousState;
        }
        #endregion
    }
}
