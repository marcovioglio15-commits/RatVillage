using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Relationships
        private static bool ApplyRelationshipDelta(Entity source, Entity other, float delta,
            ref BufferLookup<EM_BufferElement_Relationship> relationshipLookup,
            ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup,
            ref ComponentLookup<EM_Component_NpcType> npcTypeLookup, out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (source == Entity.Null || other == Entity.Null)
                return false;

            if (!relationshipLookup.HasBuffer(source))
                return false;

            DynamicBuffer<EM_BufferElement_Relationship> relationships = relationshipLookup[source];

            for (int i = 0; i < relationships.Length; i++)
            {
                if (relationships[i].Other != other)
                    continue;

                EM_BufferElement_Relationship entry = relationships[i];
                before = entry.Affinity;
                after = math.clamp(entry.Affinity + delta, -1f, 1f);
                entry.Affinity = after;
                relationships[i] = entry;
                return true;
            }

            float baseAffinity = GetTypeAffinity(source, other, ref relationshipTypeLookup, ref npcTypeLookup);
            before = baseAffinity;
            after = math.clamp(baseAffinity + delta, -1f, 1f);

            relationships.Add(new EM_BufferElement_Relationship
            {
                Other = other,
                Affinity = after
            });

            return true;
        }

        private static float GetTypeAffinity(Entity source, Entity target,
            ref BufferLookup<EM_BufferElement_RelationshipType> relationshipTypeLookup, ref ComponentLookup<EM_Component_NpcType> npcTypeLookup)
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
