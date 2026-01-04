using System.Globalization;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_LogUiManager
    {
        #region Formatting
        private string FormatEvent(EM_Component_Event debugEvent)
        {
            string template = GetTemplate(debugEvent.Type);

            if (string.IsNullOrEmpty(template))
                return string.Empty;

            float timeOfDay = (float)debugEvent.Time;

            if (timeOfDay <= 0f)
                timeOfDay = GetSocietyTime();

            string timeString = FormatTimeOfDay(timeOfDay);
            string subject = FormatEntityId(debugEvent.Subject);
            string target = FormatEntityId(debugEvent.Target);
            string society = FormatEntityId(debugEvent.Society);
            string signal = debugEvent.SignalId.Length > 0 ? debugEvent.SignalId.ToString() : "None";
            string intent = debugEvent.IntentId.Length > 0 ? debugEvent.IntentId.ToString() : "None";
            string effect = debugEvent.Type == EM_DebugEventType.EffectApplied ? debugEvent.EffectType.ToString() : "None";
            string parameter = debugEvent.ParameterId.Length > 0 ? debugEvent.ParameterId.ToString() : "None";
            string context = debugEvent.ContextId.Length > 0 ? debugEvent.ContextId.ToString() : "None";
            string need = debugEvent.NeedId.Length > 0 ? debugEvent.NeedId.ToString() : "None";
            string resource = debugEvent.ResourceId.Length > 0 ? debugEvent.ResourceId.ToString() : "None";
            string activity = debugEvent.ActivityId.Length > 0 ? debugEvent.ActivityId.ToString() : "None";
            string reason = debugEvent.Reason.Length > 0 ? debugEvent.Reason.ToString() : "None";
            string value = FormatValue(debugEvent.Value);
            string delta = FormatValue(debugEvent.Delta);
            string before = FormatValue(debugEvent.Before);
            string after = FormatValue(debugEvent.After);

            string formatted = ApplyTemplate(template, timeString, subject, target, society, signal, intent, effect, parameter, context,
                need, resource, activity, reason, value, delta, before, after);
            return ApplyLineColor(debugEvent, formatted);
        }

        private static string ApplyTemplate(string template, string time, string subject, string target, string society,
            string signal, string intent, string effect, string parameter, string context, string need, string resource,
            string activity, string reason, string value, string delta, string before, string after)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string formatted = template;
            formatted = formatted.Replace("{time}", time);
            formatted = formatted.Replace("{subject}", subject);
            formatted = formatted.Replace("{target}", target);
            formatted = formatted.Replace("{society}", society);
            formatted = formatted.Replace("{signal}", signal);
            formatted = formatted.Replace("{intent}", intent);
            formatted = formatted.Replace("{effect}", effect);
            formatted = formatted.Replace("{parameter}", parameter);
            formatted = formatted.Replace("{context}", context);
            formatted = formatted.Replace("{need}", need);
            formatted = formatted.Replace("{resource}", resource);
            formatted = formatted.Replace("{activity}", activity);
            formatted = formatted.Replace("{window}", activity);
            formatted = formatted.Replace("{reason}", reason);
            formatted = formatted.Replace("{value}", value);
            formatted = formatted.Replace("{delta}", delta);
            formatted = formatted.Replace("{before}", before);
            formatted = formatted.Replace("{after}", after);

            return formatted;
        }

        private static string FormatTimeOfDay(float timeOfDay)
        {
            float wrapped = Mathf.Repeat(timeOfDay, 24f);
            int hours = Mathf.FloorToInt(wrapped);
            float minutesFloat = (wrapped - hours) * 60f;
            int minutes = Mathf.FloorToInt(minutesFloat);

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hours, minutes);
        }

        private string FormatEntityId(Entity entity)
        {
            if (entity == Entity.Null)
                return "None";

            if (!entityManager.Exists(entity))
                return entity.Index.ToString(CultureInfo.InvariantCulture);

            if (entityManager.HasComponent<EM_Component_NpcType>(entity))
            {
                EM_Component_NpcType LogName = entityManager.GetComponentData<EM_Component_NpcType>(entity);

                if (LogName.TypeId.Length > 0)
                {
                    string name = LogName.TypeId.ToString();
                    string id = entity.Index.ToString(CultureInfo.InvariantCulture);
                    return string.Format(CultureInfo.InvariantCulture, "{0} (ID {1})", name, id);
                }
            }

            return entity.Index.ToString(CultureInfo.InvariantCulture);
        }

        private string ApplyLineColor(EM_Component_Event debugEvent, string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            Entity colorEntity = ResolveLineColorEntity(debugEvent);

            if (colorEntity == Entity.Null)
                return text;

            if (!entityManager.Exists(colorEntity))
                return text;

            if (!entityManager.HasComponent<EM_Component_LogColor>(colorEntity))
                return text;

            EM_Component_LogColor logColor = entityManager.GetComponentData<EM_Component_LogColor>(colorEntity);
            Color color = new Color(logColor.Value.x, logColor.Value.y, logColor.Value.z, logColor.Value.w);
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            return string.Format(CultureInfo.InvariantCulture, "<color=#{0}>{1}</color>", hex, text);
        }

        private static Entity ResolveLineColorEntity(EM_Component_Event debugEvent)
        {
            if (debugEvent.Subject != Entity.Null)
                return debugEvent.Subject;

            if (debugEvent.Target != Entity.Null)
                return debugEvent.Target;

            return Entity.Null;
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Templates
        private string GetTemplate(EM_DebugEventType eventType)
        {
            EM_DebugMessageTemplates activeTemplates = templates;

            if (activeTemplates == null)
                return GetDefaultTemplate(eventType);

            switch (eventType)
            {
                case EM_DebugEventType.SignalEmitted:
                    return activeTemplates.SignalEmittedTemplate;

                case EM_DebugEventType.IntentCreated:
                    return activeTemplates.IntentCreatedTemplate;

                case EM_DebugEventType.EffectApplied:
                    return activeTemplates.EffectAppliedTemplate;

                case EM_DebugEventType.InteractionAttempt:
                    return activeTemplates.InteractionAttemptTemplate;

                case EM_DebugEventType.InteractionSuccess:
                    return activeTemplates.InteractionSuccessTemplate;

                case EM_DebugEventType.InteractionFail:
                    return activeTemplates.InteractionFailTemplate;

                case EM_DebugEventType.ScheduleWindow:
                    return activeTemplates.ScheduleWindowTemplate;

                case EM_DebugEventType.ScheduleEnd:
                    return activeTemplates.ScheduleEndTemplate;

                case EM_DebugEventType.ScheduleTick:
                    return activeTemplates.ScheduleTickTemplate;

                default:
                    return string.Empty;
            }
        }

        private string GetTimeLabelTemplate()
        {
            if (templates == null)
                return DefaultTimeLabelTemplate;

            return templates.TimeLabelTemplate;
        }

        private static string GetDefaultTemplate(EM_DebugEventType eventType)
        {
            switch (eventType)
            {
                case EM_DebugEventType.SignalEmitted:
                    return DefaultSignalEmittedTemplate;

                case EM_DebugEventType.IntentCreated:
                    return DefaultIntentCreatedTemplate;

                case EM_DebugEventType.EffectApplied:
                    return DefaultEffectAppliedTemplate;

                case EM_DebugEventType.InteractionAttempt:
                    return DefaultInteractionAttemptTemplate;

                case EM_DebugEventType.InteractionSuccess:
                    return DefaultInteractionSuccessTemplate;

                case EM_DebugEventType.InteractionFail:
                    return DefaultInteractionFailTemplate;

                case EM_DebugEventType.ScheduleWindow:
                    return DefaultScheduleWindowTemplate;

                case EM_DebugEventType.ScheduleEnd:
                    return DefaultScheduleEndTemplate;

                case EM_DebugEventType.ScheduleTick:
                    return DefaultScheduleTickTemplate;

                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
