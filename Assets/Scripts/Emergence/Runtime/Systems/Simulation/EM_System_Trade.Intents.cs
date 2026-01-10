using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Constants
        // Reason keys used in interaction debug events.
        private static readonly FixedString64Bytes ReasonNoResource = new FixedString64Bytes("NoResource");
        private static readonly FixedString64Bytes ReasonNoPartner = new FixedString64Bytes("NoPartner");
        private static readonly FixedString64Bytes ReasonRejected = new FixedString64Bytes("Rejected");
        #endregion

        #region IntentResolution
        // Resolve the highest urgency intent for the requester entity.
        private static void TryResolveIntent(Entity requester, Entity societyRoot, EM_Component_TradeSettings settings, double timeSeconds,
            ref EM_Blob_NpcScheduleEntry tradeEntry, ref EM_Component_RandomSeed seed, DynamicBuffer<EM_BufferElement_Intent> intents,
            DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_NeedSetting> needSettings, DynamicBuffer<EM_BufferElement_Resource> requesterResources,
            DynamicBuffer<EM_BufferElement_SignalEvent> requesterSignals, ref BufferLookup<EM_BufferElement_Resource> resourceLookup,
            ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup, ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ref ComponentLookup<EM_Component_NpcType> npcTypeLookup, ref ComponentLookup<EM_Component_NpcTradePreferences> tradePreferencesLookup,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            IntentPolicy policy = ResolveIntentPolicy(settings);

            if (intents.Length == 0)
                return;

            PruneIntents(intents, needs, needSettings, policy.MinUrgencyToKeep);

            int intentIndex;
            EM_BufferElement_Intent intent;
            bool hasIntent = SelectBestIntent(intents, timeSeconds, policy.MinUrgency, ref tradeEntry, out intentIndex, out intent);

            if (!hasIntent)
                return;

            NeedResolutionData needData;
            bool hasNeedData = ResolveNeedData(intent, needSettings, out needData);

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

            if (hasDebugBuffer)
            {
                EM_Component_Event attemptEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionAttempt, default,
                    requester, Entity.Null, societyRoot, needData.NeedId, needData.ResourceId, intent.Urgency, timeSeconds);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, attemptEvent);
            }

            if (policy.ConsumeInventoryFirst)
            {
                float inventoryResolved = TryResolveWithInventory(requesterResources, needs, needData, remainingAmount, policy.ClampTransferToNeed);

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

            float societyResolved = TryResolveWithSociety(requester, societyRoot, needData, needs, requesterResources, resourceLookup,
                requesterSignals, settings, timeSeconds, remainingAmount, policy, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

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

            FixedList128Bytes<Entity> rejectedProviders = default;
            int maxProviderAttempts = policy.MaxProviderAttemptsPerTick;

            if (maxProviderAttempts < 1)
                maxProviderAttempts = 1;

            int providerAttempts = 0;
            bool attemptedProvider = false;

            while (providerAttempts < maxProviderAttempts)
            {
                Entity provider;
                float affinity;
                float availableAmount;
                bool found = FindBestProvider(requester, societyRoot, needData.ResourceId, candidates, candidateSocieties, resourceLookup,
                    relationshipLookup, relationshipTypeLookup, npcTypeLookup, providerLock, policy.LockProviderPerTick, ref rejectedProviders,
                    out provider, out affinity, out availableAmount);

                if (!found)
                    break;

                attemptedProvider = true;

                if (provider == requester || provider.Index == requester.Index)
                {
                    if (rejectedProviders.Length < MaxProviderAttemptsPerTickCap)
                        rejectedProviders.Add(provider);

                    providerAttempts++;
                    continue;
                }

                float acceptance = math.saturate(settings.BaseAcceptance + affinity * settings.AffinityWeight);
                float acceptanceRoll = NextRandom01(ref seed);

                if (acceptanceRoll > acceptance)
                {
                    EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, 0f,
                        hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                    if (hasDebugBuffer)
                    {
                        EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonRejected,
                            requester, provider, societyRoot, needData.NeedId, needData.ResourceId, 0f, timeSeconds);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, failEvent);
                    }

                    if (rejectedProviders.Length < MaxProviderAttemptsPerTickCap)
                        rejectedProviders.Add(provider);

                    providerAttempts++;
                    continue;
                }

                float affinity01 = math.saturate((affinity + 1f) * 0.5f);
                float multiplier = SampleAffinityMultiplier(provider, affinity01, ref tradePreferencesLookup);
                float desiredAmount = remainingAmount * multiplier;
                float transferAmount = math.min(availableAmount, desiredAmount);

                if (policy.ClampTransferToNeed)
                    transferAmount = math.min(transferAmount, remainingAmount);

                if (transferAmount <= 0f)
                {
                    EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, 0f,
                        hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                    if (hasDebugBuffer)
                    {
                        EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoResource,
                            requester, provider, societyRoot, needData.NeedId, needData.ResourceId, 0f, timeSeconds);
                        EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, failEvent);
                    }

                    if (rejectedProviders.Length < MaxProviderAttemptsPerTickCap)
                        rejectedProviders.Add(provider);

                    providerAttempts++;
                    continue;
                }

                if (!policy.ConsumeOnResolve)
                    ApplyResourceDelta(requesterResources, needData.ResourceId, transferAmount);

                ApplyResourceDelta(resourceLookup[provider], needData.ResourceId, -transferAmount);
                ApplyNeedDelta(needs, needData.NeedId, -transferAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);

                EmitTradeSignal(requesterSignals, settings.TradeSuccessSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, transferAmount,
                    hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                if (hasDebugBuffer)
                {
                    EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, default,
                        requester, provider, societyRoot, needData.NeedId, needData.ResourceId, transferAmount, timeSeconds);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, successEvent);
                }

                if (policy.LockProviderPerTick)
                    providerLock.Add(provider);

                remainingAmount -= transferAmount;
                urgency = ResolveNeedUrgency(needs, needData);

                if (remainingAmount <= 0f || urgency < policy.MinUrgencyToKeep)
                {
                    RemoveIntentAt(intentIndex, intents);
                    return;
                }

                UpdateIntentAfterProgress(intentIndex, intents, remainingAmount, timeSeconds, urgency);
                return;
            }

            if (!attemptedProvider)
            {
                EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, Entity.Null, societyRoot, timeSeconds, 0f,
                    hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

                if (hasDebugBuffer)
                {
                    EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoPartner,
                        requester, Entity.Null, societyRoot, needData.NeedId, needData.ResourceId, 0f, timeSeconds);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, failEvent);
                }
            }

            urgency = ResolveNeedUrgency(needs, needData);

            if (urgency < policy.MinUrgencyToKeep)
            {
                RemoveIntentAt(intentIndex, intents);
                return;
            }

            UpdateIntentAfterFailure(intentIndex, intents, remainingAmount, urgency, policy, timeSeconds, ref seed);
        }
        #endregion
    }
}
