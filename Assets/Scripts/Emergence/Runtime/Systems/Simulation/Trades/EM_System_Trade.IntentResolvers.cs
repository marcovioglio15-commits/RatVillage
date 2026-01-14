using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region IntentResolvers
        private static float TryResolveWithSociety(Entity requester, Entity societyRoot, NeedResolutionData needData,
            DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_Resource> requesterResources, BufferLookup<EM_BufferElement_Resource> resourceLookup,
            DynamicBuffer<EM_BufferElement_SignalEvent> requesterSignals, EM_Component_TradeSettings settings, double timeSeconds, float requestAmount,
            IntentPolicy policy,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (societyRoot == Entity.Null)
                return 0f;

            if (!resourceLookup.HasBuffer(societyRoot))
                return 0f;

            if (requestAmount <= 0f)
                return 0f;

            DynamicBuffer<EM_BufferElement_Resource> societyResources = resourceLookup[societyRoot];
            float available = GetResourceAmount(societyResources, needData.ResourceId);

            if (available <= 0f)
                return 0f;

            float desiredAmount = requestAmount;

            if (policy.ClampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                    return 0f;

                desiredAmount = math.min(desiredAmount, remainingNeedAmount);
            }

            float transferAmount = math.min(available, desiredAmount);

            if (transferAmount <= 0f)
                return 0f;

            if (!policy.ConsumeOnResolve)
                ApplyResourceDelta(requesterResources, needData.ResourceId, transferAmount);

            ApplyResourceDelta(societyResources, needData.ResourceId, -transferAmount);
            ApplyNeedDelta(needs, needData.NeedId, -transferAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);

            EmitTradeSignal(requesterSignals, settings.TradeSuccessSignalId, needData.NeedId, requester, societyRoot, societyRoot, timeSeconds, transferAmount,
                hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);

            if (hasDebugBuffer)
            {
                EM_Component_Event successEvent = EM_Utility_LogEvent.BuildInteractionEvent(EM_DebugEventType.InteractionSuccess, ReasonSociety,
                    requester, societyRoot, societyRoot, needData.NeedId, needData.ResourceId, transferAmount, timeSeconds);
                EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, successEvent);
            }

            return transferAmount;
        }

        private static float TryResolveWithInventory(DynamicBuffer<EM_BufferElement_Resource> requesterResources, DynamicBuffer<EM_BufferElement_Need> needs,
            NeedResolutionData needData, float requestAmount, bool clampTransferToNeed)
        {
            if (requestAmount <= 0f)
                return 0f;

            if (needData.ResourceId.Length == 0)
                return 0f;

            float available = GetResourceAmount(requesterResources, needData.ResourceId);

            if (available <= 0f)
                return 0f;

            float desiredAmount = requestAmount;

            if (clampTransferToNeed)
            {
                float remainingNeedAmount = ResolveRemainingNeedAmount(needs, needData);

                if (remainingNeedAmount <= 0f)
                    return 0f;

                desiredAmount = math.min(desiredAmount, remainingNeedAmount);
            }

            float transferAmount = math.min(available, desiredAmount);

            if (transferAmount <= 0f)
                return 0f;

            ApplyResourceDelta(requesterResources, needData.ResourceId, -transferAmount);
            ApplyNeedDelta(needs, needData.NeedId, -transferAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);
            return transferAmount;
        }
        #endregion
    }
}
