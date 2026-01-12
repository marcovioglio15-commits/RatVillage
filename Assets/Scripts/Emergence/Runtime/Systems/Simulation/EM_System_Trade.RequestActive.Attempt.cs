using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestAttempt
        private void TryHandleTradeAttempt(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Component_RandomSeed seed, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, DynamicBuffer<EM_BufferElement_Intent> intents,
            DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_Resource> resources, NeedResolutionData needData,
            float remainingAmount, DynamicBuffer<EM_BufferElement_SignalEvent> signals, NativeList<Entity> candidates,
            NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock, bool hasDebugBuffer,
            DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog,
            RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, RefRW<EM_Component_NpcNavigationState> navigationState, bool isMidway)
        {
            Entity provider = tradeRequest.ValueRO.Provider;

            if (tradeQueueLookup.HasBuffer(provider))
            {
                DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue = tradeQueueLookup[provider];
                RemoveRequesterFromQueue(queue, requester);
            }

            if (hasDebugBuffer)
            {
                EM_Component_Event attemptEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionAttempt, default,
                    requester, provider, societyRoot, needData.NeedId, needData.ResourceId, remainingAmount, timeSeconds);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, attemptEvent);
            }

            EM_Component_NpcNavigationState previousNavigation = default;
            bool paused = false;

            if (isMidway)
                paused = TryPauseProviderNavigation(provider, out previousNavigation);

            float transferAmount;
            EM_TradeAttemptResult result = TryExecuteTrade(requester, provider, societyRoot, timeSeconds, remainingAmount, needData, policy,
                tradeSettings, resources, needs, ref seed, out transferAmount);

            if (paused)
                RestoreProviderNavigation(provider, previousNavigation);

            SetProviderBusy(provider, requester, tradeSettings, timeSeconds);

            if (policy.LockProviderPerTick)
                providerLock.Add(provider);

            if (result == EM_TradeAttemptResult.Success)
            {
                EmitTradeSignal(signals, tradeSettings.TradeSuccessSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds,
                    transferAmount, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                if (hasDebugBuffer)
                {
                    EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, default,
                        requester, provider, societyRoot, needData.NeedId, needData.ResourceId, transferAmount, timeSeconds);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, successEvent);
                }

                HandleTradeSuccess(requester, societyRoot, timeSeconds, policy, tradeRequest, attemptedProviders, intents, needs, needData,
                    remainingAmount, transferAmount, scheduleOverride, navigationState);
                return;
            }

            FixedString64Bytes reason = result == EM_TradeAttemptResult.Rejected ? ReasonRejected : ReasonNoResource;

            HandleProviderFailure(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents,
                needs, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer, maxEntries,
                ref debugLog, reason, scheduleOverride, navigationState);
        }
        #endregion
    }
}
