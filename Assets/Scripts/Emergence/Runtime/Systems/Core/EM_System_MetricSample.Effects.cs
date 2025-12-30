using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_MetricSample : ISystem
    {
        #region Effects
        // Effect routing based on the effect type enum.
        #region Routing
        private static void ApplyEffect(EM_Blob_Effect effect, Entity target, float magnitude,
            ref BufferLookup<EM_BufferElement_Need> needLookup, ref BufferLookup<EM_BufferElement_Resource> resourceLookup,
            ref ComponentLookup<EM_Component_Reputation> reputationLookup, ref ComponentLookup<EM_Component_Cohesion> cohesionLookup,
            ref ComponentLookup<EM_Component_NpcSchedule> scheduleLookup, ref ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup)
        {
            switch (effect.EffectType)
            {
                case EmergenceEffectType.ModifyNeed:
                    ApplyNeedDelta(target, effect.ParameterId, magnitude, ref needLookup);
                    return;

                case EmergenceEffectType.ModifyResource:
                    ApplyResourceDelta(target, effect.ParameterId, magnitude, ref resourceLookup);
                    return;

                case EmergenceEffectType.ModifyReputation:
                    ApplyReputationDelta(target, magnitude, ref reputationLookup);
                    return;

                case EmergenceEffectType.ModifyCohesion:
                    ApplyCohesionDelta(target, magnitude, ref cohesionLookup);
                    return;

                case EmergenceEffectType.OverrideSchedule:
                    ApplyScheduleOverride(target, effect.ParameterId, magnitude, ref scheduleLookup, ref scheduleOverrideLookup);
                    return;
            }
        }
        #endregion

        // Need buffer updates.
        #region Need
        private static void ApplyNeedDelta(Entity target, FixedString64Bytes needId, float delta,
            ref BufferLookup<EM_BufferElement_Need> needLookup)
        {
            if (!needLookup.HasBuffer(target) || needId.Length == 0)
                return;

            DynamicBuffer<EM_BufferElement_Need> needs = needLookup[target];

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EM_BufferElement_Need entry = needs[i];
                entry.Value += delta;
                needs[i] = entry;
                return;
            }

            EM_BufferElement_Need newEntry = new EM_BufferElement_Need
            {
                NeedId = needId,
                Value = delta
            };

            needs.Add(newEntry);
        }
        #endregion

        // Resource buffer updates.
        #region Resources
        private static void ApplyResourceDelta(Entity target, FixedString64Bytes resourceId, float delta,
            ref BufferLookup<EM_BufferElement_Resource> resourceLookup)
        {
            if (!resourceLookup.HasBuffer(target) || resourceId.Length == 0)
                return;

            DynamicBuffer<EM_BufferElement_Resource> resources = resourceLookup[target];

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EM_BufferElement_Resource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            EM_BufferElement_Resource newEntry = new EM_BufferElement_Resource
            {
                ResourceId = resourceId,
                Amount = delta
            };

            resources.Add(newEntry);
        }
        #endregion

        // Reputation and cohesion updates.
        #region Social
        private static void ApplyReputationDelta(Entity target, float delta,
            ref ComponentLookup<EM_Component_Reputation> reputationLookup)
        {
            if (!reputationLookup.HasComponent(target))
                return;

            EM_Component_Reputation reputation = reputationLookup[target];
            reputation.Value += delta;
            reputationLookup[target] = reputation;
        }

        private static void ApplyCohesionDelta(Entity target, float delta,
            ref ComponentLookup<EM_Component_Cohesion> cohesionLookup)
        {
            if (!cohesionLookup.HasComponent(target))
                return;

            EM_Component_Cohesion cohesion = cohesionLookup[target];
            cohesion.Value += delta;
            cohesionLookup[target] = cohesion;
        }
        #endregion

        // Schedule override effect application.
        #region ScheduleOverride
        private static void ApplyScheduleOverride(Entity target, FixedString64Bytes activityId, float durationHours,
            ref ComponentLookup<EM_Component_NpcSchedule> scheduleLookup, ref ComponentLookup<EM_Component_NpcScheduleOverride> scheduleOverrideLookup)
        {
            if (!scheduleOverrideLookup.HasComponent(target) || !scheduleLookup.HasComponent(target))
                return;

            EM_Component_NpcSchedule schedule = scheduleLookup[target];

            if (!schedule.Schedule.IsCreated)
                return;

            EM_Component_NpcScheduleOverride scheduleOverride = scheduleOverrideLookup[target];

            if (durationHours <= 0f || activityId.Length == 0)
            {
                scheduleOverride.ActivityId = default;
                scheduleOverride.RemainingHours = 0f;
                scheduleOverride.DurationHours = 0f;
                scheduleOverride.EntryIndex = -1;
                scheduleOverrideLookup[target] = scheduleOverride;
                return;
            }

            scheduleOverride.ActivityId = activityId;
            scheduleOverride.RemainingHours = durationHours;
            scheduleOverride.DurationHours = durationHours;
            scheduleOverride.EntryIndex = FindEntryIndexByActivityId(schedule.Schedule, activityId);
            scheduleOverrideLookup[target] = scheduleOverride;
        }

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
        #endregion
    }
}
