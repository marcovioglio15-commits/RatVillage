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
            string need = debugEvent.NeedId.Length > 0 ? debugEvent.NeedId.ToString() : "None";
            string resource = debugEvent.ResourceId.Length > 0 ? debugEvent.ResourceId.ToString() : "None";
            string window = debugEvent.WindowId.Length > 0 ? debugEvent.WindowId.ToString() : "None";
            string reason = debugEvent.Reason.Length > 0 ? debugEvent.Reason.ToString() : "None";
            string value = FormatValue(debugEvent.Value);

            return ApplyTemplate(template, timeString, subject, target, society, need, resource, window, reason, value);
        }

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

            if (entityManager.Exists(entity))
            {
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
            }

            return entity.Index.ToString(CultureInfo.InvariantCulture);
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
                case EM_DebugEventType.ScheduleWindow:
                    return activeTemplates.ScheduleWindowTemplate;

                case EM_DebugEventType.ScheduleTick:
                    return activeTemplates.ScheduleTickTemplate;

                case EM_DebugEventType.TradeAttempt:
                    return activeTemplates.TradeAttemptTemplate;

                case EM_DebugEventType.TradeSuccess:
                    return activeTemplates.TradeSuccessTemplate;

                case EM_DebugEventType.TradeFail:
                    return activeTemplates.TradeFailTemplate;

                case EM_DebugEventType.DistributionTransfer:
                    return activeTemplates.DistributionTemplate;

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
                case EM_DebugEventType.ScheduleWindow:
                    return DefaultScheduleWindowTemplate;

                case EM_DebugEventType.ScheduleTick:
                    return DefaultScheduleTickTemplate;

                case EM_DebugEventType.TradeAttempt:
                    return DefaultTradeAttemptTemplate;

                case EM_DebugEventType.TradeSuccess:
                    return DefaultTradeSuccessTemplate;

                case EM_DebugEventType.TradeFail:
                    return DefaultTradeFailTemplate;

                case EM_DebugEventType.DistributionTransfer:
                    return DefaultDistributionTemplate;

                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
