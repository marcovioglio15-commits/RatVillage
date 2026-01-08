using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Schedule
        // Apply a schedule override for the target NPC.
        private static bool ApplyScheduleOverride(Entity target, FixedString64Bytes activityId, float durationHours, double timeSeconds,
            float priority, Entity societyRoot, ref ComponentLookup<EM_Component_SocietyMember> memberLookup,
            ref ComponentLookup<EM_Component_ScheduleOverrideSettings> overrideSettingsLookup,
            ref ComponentLookup<EM_Component_NpcSchedule> scheduleLookup,
            ref ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup,
            ref ComponentLookup<EM_Component_NpcScheduleOverrideGate> overrideGateLookup,
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

            if (IsOverrideBlocked(target, societyRoot, scheduleOverride, ref memberLookup, ref overrideSettingsLookup))
            {
                after = before;
                return false;
            }

            if (!TryArbitrateScheduleOverride(target, timeSeconds, priority, activityId, ref overrideGateLookup))
            {
                after = before;
                return false;
            }

            scheduleOverride.ActivityId = activityId;
            scheduleOverride.RemainingHours = durationHours;
            scheduleOverride.DurationHours = durationHours;
            scheduleOverride.EntryIndex = FindEntryIndexByActivityId(schedule.Schedule, activityId);
            scheduleOverrideLookup[target] = scheduleOverride;
            after = durationHours;
            return true;
        }

        private static bool IsOverrideBlocked(Entity target, Entity societyRoot, EM_Component_NpcScheduleOverride scheduleOverride,
            ref ComponentLookup<EM_Component_SocietyMember> memberLookup,
            ref ComponentLookup<EM_Component_ScheduleOverrideSettings> overrideSettingsLookup)
        {
            if (scheduleOverride.RemainingHours <= 0f || scheduleOverride.ActivityId.Length == 0)
                return false;

            Entity settingsRoot = societyRoot;

            if (settingsRoot == Entity.Null && memberLookup.HasComponent(target))
                settingsRoot = memberLookup[target].SocietyRoot;

            if (settingsRoot == Entity.Null || !overrideSettingsLookup.HasComponent(settingsRoot))
                return false;

            return overrideSettingsLookup[settingsRoot].BlockOverrideWhileOverridden != 0;
        }

        private static bool TryArbitrateScheduleOverride(Entity target, double timeSeconds, float priority, FixedString64Bytes activityId,
            ref ComponentLookup<EM_Component_NpcScheduleOverrideGate> overrideGateLookup)
        {
            if (!overrideGateLookup.HasComponent(target))
                return true;

            EM_Component_NpcScheduleOverrideGate gate = overrideGateLookup[target];

            if (gate.LastOverrideTimeSeconds == timeSeconds && priority <= gate.LastOverridePriority)
                return false;

            gate.LastOverrideTimeSeconds = timeSeconds;
            gate.LastOverridePriority = priority;
            gate.LastOverrideActivityId = activityId;
            overrideGateLookup[target] = gate;
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
