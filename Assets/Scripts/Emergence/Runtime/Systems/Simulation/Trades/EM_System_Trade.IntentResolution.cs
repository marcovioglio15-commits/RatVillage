using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region IntentResolution
        #region NeedResolution
        private static bool ResolveNeedData(EM_BufferElement_Intent intent, DynamicBuffer<EM_BufferElement_NeedSetting> settings,
            out NeedResolutionData data)
        {
            data = default;
            FixedString64Bytes needId = intent.NeedId;
            FixedString64Bytes resourceId = intent.ResourceId;

            if (needId.Length == 0 && resourceId.Length == 0)
                return false;

            for (int i = 0; i < settings.Length; i++)
            {
                if (needId.Length > 0 && !settings[i].NeedId.Equals(needId))
                    continue;

                data.NeedId = settings[i].NeedId;
                data.ResourceId = resourceId.Length > 0 ? resourceId : settings[i].ResourceId;
                data.MinValue = math.min(settings[i].MinValue, settings[i].MaxValue);
                data.MaxValue = math.max(settings[i].MinValue, settings[i].MaxValue);
                data.RequestAmount = intent.DesiredAmount > 0f ? intent.DesiredAmount : settings[i].RequestAmount;
                data.NeedSatisfactionPerUnit = settings[i].NeedSatisfactionPerUnit > 0f ? settings[i].NeedSatisfactionPerUnit : 1f;

                return data.ResourceId.Length > 0;
            }

            if (resourceId.Length == 0)
                return false;

            data.NeedId = needId;
            data.ResourceId = resourceId;
            data.MinValue = 0f;
            data.MaxValue = 1f;
            data.RequestAmount = intent.DesiredAmount;
            data.NeedSatisfactionPerUnit = 1f;
            return true;
        }

        private static bool TryGetNeedValue(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId, out float value)
        {
            value = 0f;

            if (needId.Length == 0)
                return false;

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                value = needs[i].Value;
                return true;
            }

            return false;
        }

        private static float ResolveNeedUrgency(DynamicBuffer<EM_BufferElement_Need> needs, NeedResolutionData data)
        {
            float value;
            bool found = TryGetNeedValue(needs, data.NeedId, out value);

            if (!found)
                return 0f;

            float range = data.MaxValue - data.MinValue;

            if (range <= 0f)
                return 0f;

            return math.saturate((value - data.MinValue) / range);
        }

        private static float ResolveRemainingNeedAmount(DynamicBuffer<EM_BufferElement_Need> needs, NeedResolutionData data)
        {
            float value;
            bool found = TryGetNeedValue(needs, data.NeedId, out value);

            if (!found)
                return data.RequestAmount;

            float remainingValue = math.max(0f, value - data.MinValue);

            if (data.NeedSatisfactionPerUnit <= 0f)
                return remainingValue;

            return remainingValue / data.NeedSatisfactionPerUnit;
        }
        #endregion

        #region IntentUpdates
        private static void RemoveIntentAt(int intentIndex, DynamicBuffer<EM_BufferElement_Intent> intents)
        {
            if (intentIndex < 0 || intentIndex >= intents.Length)
                return;

            intents.RemoveAt(intentIndex);
        }

        private static bool ApplyIntentFailure(int intentIndex, DynamicBuffer<EM_BufferElement_Intent> intents, IntentPolicy policy,
            double timeSeconds, ref EM_Component_RandomSeed seed, out EM_BufferElement_Intent intent)
        {
            intent = default;

            if (intentIndex < 0 || intentIndex >= intents.Length)
                return false;

            intent = intents[intentIndex];
            int nextAttemptCount = intent.AttemptCount + 1;

            if (policy.MaxAttempts > 0 && nextAttemptCount > policy.MaxAttempts)
            {
                intents.RemoveAt(intentIndex);
                return false;
            }

            intent.AttemptCount = nextAttemptCount;
            intent.LastAttemptTime = timeSeconds;
            intent.NextAttemptTime = timeSeconds + ResolveIntentBackoffSeconds(policy, nextAttemptCount, ref seed);
            intents[intentIndex] = intent;
            return true;
        }

        private static void UpdateIntentAfterProgress(int intentIndex, DynamicBuffer<EM_BufferElement_Intent> intents,
            float remainingAmount, double timeSeconds, float urgency)
        {
            if (intentIndex < 0 || intentIndex >= intents.Length)
                return;

            EM_BufferElement_Intent intent = intents[intentIndex];
            intent.DesiredAmount = math.max(0f, remainingAmount);
            intent.Urgency = math.clamp(urgency, 0f, 1f);
            intent.AttemptCount = 0;
            intent.LastAttemptTime = timeSeconds;
            intent.NextAttemptTime = timeSeconds;
            intents[intentIndex] = intent;
        }

        private static void UpdateIntentAfterFailure(int intentIndex, DynamicBuffer<EM_BufferElement_Intent> intents, float remainingAmount,
            float urgency, IntentPolicy policy, double timeSeconds, ref EM_Component_RandomSeed seed)
        {
            EM_BufferElement_Intent intent;
            bool alive = ApplyIntentFailure(intentIndex, intents, policy, timeSeconds, ref seed, out intent);

            if (!alive)
                return;

            intent.DesiredAmount = math.max(0f, remainingAmount);
            intent.Urgency = math.clamp(urgency, 0f, 1f);
            intents[intentIndex] = intent;
        }

        private static double ResolveIntentBackoffSeconds(IntentPolicy policy, int attemptCount, ref EM_Component_RandomSeed seed)
        {
            if (policy.BackoffBaseSeconds <= 0d)
                return 0d;

            double backoffSeconds = policy.BackoffBaseSeconds * (1d + attemptCount);

            if (policy.BackoffMaxSeconds > 0d && backoffSeconds > policy.BackoffMaxSeconds)
                backoffSeconds = policy.BackoffMaxSeconds;

            if (policy.BackoffJitterSeconds > 0d)
                backoffSeconds += policy.BackoffJitterSeconds * NextRandom01(ref seed);

            return backoffSeconds;
        }
        #endregion

        #region Data
        private struct NeedResolutionData
        {
            public FixedString64Bytes NeedId;
            public FixedString64Bytes ResourceId;
            public float MinValue;
            public float MaxValue;
            public float RequestAmount;
            public float NeedSatisfactionPerUnit;
        }
        #endregion
        #endregion
    }
}
