using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Debug helpers for society resource distribution.
    /// </summary>
    public partial struct EmergenceSocietyResourceDistributionSystem
    {
        #region Debug
        /// <summary>
        /// Returns the current time-of-day for the target society.
        /// </summary>
        private static float GetSocietyTime(Entity societyRoot, ref ComponentLookup<EmergenceSocietyClock> clockLookup)
        {
            if (!clockLookup.HasComponent(societyRoot))
                return 0f;

            return clockLookup[societyRoot].TimeOfDay;
        }

        /// <summary>
        /// Appends a distribution debug event to the shared log buffer.
        /// </summary>
        private static void AppendDistributionDebugEvent(DynamicBuffer<EmergenceDebugEvent> buffer, int maxEntries, float timeOfDay,
            Entity society, Entity member, FixedString64Bytes needId, FixedString64Bytes resourceId, float value)
        {
            EmergenceDebugEvent debugEvent = new EmergenceDebugEvent
            {
                Type = EmergenceDebugEventType.DistributionTransfer,
                Time = timeOfDay,
                Society = society,
                Subject = member,
                Target = society,
                NeedId = needId,
                ResourceId = resourceId,
                WindowId = default,
                Reason = default,
                Value = value
            };

            EmergenceDebugEventUtility.AppendEvent(buffer, maxEntries, debugEvent);
        }
        #endregion
    }
}
