using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcSchedule : ISystem
    {
        #region Duration
        // Duration state management.
        #region DurationState
        private static void UpdateDuration(RefRW<EM_Component_NpcScheduleDuration> scheduleDuration, float deltaHours)
        {
            if (scheduleDuration.ValueRO.RemainingHours > 0f && scheduleDuration.ValueRO.ActivityId.Length > 0)
            {
                float remaining = scheduleDuration.ValueRO.RemainingHours - deltaHours;

                if (remaining <= 0f)
                {
                    scheduleDuration.ValueRW.RemainingHours = 0f;
                    scheduleDuration.ValueRW.DurationHours = 0f;
                    scheduleDuration.ValueRW.ActivityId = default;
                    scheduleDuration.ValueRW.EntryIndex = -1;
                }
                else
                {
                    scheduleDuration.ValueRW.RemainingHours = remaining;
                }

                return;
            }

            if (scheduleDuration.ValueRO.RemainingHours != 0f || scheduleDuration.ValueRO.ActivityId.Length > 0 || scheduleDuration.ValueRO.EntryIndex != -1)
            {
                scheduleDuration.ValueRW.RemainingHours = 0f;
                scheduleDuration.ValueRW.DurationHours = 0f;
                scheduleDuration.ValueRW.ActivityId = default;
                scheduleDuration.ValueRW.EntryIndex = -1;
            }
        }

        private static void ResolveDurationEntry(RefRW<EM_Component_NpcScheduleDuration> scheduleDuration,
            ref BlobArray<EM_Blob_NpcScheduleEntry> entries, out int entryIndex)
        {
            entryIndex = scheduleDuration.ValueRO.EntryIndex;

            if (entryIndex >= 0 && entryIndex < entries.Length)
                return;

            if (scheduleDuration.ValueRO.ActivityId.Length == 0)
            {
                entryIndex = -1;
                return;
            }

            int resolved = FindEntryByActivityId(ref entries, scheduleDuration.ValueRO.ActivityId);

            if (resolved >= 0)
            {
                scheduleDuration.ValueRW.EntryIndex = resolved;
                entryIndex = resolved;
                return;
            }

            entryIndex = -1;
        }
        #endregion

        // Duration progress helpers.
        #region DurationProgress
        private static float GetDurationProgress(EM_Component_NpcScheduleDuration scheduleDuration)
        {
            float duration = scheduleDuration.DurationHours;

            if (duration <= 0f)
                return 0f;

            float remaining = math.max(scheduleDuration.RemainingHours, 0f);
            float progress = 1f - (remaining / duration);

            return math.saturate(progress);
        }
        #endregion

        // Duration sampling helpers.
        #region DurationSampling
        private static float ResolveDurationHours(ref EM_Blob_NpcScheduleEntry entryData, bool hasRandom,
            ref EM_Component_RandomSeed seed, out bool seedChanged)
        {
            seedChanged = false;

            if (entryData.UseDuration == 0)
                return 0f;

            float minHours = math.max(entryData.MinDurationHours, 0f);
            float maxHours = math.max(entryData.MaxDurationHours, 0f);

            if (maxHours <= 0f)
                return 0f;

            if (minHours > maxHours)
                minHours = maxHours;

            if (minHours == maxHours)
                return maxHours;

            if (!hasRandom)
                return maxHours;

            float t = NextRandom01(ref seed);
            seedChanged = true;
            return math.lerp(minHours, maxHours, t);
        }
        #endregion

        // Deterministic random helpers.
        #region Random
        private static float NextRandom01(ref EM_Component_RandomSeed seed)
        {
            uint current = seed.Value;

            if (current == 0)
                current = 1u;

            Random random = Random.CreateFromIndex(current);
            float value = random.NextFloat();
            seed.Value = random.NextUInt();

            return value;
        }
        #endregion
        #endregion
    }
}
