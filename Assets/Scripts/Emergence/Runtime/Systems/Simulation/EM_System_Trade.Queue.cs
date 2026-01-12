using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Queue
        private bool TryGetQueueEntryIndex(DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue, Entity requester, out int index)
        {
            index = -1;

            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].Requester != requester)
                    continue;

                index = i;
                return true;
            }

            return false;
        }

        private static bool IsRequesterFirstInQueue(DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue, Entity requester)
        {
            if (queue.Length == 0)
                return false;

            return queue[0].Requester == requester;
        }

        private static int FindAvailableQueueSlot(DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue, int slotCount)
        {
            if (slotCount <= 0)
                return -1;

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                bool occupied = false;

                for (int i = 0; i < queue.Length; i++)
                {
                    if (queue[i].SlotIndex != slotIndex)
                        continue;

                    occupied = true;
                    break;
                }

                if (!occupied)
                    return slotIndex;
            }

            return -1;
        }

        private static bool RemoveRequesterFromQueue(DynamicBuffer<EM_BufferElement_TradeQueueEntry> queue, Entity requester)
        {
            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].Requester != requester)
                    continue;

                queue.RemoveAt(i);
                return true;
            }

            return false;
        }

        private bool TryGetQueueSlotPosition(Entity anchorEntity, int slotIndex, out float3 position)
        {
            position = float3.zero;

            if (anchorEntity == Entity.Null)
                return false;

            if (!locationAnchorLookup.HasComponent(anchorEntity))
                return false;

            if (!transformLookup.HasComponent(anchorEntity))
                return false;

            EM_Component_LocationAnchor anchor = locationAnchorLookup[anchorEntity];
            LocalTransform transform = transformLookup[anchorEntity];
            int slotCount = math.max(anchor.QueueSlotCount, 0);

            if (slotCount <= 0)
                return false;

            int resolvedSlot = slotIndex;

            if (resolvedSlot < 0 || resolvedSlot >= slotCount)
                resolvedSlot = 0;

            float radius = math.max(0f, anchor.QueueRadius);
            float angle = slotCount <= 1 ? 0f : math.PI * 2f * (float)resolvedSlot / slotCount;
            float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * radius;
            position = transform.Position + offset;
            return true;
        }
        #endregion
    }
}
