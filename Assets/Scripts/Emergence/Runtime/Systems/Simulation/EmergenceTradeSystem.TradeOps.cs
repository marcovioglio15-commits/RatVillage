using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Trade execution helpers for the trade system.
    /// </summary>
    public partial struct EmergenceTradeSystem
    {
        #region Constants
        private static readonly FixedString64Bytes ReasonNoPartner = new FixedString64Bytes("NoPartner");
        private static readonly FixedString64Bytes ReasonRejected = new FixedString64Bytes("Rejected");
        private static readonly FixedString64Bytes ReasonNoResource = new FixedString64Bytes("NoResource");
        private static readonly FixedString64Bytes ReasonSuccess = new FixedString64Bytes("Success");
        #endregion

        #region Helpers
        private static bool TryResolveTrade(Entity requester, Entity societyRoot, EmergenceNeedRule rule, EmergenceTradeSettings settings,
            ref EmergenceRandomSeed seed, DynamicBuffer<EmergenceNeed> needs, ref BufferLookup<EmergenceResource> resourceLookup,
            ref BufferLookup<EmergenceRelationship> relationshipLookup, ref BufferLookup<EmergenceSignalEvent> signalLookup,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, out Entity partner, out FixedString64Bytes reason,
            out float transferAmount)
        {
            partner = Entity.Null;
            reason = default;
            transferAmount = 0f;
            float affinity;
            float availableAmount;
            bool found = FindBestPartner(requester, societyRoot, rule.ResourceId, candidates, candidateSocieties, resourceLookup, relationshipLookup,
                out partner, out affinity, out availableAmount);

            if (!found)
            {
                reason = ReasonNoPartner;
                EmitTradeSignal(requester, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            float acceptance = math.saturate(settings.BaseAcceptance + affinity * settings.AffinityWeight);
            float acceptanceRoll = NextRandom01(ref seed);

            if (acceptanceRoll > acceptance)
            {
                reason = ReasonRejected;
                ApplyAffinityDelta(requester, partner, settings.AffinityChangeOnFail, ref relationshipLookup);
                ApplyAffinityDelta(partner, requester, settings.AffinityChangeOnFail, ref relationshipLookup);
                EmitTradeSignal(requester, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            float requestedAmount = rule.ResourceTransferAmount;

            if (requestedAmount <= 0f)
                requestedAmount = availableAmount;

            float actualTransfer = math.min(availableAmount, requestedAmount);

            if (actualTransfer <= 0f)
            {
                reason = ReasonNoResource;
                EmitTradeSignal(requester, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            ApplyResourceDelta(requester, rule.ResourceId, actualTransfer, ref resourceLookup);
            ApplyResourceDelta(partner, rule.ResourceId, -actualTransfer, ref resourceLookup);

            float satisfaction = rule.NeedSatisfactionAmount;

            if (satisfaction <= 0f)
                satisfaction = actualTransfer;

            ApplyNeedDelta(needs, rule.NeedId, -satisfaction, rule.MinValue, rule.MaxValue);
            ApplyAffinityDelta(requester, partner, settings.AffinityChangeOnSuccess, ref relationshipLookup);
            ApplyAffinityDelta(partner, requester, settings.AffinityChangeOnSuccess, ref relationshipLookup);
            EmitTradeSignal(requester, settings.TradeSuccessSignalId, ref signalLookup);
            reason = ReasonSuccess;
            transferAmount = actualTransfer;

            return true;
        }

        private static bool FindBestPartner(Entity requester, Entity societyRoot, FixedString64Bytes resourceId, NativeList<Entity> candidates,
            NativeList<Entity> candidateSocieties, BufferLookup<EmergenceResource> resourceLookup, BufferLookup<EmergenceRelationship> relationshipLookup,
            out Entity partner, out float affinity, out float availableAmount)
        {
            partner = Entity.Null;
            affinity = 0f;
            availableAmount = 0f;
            float bestAffinity = float.MinValue;

            for (int i = 0; i < candidates.Length; i++)
            {
                Entity candidate = candidates[i];

                if (candidate == requester)
                    continue;

                if (candidateSocieties[i] != societyRoot)
                    continue;

                if (!resourceLookup.HasBuffer(candidate))
                    continue;

                DynamicBuffer<EmergenceResource> resources = resourceLookup[candidate];
                float amount = GetResourceAmount(resources, resourceId);

                if (amount <= 0f)
                    continue;

                float currentAffinity = GetAffinity(requester, candidate, ref relationshipLookup);

                if (currentAffinity <= bestAffinity)
                    continue;

                bestAffinity = currentAffinity;
                partner = candidate;
                affinity = currentAffinity;
                availableAmount = amount;
            }

            if (partner == Entity.Null)
                return false;

            return true;
        }

        private static float GetResourceAmount(DynamicBuffer<EmergenceResource> resources, FixedString64Bytes resourceId)
        {
            if (resourceId.Length == 0)
                return 0f;

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                return resources[i].Amount;
            }

            return 0f;
        }

        private static void ApplyResourceDelta(Entity target, FixedString64Bytes resourceId, float delta, ref BufferLookup<EmergenceResource> resourceLookup)
        {
            if (!resourceLookup.HasBuffer(target))
                return;

            if (resourceId.Length == 0)
                return;

            DynamicBuffer<EmergenceResource> resources = resourceLookup[target];

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EmergenceResource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            EmergenceResource newEntry = new EmergenceResource
            {
                ResourceId = resourceId,
                Amount = delta
            };

            resources.Add(newEntry);
        }

        private static void ApplyNeedDelta(DynamicBuffer<EmergenceNeed> needs, FixedString64Bytes needId, float delta, float minValue, float maxValue)
        {
            if (needId.Length == 0)
                return;

            float minClamp = math.min(minValue, maxValue);
            float maxClamp = math.max(minValue, maxValue);

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EmergenceNeed entry = needs[i];
                entry.Value = math.clamp(entry.Value + delta, minClamp, maxClamp);
                needs[i] = entry;
                return;
            }

            EmergenceNeed newEntry = new EmergenceNeed
            {
                NeedId = needId,
                Value = math.clamp(delta, minClamp, maxClamp)
            };

            needs.Add(newEntry);
        }

        private static float GetAffinity(Entity source, Entity target, ref BufferLookup<EmergenceRelationship> relationshipLookup)
        {
            if (!relationshipLookup.HasBuffer(source))
                return 0f;

            DynamicBuffer<EmergenceRelationship> relationships = relationshipLookup[source];

            for (int i = 0; i < relationships.Length; i++)
            {
                if (relationships[i].Other != target)
                    continue;

                return relationships[i].Affinity;
            }

            return 0f;
        }

        private static void ApplyAffinityDelta(Entity source, Entity target, float delta, ref BufferLookup<EmergenceRelationship> relationshipLookup)
        {
            if (!relationshipLookup.HasBuffer(source))
                return;

            DynamicBuffer<EmergenceRelationship> relationships = relationshipLookup[source];

            for (int i = 0; i < relationships.Length; i++)
            {
                if (relationships[i].Other != target)
                    continue;

                EmergenceRelationship entry = relationships[i];
                entry.Affinity = math.clamp(entry.Affinity + delta, -1f, 1f);
                relationships[i] = entry;
                return;
            }

            EmergenceRelationship newEntry = new EmergenceRelationship
            {
                Other = target,
                Affinity = math.clamp(delta, -1f, 1f)
            };

            relationships.Add(newEntry);
        }

        private static void EmitTradeSignal(Entity requester, FixedString64Bytes signalId, ref BufferLookup<EmergenceSignalEvent> signalLookup)
        {
            if (signalId.Length == 0)
                return;

            if (!signalLookup.HasBuffer(requester))
                return;

            DynamicBuffer<EmergenceSignalEvent> signals = signalLookup[requester];

            EmergenceSignalEvent signalEvent = new EmergenceSignalEvent
            {
                SignalId = signalId,
                Value = 1f,
                Target = requester,
                LodTier = EmergenceLodTier.Full,
                Time = 0d
            };

            signals.Add(signalEvent);
        }

        private static float NextRandom01(ref EmergenceRandomSeed seed)
        {
            uint current = seed.Value;

            if (current == 0)
                current = 1u;

            Random random = Random.CreateFromIndex(current);
            float value = random.NextFloat();
            seed.Value = random.NextUInt();

            return value;
        }
        #endregion
    }
}
