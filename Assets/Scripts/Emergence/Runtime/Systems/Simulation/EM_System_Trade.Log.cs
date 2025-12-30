using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Log
        private static float GetSocietyTime(Entity societyRoot, ref ComponentLookup<EM_Component_SocietyClock> clockLookup)
        {
            if (!clockLookup.HasComponent(societyRoot))
                return 0f;

            return clockLookup[societyRoot].TimeOfDay;
        }

        private static void AppendTradeDebugEvent(DynamicBuffer<EM_Component_Event> buffer, int maxEntries,
            EM_DebugEventType eventType, float timeOfDay, Entity society, Entity subject, Entity target,
            FixedString64Bytes needId, FixedString64Bytes resourceId, FixedString64Bytes reason, float value)
        {
            EM_Component_Event debugEvent = new EM_Component_Event
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

            EM_Utility_LogEvent.AppendEvent(buffer, maxEntries, debugEvent);
        }
        #endregion
    }
}
