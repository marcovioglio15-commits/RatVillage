using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static class EM_Utility_LogEvent
    {
        #region Public Properties
        public static void AppendEvent(DynamicBuffer<EM_Component_Event> buffer, int maxEntries, EM_Component_Event entry)
        {
            if (maxEntries <= 0)
                return;

            int limit = maxEntries;

            if (limit < 1)
                return;

            if (buffer.Length >= limit)
            {
                int removeCount = buffer.Length - limit + 1;

                if (removeCount > 0)
                    buffer.RemoveRange(0, removeCount);
            }

            buffer.Add(entry);
        }
        #endregion

        #region Builders
        public static EM_Component_Event BuildSignalEvent(FixedString64Bytes signalId, float value, FixedString64Bytes contextId,
            Entity subject, Entity target, Entity society)
        {
            return new EM_Component_Event
            {
                Type = EM_DebugEventType.SignalEmitted,
                Time = 0d,
                Society = society,
                Subject = subject,
                Target = target,
                SignalId = signalId,
                IntentId = default,
                EffectType = default,
                ParameterId = default,
                ContextId = contextId,
                NeedId = default,
                ResourceId = default,
                ActivityId = default,
                Reason = default,
                Value = value,
                Delta = 0f,
                Before = 0f,
                After = 0f
            };
        }

        public static EM_Component_Event BuildIntentEvent(FixedString64Bytes intentId, FixedString64Bytes needId, FixedString64Bytes resourceId,
            float desiredAmount, float urgency, Entity subject, Entity target, Entity society)
        {
            return new EM_Component_Event
            {
                Type = EM_DebugEventType.IntentCreated,
                Time = 0d,
                Society = society,
                Subject = subject,
                Target = target,
                SignalId = default,
                IntentId = intentId,
                EffectType = default,
                ParameterId = default,
                ContextId = default,
                NeedId = needId,
                ResourceId = resourceId,
                ActivityId = default,
                Reason = default,
                Value = desiredAmount,
                Delta = urgency,
                Before = 0f,
                After = urgency
            };
        }

        public static EM_Component_Event BuildEffectEvent(EmergenceEffectType effectType, FixedString64Bytes parameterId, FixedString64Bytes contextId,
            float delta, float before, float after, Entity subject, Entity target, Entity society)
        {
            return new EM_Component_Event
            {
                Type = EM_DebugEventType.EffectApplied,
                Time = 0d,
                Society = society,
                Subject = subject,
                Target = target,
                SignalId = default,
                IntentId = default,
                EffectType = effectType,
                ParameterId = parameterId,
                ContextId = contextId,
                NeedId = default,
                ResourceId = default,
                ActivityId = default,
                Reason = default,
                Value = delta,
                Delta = delta,
                Before = before,
                After = after
            };
        }

        public static EM_Component_Event BuildInteractionEvent(EM_DebugEventType eventType, FixedString64Bytes reason,
            Entity subject, Entity target, Entity society, FixedString64Bytes needId, FixedString64Bytes resourceId, float value)
        {
            return new EM_Component_Event
            {
                Type = eventType,
                Time = 0d,
                Society = society,
                Subject = subject,
                Target = target,
                SignalId = default,
                IntentId = default,
                EffectType = default,
                ParameterId = default,
                ContextId = default,
                NeedId = needId,
                ResourceId = resourceId,
                ActivityId = default,
                Reason = reason,
                Value = value,
                Delta = 0f,
                Before = 0f,
                After = 0f
            };
        }

        public static EM_Component_Event BuildScheduleEvent(EM_DebugEventType eventType, float timeOfDay, Entity society,
            Entity subject, FixedString64Bytes activityId, float value)
        {
            return new EM_Component_Event
            {
                Type = eventType,
                Time = timeOfDay,
                Society = society,
                Subject = subject,
                Target = Entity.Null,
                SignalId = default,
                IntentId = default,
                EffectType = default,
                ParameterId = default,
                ContextId = default,
                NeedId = default,
                ResourceId = default,
                ActivityId = activityId,
                Reason = default,
                Value = value,
                Delta = 0f,
                Before = 0f,
                After = 0f
            };
        }
        #endregion
    }
}
