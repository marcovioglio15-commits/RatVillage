using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Providers
        private enum EM_TradeAttemptResult : byte
        {
            None = 0,
            Success = 1,
            Rejected = 2,
            NoResource = 3
        }

        private bool TrySelectProvider(Entity requester, Entity societyRoot, FixedString64Bytes resourceId,
            DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders, NativeList<Entity> candidates,
            NativeList<Entity> candidateSocieties, ref NativeParallelHashSet<Entity> providerLock, IntentPolicy policy,
            out Entity provider, out Entity providerAnchor)
        {
            provider = Entity.Null;
            providerAnchor = Entity.Null;

            FixedList128Bytes<Entity> excludedProviders = default;
            BuildExcludedProviders(attemptedProviders, ref excludedProviders);

            int maxAttempts = policy.MaxProviderAttemptsPerTick;

            if (maxAttempts < 1)
                maxAttempts = 1;

            int attempts = 0;

            while (attempts < maxAttempts)
            {
                Entity candidate;
                float affinity;
                float availableAmount;
                bool found = FindBestProvider(requester, societyRoot, resourceId, candidates, candidateSocieties, resourceLookup,
                    relationshipLookup, relationshipTypeLookup, npcTypeLookup, providerLock, policy.LockProviderPerTick, ref excludedProviders,
                    out candidate, out affinity, out availableAmount);

                if (!found)
                    return false;

                Entity anchorEntity;
                bool hasAnchor = TryResolveProviderAnchor(candidate, out anchorEntity);

                if (!hasAnchor)
                {
                    if (excludedProviders.Length < MaxProviderAttemptsPerTickCap)
                        excludedProviders.Add(candidate);

                    attempts++;
                    continue;
                }

                provider = candidate;
                providerAnchor = anchorEntity;
                return true;
            }

            return false;
        }

        private void BuildExcludedProviders(DynamicBuffer<EM_BufferElement_TradeAttemptedProvider> attemptedProviders,
            ref FixedList128Bytes<Entity> excludedProviders)
        {
            if (attemptedProviders.Length == 0)
                return;

            for (int i = 0; i < attemptedProviders.Length; i++)
            {
                if (excludedProviders.Length >= MaxProviderAttemptsPerTickCap)
                    return;

                excludedProviders.Add(attemptedProviders[i].Provider);
            }
        }

        private bool TryResolveProviderAnchor(Entity provider, out Entity anchorEntity)
        {
            anchorEntity = Entity.Null;

            if (!locationStateLookup.HasComponent(provider))
                return false;

            EM_Component_NpcLocationState locationState = locationStateLookup[provider];

            if (locationState.LastTradeAnchor != Entity.Null)
            {
                anchorEntity = locationState.LastTradeAnchor;
                return true;
            }

            if (!scheduleTargetLookup.HasComponent(provider))
                return false;

            EM_Component_NpcScheduleTarget scheduleTarget = scheduleTargetLookup[provider];

            if (scheduleTarget.TradeCapable == 0)
                return false;

            if (scheduleTarget.LocationId.Length == 0)
                return false;

            if (locationState.CurrentLocationAnchor == Entity.Null)
                return false;

            if (!locationState.CurrentLocationId.Equals(scheduleTarget.LocationId))
                return false;

            anchorEntity = locationState.CurrentLocationAnchor;
            return true;
        }

        private EM_TradeAttemptResult TryExecuteTrade(Entity requester, Entity provider, Entity societyRoot, double timeSeconds,
            float remainingAmount, NeedResolutionData needData, IntentPolicy policy, EM_Component_TradeSettings tradeSettings,
            DynamicBuffer<EM_BufferElement_Resource> requesterResources, DynamicBuffer<EM_BufferElement_Need> needs,
            ref EM_Component_RandomSeed seed, out float transferAmount)
        {
            transferAmount = 0f;

            if (!resourceLookup.HasBuffer(provider))
                return EM_TradeAttemptResult.NoResource;

            DynamicBuffer<EM_BufferElement_Resource> providerResources = resourceLookup[provider];
            float availableAmount = GetResourceAmount(providerResources, needData.ResourceId);

            if (availableAmount <= 0f)
                return EM_TradeAttemptResult.NoResource;

            float affinity = GetAffinity(provider, requester, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);
            float acceptance = math.saturate(tradeSettings.BaseAcceptance + affinity * tradeSettings.AffinityWeight);
            float acceptanceRoll = NextRandom01(ref seed);

            if (acceptanceRoll > acceptance)
                return EM_TradeAttemptResult.Rejected;

            float affinity01 = math.saturate((affinity + 1f) * 0.5f);
            float multiplier = SampleAffinityMultiplier(provider, affinity01, ref tradePreferencesLookup);
            float desiredAmount = remainingAmount * multiplier;
            float resolvedAmount = math.min(availableAmount, desiredAmount);

            if (policy.ClampTransferToNeed)
                resolvedAmount = math.min(resolvedAmount, remainingAmount);

            if (resolvedAmount <= 0f)
                return EM_TradeAttemptResult.NoResource;

            if (!policy.ConsumeOnResolve)
                ApplyResourceDelta(requesterResources, needData.ResourceId, resolvedAmount);

            ApplyResourceDelta(providerResources, needData.ResourceId, -resolvedAmount);
            ApplyNeedDelta(needs, needData.NeedId, -resolvedAmount * needData.NeedSatisfactionPerUnit, needData.MinValue, needData.MaxValue);

            transferAmount = resolvedAmount;
            return EM_TradeAttemptResult.Success;
        }
        #endregion
    }
}
