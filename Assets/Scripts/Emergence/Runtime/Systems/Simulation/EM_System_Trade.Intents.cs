using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Constants
        // Reason keys used in interaction debug events.
        private static readonly FixedString64Bytes ReasonNoIntent = new FixedString64Bytes("NoIntent");
        private static readonly FixedString64Bytes ReasonNoResource = new FixedString64Bytes("NoResource");
        private static readonly FixedString64Bytes ReasonNoPartner = new FixedString64Bytes("NoPartner");
        private static readonly FixedString64Bytes ReasonRejected = new FixedString64Bytes("Rejected");
        #endregion

        #region IntentResolution
        // Resolve the highest urgency intent for the requester entity.
        private static void TryResolveIntent(Entity requester, Entity societyRoot, EM_Component_TradeSettings settings, double timeSeconds,
            ref EM_Component_RandomSeed seed, DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_NeedSetting> needSettings, DynamicBuffer<EM_BufferElement_Resource> requesterResources,
            DynamicBuffer<EM_BufferElement_SignalEvent> requesterSignals, ref BufferLookup<EM_BufferElement_Resource> resourceLookup,
            ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup, ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ref ComponentLookup<EM_Component_NpcType> npcTypeLookup, ref ComponentLookup<EM_Component_NpcTradePreferences> tradePreferencesLookup,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            int intentIndex;
            EM_BufferElement_Intent intent;
            bool hasIntent = SelectBestIntent(intents, timeSeconds, out intentIndex, out intent);

            if (!hasIntent)
            {
                if (hasDebugBuffer)
                {
                    EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoIntent,
                        requester, Entity.Null, societyRoot, default, default, 0f);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
                }

                return;
            }

            NeedResolutionData needData;
            bool hasNeedData = ResolveNeedData(intent, needSettings, out needData);

            if (!hasNeedData)
            {
                if (hasDebugBuffer)
                {
                    EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoResource,
                        requester, Entity.Null, societyRoot, intent.NeedId, intent.ResourceId, 0f);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
                }

                return;
            }

            if (hasDebugBuffer)
            {
                EM_Component_Event attemptEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionAttempt, default,
                    requester, Entity.Null, societyRoot, needData.NeedId, needData.ResourceId, intent.Urgency);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, attemptEvent);
            }

            if (TryResolveWithSociety(requester, societyRoot, intentIndex, intents, needData, needs, requesterResources, resourceLookup,
                requesterSignals, settings, timeSeconds, hasDebugBuffer, debugBuffer, maxEntries))
                return;

            Entity provider;
            float affinity;
            float availableAmount;
            bool found = FindBestProvider(requester, societyRoot, needData.ResourceId, candidates, candidateSocieties, resourceLookup,
                relationshipLookup, relationshipTypeLookup, npcTypeLookup, out provider, out affinity, out availableAmount);

            if (!found)
            {
                EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, Entity.Null, societyRoot, timeSeconds, 0f,
                    hasDebugBuffer, debugBuffer, maxEntries);

                if (hasDebugBuffer)
                {
                    EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoPartner,
                        requester, Entity.Null, societyRoot, needData.NeedId, needData.ResourceId, 0f);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, failEvent);
                }

                return;
            }

            float acceptance = math.saturate(settings.BaseAcceptance + affinity * settings.AffinityWeight);
            float acceptanceRoll = NextRandom01(ref seed);

            if (acceptanceRoll > acceptance)
            {
                EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, 0f,
                    hasDebugBuffer, debugBuffer, maxEntries);

                if (hasDebugBuffer)
                {
                    EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonRejected,
                        requester, provider, societyRoot, needData.NeedId, needData.ResourceId, 0f);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, failEvent);
                }

                return;
            }

            float affinity01 = math.saturate((affinity + 1f) * 0.5f);
            float multiplier = SampleAffinityMultiplier(provider, affinity01, ref tradePreferencesLookup);
            float baseRequest = needData.RequestAmount > 0f ? needData.RequestAmount : availableAmount;
            float desiredAmount = baseRequest * multiplier;
            float transferAmount = math.min(availableAmount, desiredAmount);

            if (transferAmount <= 0f)
            {
                EmitTradeSignal(requesterSignals, settings.TradeFailSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, 0f,
                    hasDebugBuffer, debugBuffer, maxEntries);

                if (hasDebugBuffer)
                {
                    EM_Component_Event failEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionFail, ReasonNoResource,
                        requester, provider, societyRoot, needData.NeedId, needData.ResourceId, 0f);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, failEvent);
                }

                return;
            }

            ApplyResourceDelta(requesterResources, needData.ResourceId, transferAmount);
            ApplyResourceDelta(resourceLookup[provider], needData.ResourceId, -transferAmount);
            ApplyNeedDelta(needs, needData.NeedId, -transferAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);

            EmitTradeSignal(requesterSignals, settings.TradeSuccessSignalId, needData.NeedId, requester, provider, societyRoot, timeSeconds, transferAmount,
                hasDebugBuffer, debugBuffer, maxEntries);

            if (hasDebugBuffer)
            {
                EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, default,
                    requester, provider, societyRoot, needData.NeedId, needData.ResourceId, transferAmount);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, successEvent);
            }

            intents.RemoveAt(intentIndex);
        }

        // Resolve an intent using the shared society resource pool.
        private static bool TryResolveWithSociety(Entity requester, Entity societyRoot, int intentIndex, DynamicBuffer<EM_BufferElement_Intent> intents,
            NeedResolutionData needData, DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_Resource> requesterResources, BufferLookup<EM_BufferElement_Resource> resourceLookup,
            DynamicBuffer<EM_BufferElement_SignalEvent> requesterSignals, EM_Component_TradeSettings settings, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            if (societyRoot == Entity.Null)
                return false;

            if (!resourceLookup.HasBuffer(societyRoot))
                return false;

            DynamicBuffer<EM_BufferElement_Resource> societyResources = resourceLookup[societyRoot];
            float available = GetResourceAmount(societyResources, needData.ResourceId);

            if (available <= 0f)
                return false;

            float baseRequest = needData.RequestAmount > 0f ? needData.RequestAmount : available;
            float transferAmount = math.min(available, baseRequest);

            if (transferAmount <= 0f)
                return false;

            ApplyResourceDelta(requesterResources, needData.ResourceId, transferAmount);
            ApplyResourceDelta(societyResources, needData.ResourceId, -transferAmount);
            ApplyNeedDelta(needs, needData.NeedId, -transferAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);

            EmitTradeSignal(requesterSignals, settings.TradeSuccessSignalId, needData.NeedId, requester, societyRoot, societyRoot, timeSeconds, transferAmount,
                hasDebugBuffer, debugBuffer, maxEntries);

            if (hasDebugBuffer)
            {
                EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, default,
                    requester, societyRoot, societyRoot, needData.NeedId, needData.ResourceId, transferAmount);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, successEvent);
            }

            intents.RemoveAt(intentIndex);
            return true;
        }

        // Select the highest urgency intent that is ready to be attempted.
        private static bool SelectBestIntent(DynamicBuffer<EM_BufferElement_Intent> intents, double time,
            out int intentIndex, out EM_BufferElement_Intent intent)
        {
            intentIndex = -1;
            intent = default;
            float bestUrgency = 0f;

            for (int i = 0; i < intents.Length; i++)
            {
                EM_BufferElement_Intent current = intents[i];

                if (time < current.NextAttemptTime)
                    continue;

                if (current.Urgency <= bestUrgency)
                    continue;

                bestUrgency = current.Urgency;
                intentIndex = i;
                intent = current;
            }

            return intentIndex >= 0;
        }

        // Resolve intent settings using the requester's need configuration.
        private static bool ResolveNeedData(EM_BufferElement_Intent intent, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            out NeedResolutionData data)
        {
            data = default;
            FixedString64Bytes needId = intent.NeedId;
            FixedString64Bytes resourceId = intent.ResourceId;

            if (needId.Length == 0 && resourceId.Length == 0)
                return false;

            for (int i = 0; i < settings.Length; i++)
            {
                if (needId.Length > 0 && !settings[i].NeedId.Equals(needId))
                    continue;

                data.NeedId = settings[i].NeedId;
                data.ResourceId = resourceId.Length > 0 ? resourceId : settings[i].ResourceId;
                data.MinValue = math.min(settings[i].MinValue, settings[i].MaxValue);
                data.MaxValue = math.max(settings[i].MinValue, settings[i].MaxValue);
                data.RequestAmount = intent.DesiredAmount > 0f ? intent.DesiredAmount : settings[i].RequestAmount;
                data.NeedSatisfactionPerUnit = settings[i].NeedSatisfactionPerUnit > 0f ? settings[i].NeedSatisfactionPerUnit : 1f;

                return data.ResourceId.Length > 0;
            }

            if (resourceId.Length == 0)
                return false;

            data.NeedId = needId;
            data.ResourceId = resourceId;
            data.MinValue = 0f;
            data.MaxValue = 1f;
            data.RequestAmount = intent.DesiredAmount;
            data.NeedSatisfactionPerUnit = 1f;
            return true;
        }
        #endregion

        #region Data
        private struct NeedResolutionData
        {
            public FixedString64Bytes NeedId;
            public FixedString64Bytes ResourceId;
            public float MinValue;
            public float MaxValue;
            public float RequestAmount;
            public float NeedSatisfactionPerUnit;
        }
        #endregion
    }
}
