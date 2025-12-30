using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region RelationshipHelpers
        // Relationship affinity resolution with type fallback.
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

        // Relationship affinity updates with type baseline.
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

            EM_BufferElement_Relationship newEntry = new EM_BufferElement_Relationship
            {
                Other = target,
                Affinity = math.clamp(baseAffinity + delta, -1f, 1f)
            };

            relationships.Add(newEntry);
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
