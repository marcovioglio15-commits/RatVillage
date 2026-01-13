using System.Globalization;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    internal static class EM_NarrativeLogFormatter
    {
        #region Formatting
        public static string ApplyTemplate(string template, EM_BufferElement_NarrativeSignal signal,
            EM_NarrativeEventType eventType, EntityManager entityManager)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string time = FormatTimeOfDay(signal.Time);
            string subject = FormatEntityName(signal.Subject, entityManager);
            string target = FormatEntityName(signal.Target, entityManager);
            string society = FormatEntityName(signal.Society, entityManager);
            string signalId = FormatId(signal.SignalId);
            string intent = FormatId(signal.IntentId);
            string effect = signal.EffectType.ToString();
            string context = FormatId(signal.ContextId);
            string need = FormatId(signal.NeedId);
            string resource = FormatId(signal.ResourceId);
            string activity = FormatId(signal.ActivityId);
            string reason = FormatId(signal.ReasonId);
            string value = FormatValue(signal.Value);
            string delta = FormatValue(signal.Delta);
            string before = FormatValue(signal.Before);
            string after = FormatValue(signal.After);

            string formatted = template;
            formatted = formatted.Replace("{time}", time);
            formatted = formatted.Replace("{subject}", subject);
            formatted = formatted.Replace("{target}", target);
            formatted = formatted.Replace("{society}", society);
            formatted = formatted.Replace("{signal}", signalId);
            formatted = formatted.Replace("{intent}", intent);
            formatted = formatted.Replace("{effect}", effect);
            formatted = formatted.Replace("{context}", context);
            formatted = formatted.Replace("{need}", need);
            formatted = formatted.Replace("{resource}", resource);
            formatted = formatted.Replace("{activity}", activity);
            formatted = formatted.Replace("{reason}", reason);
            formatted = formatted.Replace("{value}", value);
            formatted = formatted.Replace("{delta}", delta);
            formatted = formatted.Replace("{before}", before);
            formatted = formatted.Replace("{after}", after);
            formatted = formatted.Replace("{event}", eventType.ToString());
            return formatted;
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (maxLength <= 0)
                return string.Empty;

            if (value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength);
        }

        private static string FormatTimeOfDay(double timeHours)
        {
            if (timeHours < 0d)
                return "--:--";

            float wrapped = Mathf.Repeat((float)timeHours, 24f);
            int hours = Mathf.FloorToInt(wrapped);
            float minutesFloat = (wrapped - hours) * 60f;
            int minutes = Mathf.FloorToInt(minutesFloat);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hours, minutes);
        }

        private static string FormatEntityName(Entity entity, EntityManager entityManager)
        {
            if (entity == Entity.Null)
                return "None";

            string entityId = FormatEntityId(entity);

            if (!entityManager.Exists(entity))
                return entityId;

            if (entityManager.HasComponent<EM_Component_NpcType>(entity))
            {
                EM_Component_NpcType npcType = entityManager.GetComponentData<EM_Component_NpcType>(entity);

                string name = npcType.TypeId.Length > 0 ? FormatId(npcType.TypeId) : "NPC";
                return string.Format(CultureInfo.InvariantCulture, "{0} #{1}", name, entityId);
            }

            return entityId;
        }

        internal static string FormatId(FixedString64Bytes id)
        {
            if (id.Length == 0)
                return "None";

            string raw = id.ToString();
            string prettified = PrettifyId(raw);
            return string.IsNullOrEmpty(prettified) ? raw : prettified;
        }

        private static string PrettifyId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            string[] parts = id.Replace('_', '.').Split('.');
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (ShouldSkipToken(part))
                    continue;

                string spaced = InsertSpaces(part);
                string capitalized = CapitalizeWords(spaced);

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(capitalized);
            }

            return builder.ToString();
        }

        private static bool ShouldSkipToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return true;

            string lower = token.ToLowerInvariant();

            if (lower == "signal")
                return true;

            if (lower == "effect")
                return true;

            if (lower == "override")
                return true;

            if (lower == "intent")
                return true;

            if (lower == "metric")
                return true;

            if (lower == "need")
                return true;

            if (lower == "resource")
                return true;

            if (lower == "id")
                return true;

            if (lower == "npc")
                return true;

            return false;
        }

        private static string InsertSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            char previous = '\0';

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

                if (i > 0 && char.IsUpper(current) && char.IsLetter(previous) && char.IsLower(previous))
                    builder.Append(' ');

                builder.Append(current);
                previous = current;
            }

            return builder.ToString();
        }

        private static string CapitalizeWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            StringBuilder builder = new StringBuilder(input.Length);
            bool startWord = true;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

                if (char.IsWhiteSpace(current))
                {
                    builder.Append(current);
                    startWord = true;
                    continue;
                }

                if (startWord)
                {
                    builder.Append(char.ToUpperInvariant(current));
                    startWord = false;
                }
                else
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
            }

            return builder.ToString();
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatEntityId(Entity entity)
        {
            string index = entity.Index.ToString(CultureInfo.InvariantCulture);
            string version = entity.Version.ToString(CultureInfo.InvariantCulture);
            return index + ":" + version;
        }
        #endregion
    }
}
