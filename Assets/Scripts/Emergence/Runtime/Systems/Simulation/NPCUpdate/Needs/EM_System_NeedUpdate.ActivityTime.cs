using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NeedUpdate : ISystem
    {
        #region ActivityTime
        // Resolve normalized activity time for need rate sampling.
        private static float ResolveActivityNormalizedTime(FixedString64Bytes activityId, int entryIndex, byte isOverride, float timeOfDay,
            EM_Component_NpcSchedule schedule, bool hasSchedule, EM_Component_NpcScheduleOverride scheduleOverride, bool hasOverride,
            EM_Component_NpcScheduleDuration scheduleDuration, bool hasDuration)
        {
            if (activityId.Length == 0)
                return 0f;

            if (!hasSchedule || !schedule.Schedule.IsCreated)
                return 0f;

            ref BlobArray<EM_Blob_NpcScheduleEntry> entries = ref schedule.Schedule.Value.Entries;

            if (entries.Length == 0)
                return 0f;

            if (isOverride != 0)
                return GetOverrideNormalizedTime(activityId, scheduleOverride, hasOverride);

            if (IsDurationActive(activityId, scheduleDuration, hasDuration))
                return GetDurationNormalizedTime(activityId, scheduleDuration, ref entries);

            return GetWindowNormalizedTime(activityId, entryIndex, timeOfDay, ref entries);
        }

        #region Override
        private static float GetOverrideNormalizedTime(FixedString64Bytes activityId, EM_Component_NpcScheduleOverride scheduleOverride, bool hasOverride)
        {
            if (!hasOverride)
                return 0f;

            if (!scheduleOverride.ActivityId.Equals(activityId))
                return 0f;

            float duration = math.max(scheduleOverride.DurationHours, 0f);

            if (duration <= 0f)
                return 0f;

            float remaining = math.max(scheduleOverride.RemainingHours, 0f);
            float elapsed = math.max(duration - remaining, 0f);

            return math.saturate(elapsed / duration);
        }
        #endregion

        // Duration activity sampling.
        #region Duration
        private static bool IsDurationActive(FixedString64Bytes activityId, EM_Component_NpcScheduleDuration scheduleDuration, bool hasDuration)
        {
            if (!hasDuration)
                return false;

            if (scheduleDuration.RemainingHours <= 0f)
                return false;

            if (scheduleDuration.ActivityId.Length == 0)
                return false;

            if (!scheduleDuration.ActivityId.Equals(activityId))
                return false;

            return true;
        }

        private static float GetDurationNormalizedTime(FixedString64Bytes activityId, EM_Component_NpcScheduleDuration scheduleDuration,
            ref BlobArray<EM_Blob_NpcScheduleEntry> entries)
        {
            int resolvedIndex = ResolveEntryIndex(activityId, scheduleDuration.EntryIndex, ref entries);

            if (resolvedIndex < 0)
                return 0f;

            ref EM_Blob_NpcScheduleEntry entry = ref entries[resolvedIndex];
            float maxDuration = ResolveMaxDurationHours(ref entry);

            if (maxDuration <= 0f)
                return 0f;

            float duration = math.max(scheduleDuration.DurationHours, 0f);
            float remaining = math.max(scheduleDuration.RemainingHours, 0f);
            float elapsed = math.max(duration - remaining, 0f);

            return math.saturate(elapsed / maxDuration);
        }
        #endregion

        // Window-based activity sampling.
        #region Window
        private static float GetWindowNormalizedTime(FixedString64Bytes activityId, int entryIndex, float timeOfDay,
            ref BlobArray<EM_Blob_NpcScheduleEntry> entries)
        {
            int resolvedIndex = ResolveEntryIndex(activityId, entryIndex, ref entries);

            if (resolvedIndex < 0)
                return 0f;

            ref EM_Blob_NpcScheduleEntry entry = ref entries[resolvedIndex];

            return GetWindowProgress(timeOfDay, entry.StartHour, entry.EndHour);
        }

        private static float GetWindowProgress(float timeOfDay, float startHour, float endHour)
        {
            float length = GetWindowLength(startHour, endHour);

            if (length <= 0f)
                return 0f;

            if (startHour < endHour)
                return math.saturate((timeOfDay - startHour) / length);

            if (timeOfDay >= startHour)
                return math.saturate((timeOfDay - startHour) / length);

            return math.saturate((timeOfDay + (24f - startHour)) / length);
        }

        private static float GetWindowLength(float startHour, float endHour)
        {
            if (startHour == endHour)
                return 0f;

            float length = startHour < endHour
                ? endHour - startHour
                : (24f - startHour) + endHour;

            if (length > 24f)
                return 24f;

            return length;
        }
        #endregion

        // Schedule entry resolution helpers.
        #region EntryResolution
        private static int ResolveEntryIndex(FixedString64Bytes activityId, int entryIndex, ref BlobArray<EM_Blob_NpcScheduleEntry> entries)
        {
            if (entryIndex >= 0 && entryIndex < entries.Length &&
                (activityId.Length == 0 || entries[entryIndex].ActivityId.Equals(activityId)))
                return entryIndex;

            if (activityId.Length == 0)
                return -1;

            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].ActivityId.Equals(activityId))
                    continue;

                return i;
            }

            return -1;
        }

        private static float ResolveMaxDurationHours(ref EM_Blob_NpcScheduleEntry entry)
        {
            if (entry.UseDuration != 0)
                return math.max(entry.MaxDurationHours, 0f);

            return GetWindowLength(entry.StartHour, entry.EndHour);
        }
        #endregion
        #endregion
    }
}
