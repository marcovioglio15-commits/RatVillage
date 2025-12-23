using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Helper methods for the trade system selection logic.
    /// </summary>
    public partial struct EmergenceTradeSystem
    {
        #region Helpers
        private static void EnsureResolutionStates(DynamicBuffer<EmergenceNeedRule> rules, DynamicBuffer<EmergenceNeedResolutionState> states)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                FixedString64Bytes needId = rules[i].NeedId;

                if (needId.Length == 0)
                    continue;

                int stateIndex = FindStateIndex(states, needId);

                if (stateIndex >= 0)
                    continue;

                EmergenceNeedResolutionState state = new EmergenceNeedResolutionState
                {
                    NeedId = needId,
                    NextAttemptTime = 0d
                };

                states.Add(state);
            }
        }

        private static bool SelectNeedRule(DynamicBuffer<EmergenceNeed> needs, DynamicBuffer<EmergenceNeedRule> rules,
            DynamicBuffer<EmergenceNeedResolutionState> states, double time, out int ruleIndex, out float probability, out int stateIndex)
        {
            ruleIndex = -1;
            probability = 0f;
            stateIndex = -1;

            for (int i = 0; i < rules.Length; i++)
            {
                EmergenceNeedRule rule = rules[i];

                if (rule.NeedId.Length == 0)
                    continue;

                int needIndex = FindNeedIndex(needs, rule.NeedId);

                if (needIndex < 0)
                    continue;

                int currentStateIndex = FindStateIndex(states, rule.NeedId);

                if (currentStateIndex < 0)
                    continue;

                if (time < states[currentStateIndex].NextAttemptTime)
                    continue;

                float currentProbability = ComputeProbability(needs[needIndex].Value, rule);

                if (currentProbability <= probability)
                    continue;

                probability = currentProbability;
                ruleIndex = i;
                stateIndex = currentStateIndex;
            }

            if (ruleIndex < 0)
                return false;

            if (probability <= 0f)
                return false;

            return true;
        }

        private static float ComputeProbability(float needValue, EmergenceNeedRule rule)
        {
            float maxValue = math.max(rule.MinValue, rule.MaxValue);
            float startThreshold = math.min(rule.StartThreshold, maxValue);
            float range = maxValue - startThreshold;

            if (range <= 0f)
                return 0f;

            float normalized = math.saturate((needValue - startThreshold) / range);
            float exponent = math.max(0.01f, rule.ProbabilityExponent);
            float scaled = math.pow(normalized, exponent);
            float probability = rule.MaxProbability * scaled;

            return math.saturate(probability);
        }

        private static int FindNeedIndex(DynamicBuffer<EmergenceNeed> needs, FixedString64Bytes needId)
        {
            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }

        private static int FindStateIndex(DynamicBuffer<EmergenceNeedResolutionState> states, FixedString64Bytes needId)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (!states[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }
        #endregion
    }
}
