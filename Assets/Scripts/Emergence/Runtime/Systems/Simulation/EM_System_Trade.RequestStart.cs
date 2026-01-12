using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestStart
        private void TryStartTradeRequest(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
            IntentPolicy policy, ref EM_Blob_NpcScheduleEntry tradeEntry, ref EM_Component_RandomSeed seed,
            RefRO<EM_Component_NpcTradeInteraction> tradeInteraction, RefRO<EM_Component_NpcScheduleTarget> scheduleTarget,
            RefRW<EM_Component_NpcNavigationState> navigationState, RefRW<EM_Component_TradeRequestState> tradeRequest,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, DynamicBuffer<EM_BufferElement_Intent> intents,
            DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            DynamicBuffer<EM_BufferElement_Resource> resources, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (intents.Length == 0)
                return;

            PruneIntents(intents, needs, settings, policy.MinUrgencyToKeep);

            int intentIndex;
            EM_BufferElement_Intent intent;
            bool hasIntent = SelectBestIntent(intents, timeSeconds, policy.MinUrgency, ref tradeEntry, out intentIndex, out intent);

            if (!hasIntent)
                return;

            NeedResolutionData needData;
            bool hasNeedData = ResolveNeedData(intent, settings, out needData);

            if (!hasNeedData)
            {
                if (hasDebugBuffer)
                {
                    EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoResource,
                        requester, Entity.Null, societyRoot, intent.NeedId, intent.ResourceId, 0f, timeSeconds);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
                }

                RemoveIntentAt(intentIndex, intents);
                return;
            }

            float urgency = ResolveNeedUrgency(needs, needData);

            if (urgency < policy.MinUrgencyToKeep)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            intent.Urgency = urgency;

            if (intent.ResourceId.Length == 0 && needData.ResourceId.Length > 0)
                intent.ResourceId = needData.ResourceId;

            if (intent.DesiredAmount <= 0f && needData.RequestAmount > 0f)
                intent.DesiredAmount = needData.RequestAmount;

            intents[intentIndex] = intent;

            float remainingAmount = intent.DesiredAmount > 0f ? intent.DesiredAmount : needData.RequestAmount;

            if (remainingAmount <= 0f)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            if (policy.ClampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                {
                    RemoveIntentAt(intentIndex, intents);
                    return;
                }

                remainingAmount = math.min(remainingAmount, remainingNeedAmount);
            }

            if (remainingAmount <= 0f)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            if (policy.ConsumeInventoryFirst)
            {
                float inventoryResolved = TryResolveWithInventory(resources, needs, needData, remainingAmount, policy.ClampTransferToNeed);

                if (inventoryResolved > 0f)
                {
                    remainingAmount -= inventoryResolved;

                    if (hasDebugBuffer)
                    {
                        EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, default,
                            requester, requester, societyRoot, needData.NeedId, needData.ResourceId, inventoryResolved, timeSeconds);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, successEvent);
                    }
                }
            }

            if (remainingAmount <= 0f)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            if (policy.ClampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                {
                    RemoveIntentAt(intentIndex, intents);
                    return;
                }

                remainingAmount = math.min(remainingAmount, remainingNeedAmount);
            }

            float societyResolved = TryResolveWithSociety(requester, societyRoot, needData, needs, resources, resourceLookup, signals,
                tradeSettings, timeSeconds, remainingAmount, policy, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

            if (societyResolved > 0f)
                remainingAmount -= societyResolved;

            if (remainingAmount <= 0f)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            if (policy.ClampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                {
                    RemoveIntentAt(intentIndex, intents);
                    return;
                }

                remainingAmount = math.min(remainingAmount, remainingNeedAmount);
            }

            attemptedProviders.Clear();

            Entity provider;
            Entity providerAnchor;
            bool found = TrySelectProvider(requester, societyRoot, needData.ResourceId, attemptedProviders, candidates, candidateSocieties,
                ref providerLock, policy, out provider, out providerAnchor);

            if (!found)
            {
                EmitTradeSignal(signals, tradeSettings.TradeFailSignalId, needData.NeedId, requester, Entity.Null, societyRoot, timeSeconds, 0f,
                    hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                if (hasDebugBuffer)
                {
                    EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoPartner,
                        requester, Entity.Null, societyRoot, needData.NeedId, needData.ResourceId, 0f, timeSeconds);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, failEvent);
                }

                urgency = ResolveNeedUrgency(needs, needData);

                if (urgency < policy.MinUrgencyToKeep)
                {
                    RemoveIntentAt(intentIndex, intents);
                    return;
                }

                UpdateIntentAfterFailure(intentIndex, intents, remainingAmount, urgency, policy, timeSeconds, ref seed);
                return;
            }

            tradeRequest.ValueRW.IntentId = intent.IntentId;
            tradeRequest.ValueRW.NeedId = needData.NeedId;
            tradeRequest.ValueRW.ResourceId = needData.ResourceId;
            tradeRequest.ValueRW.DesiredAmount = remainingAmount;
            tradeRequest.ValueRW.Urgency = urgency;
            tradeRequest.ValueRW.Provider = provider;
            tradeRequest.ValueRW.TargetAnchor = providerAnchor;
            tradeRequest.ValueRW.StartTimeSeconds = timeSeconds;
            tradeRequest.ValueRW.WaitStartTimeSeconds = -1d;
            tradeRequest.ValueRW.QueueSlotIndex = -1;
            tradeRequest.ValueRW.IsOverrideRequest = scheduleTarget.ValueRO.IsOverride;
            tradeRequest.ValueRW.Stage = EM_TradeRequestStage.Traveling;

            SetTradeMeetingDestination(navigationState, providerAnchor);

            if (policy.LockProviderPerTick)
                providerLock.Add(provider);
        }
        #endregion
    }
}
