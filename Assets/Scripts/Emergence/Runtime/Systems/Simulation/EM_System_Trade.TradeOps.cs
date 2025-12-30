using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade : ISystem
    {
        #region Fields

        #region Constants
        private static readonly FixedString64Bytes ReasonNoPartner = new FixedString64Bytes("NoPartner");
        private static readonly FixedString64Bytes ReasonRejected = new FixedString64Bytes("Rejected");
        private static readonly FixedString64Bytes ReasonNoResource = new FixedString64Bytes("NoResource");
        private static readonly FixedString64Bytes ReasonSuccess = new FixedString64Bytes("Success");
        #endregion

        #endregion

        #region Methods

        #region Helpers
        private static bool TryResolveTrade(Entity requester, Entity societyRoot, EM_BufferElement_NeedRule rule, EM_Component_TradeSettings settings,
            ref EM_Component_RandomSeed seed, DynamicBuffer<EM_BufferElement_Need> needs, ref BufferLookup<EM_BufferElement_Resource> resourceLookup,
            ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup, ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ref ComponentLookup<EM_Component_NpcType> npcTypeLookup, ref BufferLookup<EM_BufferElement_SignalEvent> signalLookup,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, out Entity partner, out FixedString64Bytes reason, out float transferAmount)
        {
            partner = Entity.Null;
            reason = default;
            transferAmount = 0f;
            float affinity;
            float availableAmount;
            bool found = FindBestPartner(requester, societyRoot, rule.ResourceId, candidates, candidateSocieties, resourceLookup, relationshipLookup,
                relationshipTypeLookup, npcTypeLookup, out partner, out affinity, out availableAmount);

            if (!found)
            {
                reason = ReasonNoPartner;
                EmitTradeSignal(requester, societyRoot, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            float acceptance = math.saturate(settings.BaseAcceptance + affinity * settings.AffinityWeight);
            float acceptanceRoll = NextRandom01(ref seed);

            if (acceptanceRoll > acceptance)
            {
                reason = ReasonRejected;
                ApplyAffinityDelta(requester, partner, settings.AffinityChangeOnFail, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);
                ApplyAffinityDelta(partner, requester, settings.AffinityChangeOnFail, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);
                EmitTradeSignal(requester, societyRoot, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            float requestedAmount = rule.ResourceTransferAmount;

            if (requestedAmount <= 0f)
                requestedAmount = availableAmount;

            float actualTransfer = math.min(availableAmount, requestedAmount);

            if (actualTransfer <= 0f)
            {
                reason = ReasonNoResource;
                EmitTradeSignal(requester, societyRoot, settings.TradeFailSignalId, ref signalLookup);
                return false;
            }

            ApplyResourceDelta(requester, rule.ResourceId, actualTransfer, ref resourceLookup);
            ApplyResourceDelta(partner, rule.ResourceId, -actualTransfer, ref resourceLookup);

            float satisfaction = rule.NeedSatisfactionAmount;

            if (satisfaction <= 0f)
                satisfaction = actualTransfer;

            ApplyNeedDelta(needs, rule.NeedId, -satisfaction, rule.MinValue, rule.MaxValue);
            ApplyAffinityDelta(requester, partner, settings.AffinityChangeOnSuccess, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);
            ApplyAffinityDelta(partner, requester, settings.AffinityChangeOnSuccess, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);
            EmitTradeSignal(requester, societyRoot, settings.TradeSuccessSignalId, ref signalLookup);
            reason = ReasonSuccess;
            transferAmount = actualTransfer;

            return true;
        }

        private static bool FindBestPartner(Entity requester, Entity societyRoot, FixedString64Bytes resourceId, NativeList<Entity> candidates,
            NativeList<Entity> candidateSocieties, BufferLookup<EM_BufferElement_Resource> resourceLookup, BufferLookup<EM_BufferElement_Relationship> relationshipLookup,
            BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup, ComponentLookup<EM_Component_NpcType> npcTypeLookup,
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

                DynamicBuffer<EM_BufferElement_Resource> resources = resourceLookup[candidate];
                float amount = GetResourceAmount(resources, resourceId);

                if (amount <= 0f)
                    continue;

                float currentAffinity = GetAffinity(requester, candidate, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);

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

        private static float GetResourceAmount(DynamicBuffer<EM_BufferElement_Resource> resources, FixedString64Bytes resourceId)
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

        private static void ApplyResourceDelta(Entity target, FixedString64Bytes resourceId, float delta, ref BufferLookup<EM_BufferElement_Resource> resourceLookup)
        {
            if (!resourceLookup.HasBuffer(target))
                return;

            if (resourceId.Length == 0)
                return;

            DynamicBuffer<EM_BufferElement_Resource> resources = resourceLookup[target];

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EM_BufferElement_Resource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            EM_BufferElement_Resource newEntry = new EM_BufferElement_Resource
            {
                ResourceId = resourceId,
                Amount = delta
            };

            resources.Add(newEntry);
        }

        private static void ApplyNeedDelta(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId, float delta, float minValue, float maxValue)
        {
            if (needId.Length == 0)
                return;

            float minClamp = math.min(minValue, maxValue);
            float maxClamp = math.max(minValue, maxValue);

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EM_BufferElement_Need entry = needs[i];
                entry.Value = math.clamp(entry.Value + delta, minClamp, maxClamp);
                needs[i] = entry;
                return;
            }

            EM_BufferElement_Need newEntry = new EM_BufferElement_Need
            {
                NeedId = needId,
                Value = math.clamp(delta, minClamp, maxClamp)
            };

            needs.Add(newEntry);
        }

        private static void EmitTradeSignal(Entity requester, Entity societyRoot, FixedString64Bytes signalId,
            ref BufferLookup<EM_BufferElement_SignalEvent> signalLookup)
        {
            if (signalId.Length == 0)
                return;

            if (!signalLookup.HasBuffer(requester))
                return;

            DynamicBuffer<EM_BufferElement_SignalEvent> signals = signalLookup[requester];

            EM_BufferElement_SignalEvent signalEvent = new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = 1f,
                Subject = requester,
                SocietyRoot = societyRoot,
                Time = 0d
            };

            signals.Add(signalEvent);
        }

        private static float NextRandom01(ref EM_Component_RandomSeed seed)
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

        #endregion
    }
}
