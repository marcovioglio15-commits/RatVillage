using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Debug event helpers for the Emergence trade system.
    /// </summary>
    public partial struct EmergenceTradeSystem
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
        /// Appends a trade debug event to the shared log buffer.
        /// </summary>
        private static void AppendTradeDebugEvent(DynamicBuffer<EmergenceDebugEvent> buffer, int maxEntries,
            EmergenceDebugEventType eventType, float timeOfDay, Entity society, Entity subject, Entity target,
            FixedString64Bytes needId, FixedString64Bytes resourceId, FixedString64Bytes reason, float value)
        {
            EmergenceDebugEvent debugEvent = new EmergenceDebugEvent
            {
                Type = eventType,
                Time = timeOfDay,
                Society = society,
                Subject = subject,
                Target = target,
                NeedId = needId,
                ResourceId = resourceId,
                WindowId = default,
                Reason = reason,
                Value = value
            };

            EmergenceDebugEventUtility.AppendEvent(buffer, maxEntries, debugEvent);
        }
        #endregion
    }
}
