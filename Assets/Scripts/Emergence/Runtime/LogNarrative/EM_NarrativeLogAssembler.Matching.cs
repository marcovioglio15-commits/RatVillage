using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NarrativeLogAssembler
    {
        #region Entry
        private bool TryAppendEntryFromSignal(EM_BufferElement_NarrativeSignal signal, EM_NarrativeTemplate[] templateList,
            EM_NarrativeThresholds thresholds, ref EM_Component_NarrativeLog log,
            DynamicBuffer<EM_BufferElement_NarrativeLogEntry> entryBuffer)
        {
            EM_NarrativeEventType eventType;

            if (!TryResolveEventType(signal, thresholds, out eventType))
                return false;

            matchingTemplates.Clear();

            for (int i = 0; i < templateList.Length; i++)
            {
                if (!IsTemplateMatch(templateList[i], eventType, signal, thresholds))
                    continue;

                matchingTemplates.Add(i);
            }

            if (matchingTemplates.Count == 0)
                return false;

            int templateIndex = SelectTemplateIndex(templateList, matchingTemplates);

            if (templateIndex < 0 || templateIndex >= templateList.Length)
                return false;

            EM_NarrativeTemplate template = templateList[templateIndex];
            double timeHours = signal.Time;
            EM_NarrativeEventKey key = BuildEventKey(eventType, signal);
            float cooldownHours = Mathf.Max(thresholds.DedupWindowHours, template.CooldownHours);

            if (IsOnCooldown(key, timeHours, cooldownHours))
                return false;

            string title = EM_NarrativeLogFormatter.ApplyTemplate(template.TitleTemplate, signal, eventType, entityManager);
            string body = EM_NarrativeLogFormatter.ApplyTemplate(template.BodyTemplate, signal, eventType, entityManager);

            if (string.IsNullOrEmpty(body) && string.IsNullOrEmpty(title))
                return false;

            string safeTitle = EM_NarrativeLogFormatter.Truncate(title, 128);
            string safeBody = EM_NarrativeLogFormatter.Truncate(body, 512);
            EM_BufferElement_NarrativeLogEntry entry = new EM_BufferElement_NarrativeLogEntry
            {
                EventType = eventType,
                Severity = template.Severity,
                Visibility = template.Visibility,
                Verbosity = template.Verbosity,
                Tags = template.Tags,
                Time = timeHours,
                Subject = signal.Subject,
                Target = signal.Target,
                Title = new FixedString128Bytes(safeTitle ?? string.Empty),
                Text = new FixedString512Bytes(safeBody ?? string.Empty)
            };

            EM_Utility_NarrativeLogEvent.AppendEntry(entryBuffer, log.MaxLogEntries, ref log, entry);
            lastEventTimes[key] = timeHours;
            return true;
        }

        private bool TryResolveEventType(EM_BufferElement_NarrativeSignal signal, EM_NarrativeThresholds thresholds,
            out EM_NarrativeEventType resolvedEventType)
        {
            resolvedEventType = signal.EventType;

            if (signal.EventType == EM_NarrativeEventType.NeedUrgency)
                return TryResolveNeedEvent(signal, thresholds, out resolvedEventType);

            if (signal.EventType == EM_NarrativeEventType.HealthValue)
                return TryResolveHealthEvent(signal, thresholds, out resolvedEventType);

            if (signal.EventType == EM_NarrativeEventType.HealthDamage)
                return signal.Value >= thresholds.HealthDamageMin;

            if (signal.EventType == EM_NarrativeEventType.ResourceChange)
                return ShouldLogResourceChange(signal, thresholds);

            if (signal.EventType == EM_NarrativeEventType.RelationshipChange)
                return Mathf.Abs(signal.Delta) >= thresholds.RelationshipDeltaMin;

            return true;
        }

        private bool TryResolveNeedEvent(EM_BufferElement_NarrativeSignal signal, EM_NarrativeThresholds thresholds,
            out EM_NarrativeEventType resolvedEventType)
        {
            resolvedEventType = EM_NarrativeEventType.NeedUrgency;

            if (signal.Subject == Entity.Null || signal.NeedId.Length == 0)
                return false;

            EM_NarrativeNeedKey key = new EM_NarrativeNeedKey
            {
                Subject = signal.Subject,
                NeedId = signal.NeedId
            };

            EM_NarrativeNeedState state;
            bool hasState = needStates.TryGetValue(key, out state);
            EM_NarrativeNeedTier newTier = ResolveNeedTier(signal.Value, thresholds);

            if (!hasState)
            {
                state = new EM_NarrativeNeedState
                {
                    Tier = newTier,
                    Urgency = signal.Value,
                    LastEventTime = -1d
                };
                needStates[key] = state;
                return false;
            }

            if (newTier == state.Tier)
            {
                state.Urgency = signal.Value;
                needStates[key] = state;
                return false;
            }

            double timeHours = signal.Time;

            if (IsTierOnCooldown(state.LastEventTime, timeHours, thresholds.NeedCooldownHours))
            {
                state.Tier = newTier;
                state.Urgency = signal.Value;
                needStates[key] = state;
                return false;
            }

            if (newTier > state.Tier)
            {
                state.Tier = newTier;
                state.Urgency = signal.Value;
                state.LastEventTime = timeHours;
                needStates[key] = state;
                resolvedEventType = EM_NarrativeEventType.NeedUrgency;
                return true;
            }

            if (signal.Value <= thresholds.NeedRelief && newTier == EM_NarrativeNeedTier.Low)
            {
                state.Tier = newTier;
                state.Urgency = signal.Value;
                state.LastEventTime = timeHours;
                needStates[key] = state;
                resolvedEventType = EM_NarrativeEventType.NeedRelief;
                return true;
            }

            state.Tier = newTier;
            state.Urgency = signal.Value;
            needStates[key] = state;
            return false;
        }

        private bool TryResolveHealthEvent(EM_BufferElement_NarrativeSignal signal, EM_NarrativeThresholds thresholds,
            out EM_NarrativeEventType resolvedEventType)
        {
            resolvedEventType = EM_NarrativeEventType.HealthLow;

            if (signal.Subject == Entity.Null)
                return false;

            EM_NarrativeHealthState state;
            bool hasState = healthStates.TryGetValue(signal.Subject, out state);
            EM_NarrativeHealthTier newTier = ResolveHealthTier(signal.Value, thresholds);

            if (!hasState)
            {
                state = new EM_NarrativeHealthState
                {
                    Tier = newTier,
                    Value = signal.Value,
                    LastEventTime = -1d
                };
                healthStates[signal.Subject] = state;
                return false;
            }

            if (newTier == state.Tier)
            {
                state.Value = signal.Value;
                healthStates[signal.Subject] = state;
                return false;
            }

            double timeHours = signal.Time;

            if (IsTierOnCooldown(state.LastEventTime, timeHours, thresholds.HealthCooldownHours))
            {
                state.Tier = newTier;
                state.Value = signal.Value;
                healthStates[signal.Subject] = state;
                return false;
            }

            if (newTier == EM_NarrativeHealthTier.Critical)
            {
                state.Tier = newTier;
                state.Value = signal.Value;
                state.LastEventTime = timeHours;
                healthStates[signal.Subject] = state;
                resolvedEventType = EM_NarrativeEventType.HealthCritical;
                return true;
            }

            if (newTier == EM_NarrativeHealthTier.Low)
            {
                state.Tier = newTier;
                state.Value = signal.Value;
                state.LastEventTime = timeHours;
                healthStates[signal.Subject] = state;
                resolvedEventType = EM_NarrativeEventType.HealthLow;
                return true;
            }

            if (newTier == EM_NarrativeHealthTier.Normal && signal.Value >= thresholds.HealthRecovery)
            {
                state.Tier = newTier;
                state.Value = signal.Value;
                state.LastEventTime = timeHours;
                healthStates[signal.Subject] = state;
                resolvedEventType = EM_NarrativeEventType.HealthRecovered;
                return true;
            }

            state.Tier = newTier;
            state.Value = signal.Value;
            healthStates[signal.Subject] = state;
            return false;
        }

        private static bool ShouldLogResourceChange(EM_BufferElement_NarrativeSignal signal, EM_NarrativeThresholds thresholds)
        {
            if (Mathf.Abs(signal.Delta) >= thresholds.ResourceDeltaMin)
                return true;

            return signal.After <= thresholds.ResourceLow;
        }
        #endregion
    }
}
