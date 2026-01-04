using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Schedule
        // Apply a schedule override for the target NPC.
        private static bool ApplyScheduleOverride(Entity target, FixedString64Bytes activityId, float durationHours,
            ref ComponentLookup<EM_Component_NpcSchedule> scheduleLookup, ref ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup,
            out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (!scheduleOverrideLookup.HasComponent(target) || !scheduleLookup.HasComponent(target))
                return false;

            EM_Component_NpcSchedule schedule = scheduleLookup[target];

            if (!schedule.Schedule.IsCreated)
                return false;

            EM_Component_NpcScheduleOverride scheduleOverride = scheduleOverrideLookup[target];
            before = scheduleOverride.RemainingHours;

            if (durationHours <= 0f || activityId.Length == 0)
            {
                scheduleOverride.ActivityId = default;
                scheduleOverride.RemainingHours = 0f;
                scheduleOverride.DurationHours = 0f;
                scheduleOverride.EntryIndex = -1;
                scheduleOverrideLookup[target] = scheduleOverride;
                after = 0f;
                return true;
            }

            scheduleOverride.ActivityId = activityId;
            scheduleOverride.RemainingHours = durationHours;
            scheduleOverride.DurationHours = durationHours;
            scheduleOverride.EntryIndex = FindEntryIndexByActivityId(schedule.Schedule, activityId);
            scheduleOverrideLookup[target] = scheduleOverride;
            after = durationHours;
            return true;
        }

        // Find schedule entry index by activity id.
        private static int FindEntryIndexByActivityId(BlobAssetReference<EM_BlobDefinition_NpcSchedule> schedule, FixedString64Bytes activityId)
        {
            if (!schedule.IsCreated || activityId.Length == 0)
                return -1;

            ref BlobArray<EM_Blob_NpcScheduleEntry> entries = ref schedule.Value.Entries;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].ActivityId.Equals(activityId))
                    return i;
            }

            return -1;
        }
        #endregion
    }
}
