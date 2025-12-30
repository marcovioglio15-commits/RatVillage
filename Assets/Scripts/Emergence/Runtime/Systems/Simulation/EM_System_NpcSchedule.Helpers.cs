using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcSchedule : ISystem
    {
        #region Helpers
        // Override state management.
        #region OverrideState
        private static void UpdateOverride(RefRW<EM_Component_NpcScheduleOverride> scheduleOverride, float deltaHours)
        {
            if (scheduleOverride.ValueRO.RemainingHours > 0f && scheduleOverride.ValueRO.ActivityId.Length > 0)
            {
                float remaining = scheduleOverride.ValueRO.RemainingHours - deltaHours;

                if (remaining <= 0f)
                {
                    scheduleOverride.ValueRW.RemainingHours = 0f;
                    scheduleOverride.ValueRW.DurationHours = 0f;
                    scheduleOverride.ValueRW.ActivityId = default;
                    scheduleOverride.ValueRW.EntryIndex = -1;
                }
                else
                {
                    scheduleOverride.ValueRW.RemainingHours = remaining;
                }

                return;
            }

            if (scheduleOverride.ValueRO.RemainingHours != 0f || scheduleOverride.ValueRO.ActivityId.Length > 0 || scheduleOverride.ValueRO.EntryIndex != -1)
            {
                scheduleOverride.ValueRW.RemainingHours = 0f;
                scheduleOverride.ValueRW.DurationHours = 0f;
                scheduleOverride.ValueRW.ActivityId = default;
                scheduleOverride.ValueRW.EntryIndex = -1;
            }
        }

        private static void ResolveOverrideEntry(RefRW<EM_Component_NpcScheduleOverride> scheduleOverride,
            ref BlobArray<EM_Blob_NpcScheduleEntry> entries, out int entryIndex)
        {
            entryIndex = scheduleOverride.ValueRO.EntryIndex;

            if (entryIndex >= 0 && entryIndex < entries.Length)
                return;

            if (scheduleOverride.ValueRO.ActivityId.Length == 0)
            {
                entryIndex = -1;
                return;
            }

            int resolved = FindEntryByActivityId(ref entries, scheduleOverride.ValueRO.ActivityId);

            if (resolved >= 0)
            {
                scheduleOverride.ValueRW.EntryIndex = resolved;
                entryIndex = resolved;
                return;
            }

            entryIndex = -1;
        }
        #endregion

        // Schedule entry selection.
        #region EntrySelection
        private static int FindEntryForTime(float timeOfDay, ref BlobArray<EM_Blob_NpcScheduleEntry> entries)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                ref EM_Blob_NpcScheduleEntry entry = ref entries[i];

                if (entry.ActivityId.Length == 0)
                    continue;

                if (IsInWindow(timeOfDay, entry.StartHour, entry.EndHour))
                    return i;
            }

            return -1;
        }

        private static int FindEntryByActivityId(ref BlobArray<EM_Blob_NpcScheduleEntry> entries, FixedString64Bytes activityId)
        {
            if (activityId.Length == 0)
                return -1;

            for (int i = 0; i < entries.Length; i++)
            {
                ref EM_Blob_NpcScheduleEntry entry = ref entries[i];

                if (entry.ActivityId.Equals(activityId))
                    return i;
            }

            return -1;
        }
        #endregion

        // Time window helpers.
        #region TimeWindow
        private static bool IsInWindow(float timeOfDay, float startHour, float endHour)
        {
            if (startHour == endHour)
                return false;

            if (startHour < endHour)
                return timeOfDay >= startHour && timeOfDay < endHour;

            return timeOfDay >= startHour || timeOfDay < endHour;
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

        private static float GetOverrideProgress(EM_Component_NpcScheduleOverride scheduleOverride)
        {
            float duration = scheduleOverride.DurationHours;

            if (duration <= 0f)
                return 0f;

            float remaining = math.max(scheduleOverride.RemainingHours, 0f);
            float progress = 1f - (remaining / duration);

            return math.saturate(progress);
        }

        private static float GetWindowLength(float startHour, float endHour)
        {
            if (startHour == endHour)
                return 0f;

            if (startHour < endHour)
                return endHour - startHour;

            return (24f - startHour) + endHour;
        }
        #endregion

        // Curve evaluation and signal emission.
        #region SignalAndCurve
        private static float SampleCurve(ref BlobArray<float> samples, float normalizedTime)
        {
            if (samples.Length == 0)
                return 1f;

            float scaled = math.clamp(normalizedTime, 0f, 1f) * (samples.Length - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, samples.Length - 1);
            float t = scaled - index;
            float value = math.lerp(samples[index], samples[nextIndex], t);

            return math.saturate(value);
        }

        private static void EmitSignal(FixedString64Bytes signalId, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
            Entity subject, Entity societyRoot, float value)
        {
            if (signalId.Length == 0)
                return;

            EM_BufferElement_SignalEvent signalEvent = new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = value,
                Subject = subject,
                SocietyRoot = societyRoot,
                Time = 0d
            };

            signals.Add(signalEvent);
        }
        #endregion

        // Debug event formatting.
        #region DebugEvents
        private static EM_Component_Event ScheduleLogEvent(EM_DebugEventType eventType, float timeOfDay,
            Entity society, Entity subject, FixedString64Bytes activityId, float value)
        {
            EM_Component_Event debugEvent = new EM_Component_Event
            {
                Type = eventType,
                Time = timeOfDay,
                Society = society,
                Subject = subject,
                Target = Entity.Null,
                NeedId = default,
                ResourceId = default,
                WindowId = activityId,
                Reason = default,
                Value = value
            };

            return debugEvent;
        }
        #endregion
        #endregion
    }
}
