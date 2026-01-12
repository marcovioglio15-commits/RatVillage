using Unity.Collections;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NarrativeLogAssembler
    {
        #region Templates
        private static bool IsTemplateMatch(EM_NarrativeTemplate template, EM_NarrativeEventType eventType,
            EM_BufferElement_NarrativeSignal signal, EM_NarrativeThresholds thresholds)
        {
            if (template.EventType != eventType)
                return false;

            if (template.Verbosity > thresholds.Verbosity)
                return false;

            if (!MatchesStringFilter(template.SignalIdEquals, signal.SignalId))
                return false;

            if (!MatchesPrefixFilter(template.SignalIdPrefix, signal.SignalId))
                return false;

            if (!MatchesStringFilter(template.NeedIdEquals, signal.NeedId))
                return false;

            if (!MatchesStringFilter(template.ResourceIdEquals, signal.ResourceId))
                return false;

            if (!MatchesStringFilter(template.ActivityIdEquals, signal.ActivityId))
                return false;

            if (!MatchesStringFilter(template.ContextIdEquals, signal.ContextId))
                return false;

            if (!MatchesStringFilter(template.ReasonIdEquals, signal.ReasonId))
                return false;

            if (template.UseEffectType && signal.EffectType != template.EffectType)
                return false;

            if (template.UseMinValue && signal.Value < template.MinValue)
                return false;

            if (template.UseMaxValue && signal.Value > template.MaxValue)
                return false;

            if (template.UseMinDelta && signal.Delta < template.MinDelta)
                return false;

            if (template.UseMaxDelta && signal.Delta > template.MaxDelta)
                return false;

            if (template.UseMinAfter && signal.After < template.MinAfter)
                return false;

            if (template.UseMaxAfter && signal.After > template.MaxAfter)
                return false;

            return true;
        }

        private static bool MatchesStringFilter(string expected, FixedString64Bytes actual)
        {
            if (string.IsNullOrEmpty(expected))
                return true;

            if (actual.Length == 0)
                return false;

            return actual.ToString() == expected;
        }

        private static bool MatchesPrefixFilter(string prefix, FixedString64Bytes actual)
        {
            if (string.IsNullOrEmpty(prefix))
                return true;

            if (actual.Length == 0)
                return false;

            string value = actual.ToString();
            return value.StartsWith(prefix, System.StringComparison.Ordinal);
        }

        private static int SelectTemplateIndex(EM_NarrativeTemplate[] templates, System.Collections.Generic.List<int> matches)
        {
            if (matches.Count == 1)
                return matches[0];

            float totalWeight = 0f;

            for (int i = 0; i < matches.Count; i++)
            {
                float weight = templates[matches[i]].Weight;

                if (weight <= 0f)
                    weight = 1f;

                totalWeight += weight;
            }

            if (totalWeight <= 0f)
                return matches[0];

            float pick = Random.value * totalWeight;
            float cursor = 0f;

            for (int i = 0; i < matches.Count; i++)
            {
                EM_NarrativeTemplate template = templates[matches[i]];
                float weight = template.Weight;

                if (weight <= 0f)
                    weight = 1f;

                cursor += weight;

                if (pick <= cursor)
                    return matches[i];
            }

            return matches[0];
        }
        #endregion
    }
}
