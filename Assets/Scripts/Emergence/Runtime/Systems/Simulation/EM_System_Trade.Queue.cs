using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Queue
        private const float QueueReservationFallbackSeconds = 5f;

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

        private bool TryResolveQueueSlotNodeIndex(Entity anchorEntity, int slotIndex, out int nodeIndex)
        {
            nodeIndex = -1;

            float3 slotPosition;
            bool hasSlotPosition = TryGetQueueSlotPosition(anchorEntity, slotIndex, out slotPosition);

            if (!hasSlotPosition)
                return false;

            return EM_Utility_LocationGrid.TryGetNodeIndex(slotPosition, currentGrid, out nodeIndex);
        }

        private float ResolveQueueReservationTimeout(EM_Component_NpcTradeInteraction tradeInteraction)
        {
            if (tradeInteraction.WaitSeconds > 0f)
                return tradeInteraction.WaitSeconds;

            return QueueReservationFallbackSeconds;
        }

        private bool TryReserveQueueSlot(Entity requester, int nodeIndex, double timeSeconds, float reservationSeconds)
        {
            if (nodeIndex < 0)
                return false;

            if (!locationOccupancyLookup.HasBuffer(currentGridEntity))
                return false;

            if (!locationReservationLookup.HasBuffer(currentGridEntity))
                return false;

            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy = locationOccupancyLookup[currentGridEntity];
            DynamicBuffer<EM_BufferElement_LocationReservation> reservations = locationReservationLookup[currentGridEntity];

            if (nodeIndex >= occupancy.Length || nodeIndex >= reservations.Length)
                return false;

            Entity occupant = occupancy[nodeIndex].Occupant;

            if (occupant != Entity.Null && occupant != requester)
                return false;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy == requester)
            {
                entry.ReservedUntilTimeSeconds = timeSeconds + math.max(0.1f, reservationSeconds);
                reservations[nodeIndex] = entry;
                return true;
            }

            if (entry.ReservedBy != Entity.Null)
            {
                if (entry.ReservedUntilTimeSeconds > 0d && timeSeconds >= entry.ReservedUntilTimeSeconds)
                {
                    entry.ReservedBy = Entity.Null;
                    entry.ReservedUntilTimeSeconds = -1d;
                }
                else
                {
                    return false;
                }
            }

            entry.ReservedBy = requester;
            entry.ReservedUntilTimeSeconds = timeSeconds + math.max(0.1f, reservationSeconds);
            reservations[nodeIndex] = entry;
            return true;
        }

        private void ClearQueueSlotReservation(Entity requester, int nodeIndex)
        {
            if (nodeIndex < 0)
                return;

            if (!locationReservationLookup.HasBuffer(currentGridEntity))
                return;

            DynamicBuffer<EM_BufferElement_LocationReservation> reservations = locationReservationLookup[currentGridEntity];

            if (nodeIndex >= reservations.Length)
                return;

            EM_BufferElement_LocationReservation entry = reservations[nodeIndex];

            if (entry.ReservedBy != requester)
                return;

            entry.ReservedBy = Entity.Null;
            entry.ReservedUntilTimeSeconds = -1d;
            reservations[nodeIndex] = entry;
        }
        #endregion
    }
}
