using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region TradeEntry
        private static bool TryGetTradeEntry(Entity entity, ref ComponentLookup<EM_Component_NpcSchedule> scheduleLookup,
            ref ComponentLookup<EM_Component_NpcScheduleTarget> scheduleTargetLookup,
            out BlobAssetReference<EM_BlobDefinition_NpcSchedule> schedule, out int entryIndex)
        {
            schedule = default;
            entryIndex = -1;

            if (!scheduleLookup.HasComponent(entity) || !scheduleTargetLookup.HasComponent(entity))
                return false;

            EM_Component_NpcSchedule scheduleComponent = scheduleLookup[entity];

            if (!scheduleComponent.Schedule.IsCreated)
                return false;

            schedule = scheduleComponent.Schedule;
            ref BlobArray<EM_Blob_NpcScheduleEntry> entries = ref schedule.Value.Entries;

            if (entries.Length == 0)
                return false;

            EM_Component_NpcScheduleTarget scheduleTarget = scheduleTargetLookup[entity];

            if (scheduleTarget.ActivityId.Length == 0)
                return false;

            entryIndex = scheduleTarget.EntryIndex;

            if (entryIndex >= 0 && entryIndex < entries.Length)
            {
                ref EM_Blob_NpcScheduleEntry entry = ref entries[entryIndex];

                if (entry.ActivityId.Equals(scheduleTarget.ActivityId))
                    return true;
            }

            int resolvedIndex = FindEntryByActivityId(ref entries, scheduleTarget.ActivityId);

            if (resolvedIndex < 0)
                return false;

            entryIndex = resolvedIndex;
            return true;
        }
        #endregion
    }
}
