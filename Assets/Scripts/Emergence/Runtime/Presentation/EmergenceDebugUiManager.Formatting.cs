using System.Globalization;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Formatting and template logic for the Emergence debug HUD.
    /// </summary>
    public sealed partial class EmergenceDebugUiManager
    {
        #region Formatting
        /// <summary>
        /// Formats a debug event into a log line string.
        /// </summary>
        private string FormatEvent(EmergenceDebugEvent debugEvent)
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
            string need = debugEvent.NeedId.Length > 0 ? debugEvent.NeedId.ToString() : "None";
            string resource = debugEvent.ResourceId.Length > 0 ? debugEvent.ResourceId.ToString() : "None";
            string window = debugEvent.WindowId.Length > 0 ? debugEvent.WindowId.ToString() : "None";
            string reason = debugEvent.Reason.Length > 0 ? debugEvent.Reason.ToString() : "None";
            string value = FormatValue(debugEvent.Value);

            return ApplyTemplate(template, timeString, subject, target, society, need, resource, window, reason, value);
        }

        /// <summary>
        /// Applies token replacements to a template string.
        /// </summary>
        private static string ApplyTemplate(string template, string time, string subject, string target, string society,
            string need, string resource, string window, string reason, string value)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string formatted = template;
            formatted = formatted.Replace("{time}", time);
            formatted = formatted.Replace("{subject}", subject);
            formatted = formatted.Replace("{target}", target);
            formatted = formatted.Replace("{society}", society);
            formatted = formatted.Replace("{need}", need);
            formatted = formatted.Replace("{resource}", resource);
            formatted = formatted.Replace("{window}", window);
            formatted = formatted.Replace("{reason}", reason);
            formatted = formatted.Replace("{value}", value);

            return formatted;
        }

        /// <summary>
        /// Formats a time-of-day float into HH:MM.
        /// </summary>
        private static string FormatTimeOfDay(float timeOfDay)
        {
            float wrapped = Mathf.Repeat(timeOfDay, 24f);
            int hours = Mathf.FloorToInt(wrapped);
            float minutesFloat = (wrapped - hours) * 60f;
            int minutes = Mathf.FloorToInt(minutesFloat);

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hours, minutes);
        }

        /// <summary>
        /// Formats an entity into a stable id string.
        /// </summary>
        private string FormatEntityId(Entity entity)
        {
            if (entity == Entity.Null)
                return "None";

            if (entityManager.Exists(entity))
            {
                if (entityManager.HasComponent<EmergenceDebugName>(entity))
                {
                    EmergenceDebugName debugName = entityManager.GetComponentData<EmergenceDebugName>(entity);

                    if (debugName.Value.Length > 0)
                    {
                        string name = debugName.Value.ToString();
                        string id = entity.Index.ToString(CultureInfo.InvariantCulture);
                        return string.Format(CultureInfo.InvariantCulture, "{0} (ID {1})", name, id);
                    }
                }
            }

            return entity.Index.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats numeric values for the log.
        /// </summary>
        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Templates
        /// <summary>
        /// Returns the active template for the given event type.
        /// </summary>
        private string GetTemplate(EmergenceDebugEventType eventType)
        {
            EM_DebugMessageTemplates activeTemplates = templates;

            if (activeTemplates == null)
                return GetDefaultTemplate(eventType);

            if (eventType == EmergenceDebugEventType.ScheduleWindow)
                return activeTemplates.ScheduleWindowTemplate;

            if (eventType == EmergenceDebugEventType.ScheduleTick)
                return activeTemplates.ScheduleTickTemplate;

            if (eventType == EmergenceDebugEventType.TradeAttempt)
                return activeTemplates.TradeAttemptTemplate;

            if (eventType == EmergenceDebugEventType.TradeSuccess)
                return activeTemplates.TradeSuccessTemplate;

            if (eventType == EmergenceDebugEventType.TradeFail)
                return activeTemplates.TradeFailTemplate;

            if (eventType == EmergenceDebugEventType.DistributionTransfer)
                return activeTemplates.DistributionTemplate;

            return string.Empty;
        }

        /// <summary>
        /// Returns the time label template.
        /// </summary>
        private string GetTimeLabelTemplate()
        {
            if (templates == null)
                return DefaultTimeLabelTemplate;

            return templates.TimeLabelTemplate;
        }

        /// <summary>
        /// Returns the fallback template for the event type.
        /// </summary>
        private static string GetDefaultTemplate(EmergenceDebugEventType eventType)
        {
            if (eventType == EmergenceDebugEventType.ScheduleWindow)
                return DefaultScheduleWindowTemplate;

            if (eventType == EmergenceDebugEventType.ScheduleTick)
                return DefaultScheduleTickTemplate;

            if (eventType == EmergenceDebugEventType.TradeAttempt)
                return DefaultTradeAttemptTemplate;

            if (eventType == EmergenceDebugEventType.TradeSuccess)
                return DefaultTradeSuccessTemplate;

            if (eventType == EmergenceDebugEventType.TradeFail)
                return DefaultTradeFailTemplate;

            if (eventType == EmergenceDebugEventType.DistributionTransfer)
                return DefaultDistributionTemplate;

            return string.Empty;
        }
        #endregion
    }
}
