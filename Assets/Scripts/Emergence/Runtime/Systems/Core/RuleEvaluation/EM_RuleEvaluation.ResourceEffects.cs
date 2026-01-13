using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Resource
        // Apply a delta to the resource buffer.
        private static bool ApplyResourceDelta(Entity target, FixedString64Bytes resourceId, float delta,
            ref BufferLookup<EM_BufferElement_Resource> resourceLookup, out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (resourceId.Length == 0)
                return false;

            if (!resourceLookup.HasBuffer(target))
                return false;

            DynamicBuffer<EM_BufferElement_Resource> resources = resourceLookup[target];

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EM_BufferElement_Resource entry = resources[i];
                before = entry.Amount;
                after = entry.Amount + delta;
                entry.Amount = after;
                resources[i] = entry;
                return true;
            }

            before = 0f;
            after = delta;
            resources.Add(new EM_BufferElement_Resource
            {
                ResourceId = resourceId,
                Amount = after
            });

            return true;
        }
        #endregion
    }
}
