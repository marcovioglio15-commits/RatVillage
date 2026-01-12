using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeRequestMidway
        private bool TryHandleMidwayTrade(Entity requester, Entity societyRoot, double timeSeconds, EM_Component_TradeSettings tradeSettings,
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

            if (provider == Entity.Null)
                return false;

            if (!transformLookup.HasComponent(provider))
                return false;

            LocalTransform providerTransform = transformLookup[provider];
            float distance = math.distance(transform.ValueRO.Position, providerTransform.Position);

            if (distance > tradeInteraction.ValueRO.InteractionDistance)
                return false;

            if (!CanMidwayTrade(provider, tradeInteraction, locationState))
                return false;

            if (IsProviderBusy(provider, requester, timeSeconds))
                return false;

            TryHandleTradeAttempt(requester, societyRoot, timeSeconds, tradeSettings, policy, ref seed, tradeRequest, attemptedProviders, intents, needs,
                resources, needData, remainingAmount, signals, candidates, candidateSocieties, ref providerLock, hasDebugBuffer, debugBuffer,
                maxEntries, ref debugLog, scheduleOverride, navigationState, true);
            return true;
        }

        private bool CanMidwayTrade(Entity provider, RefRO<EM_Component_NpcTradeInteraction> tradeInteraction,
            RefRO<EM_Component_NpcLocationState> requesterLocation)
        {
            if (!scheduleTargetLookup.HasComponent(provider))
                return false;

            EM_Component_NpcScheduleTarget providerTarget = scheduleTargetLookup[provider];

            if (providerTarget.TradeCapable == 0)
                return false;

            if (tradeInteraction.ValueRO.AllowMidwayTradeAnywhere != 0)
                return true;

            if (!locationStateLookup.HasComponent(provider))
                return false;

            EM_Component_NpcLocationState providerLocation = locationStateLookup[provider];

            if (providerLocation.CurrentLocationId.Length == 0)
                return false;

            if (!providerLocation.CurrentLocationId.Equals(requesterLocation.ValueRO.CurrentLocationId))
                return false;

            if (!providerTarget.LocationId.Equals(providerLocation.CurrentLocationId))
                return false;

            return true;
        }
        #endregion
    }
}
