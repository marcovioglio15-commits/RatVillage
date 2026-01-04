using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Relationships
        private static bool FindBestProvider(Entity requester, Entity societyRoot, FixedString64Bytes resourceId,
            NativeList<Entity> candidates, NativeList<Entity> candidateSocieties, BufferLookup<EM_BufferElement_Resource> resourceLookup,
            BufferLookup<EM_BufferElement_Relationship> relationshipLookup, BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ComponentLookup<EM_Component_NpcType> npcTypeLookup, out Entity provider, out float affinity, out float availableAmount)
        {
            provider = Entity.Null;
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

                float currentAffinity = GetAffinity(candidate, requester, ref relationshipLookup, ref relationshipTypeLookup, ref npcTypeLookup);

                if (currentAffinity <= bestAffinity)
                    continue;

                bestAffinity = currentAffinity;
                provider = candidate;
                affinity = currentAffinity;
                availableAmount = amount;
            }

            return provider != Entity.Null;
        }

        private static float GetAffinity(Entity source, Entity target, ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup,
            ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup, ref ComponentLookup<EM_Component_NpcType> npcTypeLookup)
        {
            if (relationshipLookup.HasBuffer(source))
            {
                DynamicBuffer<EM_BufferElement_Relationship> relationships = relationshipLookup[source];

                for (int i = 0; i < relationships.Length; i++)
                {
                    if (relationships[i].Other != target)
                        continue;

                    return relationships[i].Affinity;
                }
            }

            return GetTypeAffinity(source, target, ref relationshipTypeLookup, ref npcTypeLookup);
        }

        private static void ApplyAffinityDelta(Entity source, Entity target, float delta, ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup,
            ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup, ref ComponentLookup<EM_Component_NpcType> npcTypeLookup)
        {
            if (!relationshipLookup.HasBuffer(source))
                return;

            DynamicBuffer<EM_BufferElement_Relationship> relationships = relationshipLookup[source];

            for (int i = 0; i < relationships.Length; i++)
            {
                if (relationships[i].Other != target)
                    continue;

                EM_BufferElement_Relationship entry = relationships[i];
                entry.Affinity = math.clamp(entry.Affinity + delta, -1f, 1f);
                relationships[i] = entry;
                return;
            }

            float baseAffinity = GetTypeAffinity(source, target, ref relationshipTypeLookup, ref npcTypeLookup);

            relationships.Add(new EM_BufferElement_Relationship
            {
                Other = target,
                Affinity = math.clamp(baseAffinity + delta, -1f, 1f)
            });
        }

        private static float GetTypeAffinity(Entity source, Entity target, ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ref ComponentLookup<EM_Component_NpcType> npcTypeLookup)
        {
            if (!npcTypeLookup.HasComponent(target))
                return 0f;

            FixedString64Bytes typeId = npcTypeLookup[target].TypeId;

            if (typeId.Length == 0)
                return 0f;

            if (!relationshipTypeLookup.HasBuffer(source))
                return 0f;

            DynamicBuffer<EM_BufferElement_RelationshipType> typeRelationships = relationshipTypeLookup[source];

            for (int i = 0; i < typeRelationships.Length; i++)
            {
                if (!typeRelationships[i].TypeId.Equals(typeId))
                    continue;

                return typeRelationships[i].Affinity;
            }

            return 0f;
        }
        #endregion
    }
}
