using Unity.Collections;

namespace EmergentMechanics
{
    public partial struct EM_System_NarrativeSignalCollector
    {
        #region Builders
        private static EM_BufferElement_NarrativeSignal BuildSignal(EM_NarrativeEventType eventType, EM_Component_Event debugEvent,
            FixedString64Bytes contextId)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = eventType,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = contextId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = contextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildIntentSignal(EM_NarrativeEventType eventType, EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = eventType,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildInteractionSignal(EM_NarrativeEventType eventType, EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = eventType,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildScheduleSignal(EM_NarrativeEventType eventType, EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = eventType,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildResourceSignal(EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = EM_NarrativeEventType.ResourceChange,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ParameterId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildRelationshipSignal(EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = EM_NarrativeEventType.RelationshipChange,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildScheduleOverrideSignal(EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = EM_NarrativeEventType.ScheduleOverrideStart,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.ContextId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ParameterId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }

        private static EM_BufferElement_NarrativeSignal BuildEffectRawSignal(EM_Component_Event debugEvent)
        {
            return new EM_BufferElement_NarrativeSignal
            {
                EventType = EM_NarrativeEventType.EffectRaw,
                Time = debugEvent.Time,
                Society = debugEvent.Society,
                Subject = debugEvent.Subject,
                Target = debugEvent.Target,
                SignalId = debugEvent.SignalId,
                IntentId = debugEvent.IntentId,
                NeedId = debugEvent.NeedId,
                ResourceId = debugEvent.ResourceId,
                ActivityId = debugEvent.ActivityId,
                ContextId = debugEvent.ContextId,
                ReasonId = debugEvent.Reason,
                EffectType = debugEvent.EffectType,
                Value = debugEvent.Value,
                Delta = debugEvent.Delta,
                Before = debugEvent.Before,
                After = debugEvent.After,
                Flags = 0
            };
        }
        #endregion
    }
}
