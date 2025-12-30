using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_SocietyResourceDistribution
    {
        #region Log
        private static float GetSocietyTime(Entity societyRoot, ref ComponentLookup<EM_Component_SocietyClock> clockLookup)
        {
            if (!clockLookup.HasComponent(societyRoot))
                return 0f;

            return clockLookup[societyRoot].TimeOfDay;
        }

        private static void AppendDistributionDebugEvent(DynamicBuffer<EM_Component_Event> buffer, int maxEntries, float timeOfDay,
            Entity society, Entity member, FixedString64Bytes needId, FixedString64Bytes resourceId, float value)
        {
            EM_Component_Event debugEvent = new EM_Component_Event
            {
                Type = EM_DebugEventType.DistributionTransfer,
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

            EM_Utility_LogEvent.AppendEvent(buffer, maxEntries, debugEvent);
        }
        #endregion
    }
}
