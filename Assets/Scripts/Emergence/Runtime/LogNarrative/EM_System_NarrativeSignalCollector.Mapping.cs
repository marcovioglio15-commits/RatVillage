namespace EmergentMechanics
{
    public partial struct EM_System_NarrativeSignalCollector
    {
        #region Mapping
        private bool TryBuildNarrativeSignal(EM_Component_Event debugEvent, out EM_BufferElement_NarrativeSignal signal)
        {
            signal = default;
            EM_NarrativeEventType eventType;

            if (debugEvent.Type == EM_DebugEventType.SignalEmitted)
            {
                eventType = ResolveSignalEventType(debugEvent);

                if (eventType == EM_NarrativeEventType.SignalRaw && debugEvent.SignalId.Length == 0)
                    return false;

                signal = BuildSignal(eventType, debugEvent, debugEvent.ContextId);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.IntentCreated)
            {
                eventType = EM_NarrativeEventType.IntentCreated;
                signal = BuildIntentSignal(eventType, debugEvent);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.EffectApplied)
            {
                if (TryBuildEffectSignal(debugEvent, out signal))
                    return true;

                return false;
            }

            if (debugEvent.Type == EM_DebugEventType.InteractionAttempt)
            {
                eventType = EM_NarrativeEventType.TradeAttempt;
                signal = BuildInteractionSignal(eventType, debugEvent);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.InteractionSuccess)
            {
                eventType = EM_NarrativeEventType.TradeSuccess;
                signal = BuildInteractionSignal(eventType, debugEvent);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.InteractionFail)
            {
                eventType = EM_NarrativeEventType.TradeFail;
                signal = BuildInteractionSignal(eventType, debugEvent);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.ScheduleWindow)
            {
                eventType = IsOverrideActivity(debugEvent.ActivityId)
                    ? EM_NarrativeEventType.ScheduleOverrideStart
                    : EM_NarrativeEventType.ScheduleStart;
                signal = BuildScheduleSignal(eventType, debugEvent);
                return true;
            }

            if (debugEvent.Type == EM_DebugEventType.ScheduleEnd)
            {
                eventType = IsOverrideActivity(debugEvent.ActivityId)
                    ? EM_NarrativeEventType.ScheduleOverrideEnd
                    : EM_NarrativeEventType.ScheduleEnd;
                signal = BuildScheduleSignal(eventType, debugEvent);
                return true;
            }

            return false;
        }

        private EM_NarrativeEventType ResolveSignalEventType(EM_Component_Event debugEvent)
        {
            if (IsNeedUrgencySignal(debugEvent))
                return EM_NarrativeEventType.NeedUrgency;

            if (IsHealthValueSignal(debugEvent))
                return EM_NarrativeEventType.HealthValue;

            if (IsHealthDamageSignal(debugEvent))
                return EM_NarrativeEventType.HealthDamage;

            return EM_NarrativeEventType.SignalRaw;
        }

        private bool TryBuildEffectSignal(EM_Component_Event debugEvent, out EM_BufferElement_NarrativeSignal signal)
        {
            signal = default;

            if (debugEvent.EffectType == EmergenceEffectType.ModifyResource)
            {
                signal = BuildResourceSignal(debugEvent);
                return true;
            }

            if (debugEvent.EffectType == EmergenceEffectType.ModifyRelationship)
            {
                signal = BuildRelationshipSignal(debugEvent);
                return true;
            }

            if (debugEvent.EffectType == EmergenceEffectType.OverrideSchedule && debugEvent.After > 0f)
            {
                signal = BuildScheduleOverrideSignal(debugEvent);
                return true;
            }

            if (debugEvent.EffectType == EmergenceEffectType.EmitSignal)
            {
                signal = BuildSignal(EM_NarrativeEventType.SignalRaw, debugEvent, debugEvent.ContextId);
                return true;
            }

            signal = BuildEffectRawSignal(debugEvent);
            return true;
        }
        #endregion
    }
}
