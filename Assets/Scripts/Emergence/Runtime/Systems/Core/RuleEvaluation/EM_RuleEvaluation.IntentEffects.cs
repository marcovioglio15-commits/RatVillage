using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Intent
        // Add or update an intent buffer entry for the target.
        private static bool ApplyIntent(Entity target, FixedString64Bytes intentId, FixedString64Bytes resourceOverride,
            FixedString64Bytes contextId, float urgencyValue, double timeSeconds, ref BufferLookup<EM_BufferElement_Intent> intentLookup,
            ref BufferLookup<EM_BufferElement_NeedSetting> settingLookup, out float before, out float after,
            out bool created, out FixedString64Bytes needId, out FixedString64Bytes resourceId, out float desiredAmount)
        {
            before = 0f;
            after = 0f;
            created = false;
            needId = contextId;
            resourceId = resourceOverride;
            desiredAmount = 0f;

            if (intentId.Length == 0)
                return false;

            if (!intentLookup.HasBuffer(target))
                return false;

            if (needId.Length > 0)
            {
                float fallbackAmount;
                FixedString64Bytes fallbackResource = ResolveResourceId(target, needId, ref settingLookup, out fallbackAmount);

                if (resourceId.Length == 0)
                    resourceId = fallbackResource;

                if (desiredAmount <= 0f)
                    desiredAmount = fallbackAmount;
            }

            DynamicBuffer<EM_BufferElement_Intent> intents = intentLookup[target];

            for (int i = 0; i < intents.Length; i++)
            {
                EM_BufferElement_Intent intent = intents[i];

                if (!intent.IntentId.Equals(intentId))
                    continue;

                if (needId.Length > 0 && !intent.NeedId.Equals(needId))
                    continue;

                if (resourceId.Length > 0 && !intent.ResourceId.Equals(resourceId))
                    continue;

                before = intent.Urgency;
                intent.Urgency = math.clamp(urgencyValue, 0f, 1f);

                if (desiredAmount > 0f)
                    intent.DesiredAmount = desiredAmount;

                after = intent.Urgency;
                intents[i] = intent;
                return true;
            }

            float clampedUrgency = math.clamp(urgencyValue, 0f, 1f);

            if (clampedUrgency <= 0f)
                return false;

            EM_BufferElement_Intent newIntent = new EM_BufferElement_Intent
            {
                IntentId = intentId,
                NeedId = needId,
                ResourceId = resourceId,
                Urgency = clampedUrgency,
                DesiredAmount = desiredAmount,
                CreatedTime = timeSeconds,
                NextAttemptTime = timeSeconds,
                LastAttemptTime = -1d,
                AttemptCount = 0,
                PreferredTarget = Entity.Null
            };

            before = 0f;
            after = newIntent.Urgency;
            intents.Add(newIntent);
            created = true;
            return true;
        }

        // Resolve the resource id from the need settings for the target.
        private static FixedString64Bytes ResolveResourceId(Entity target, FixedString64Bytes needId,
            ref BufferLookup<EM_BufferElement_NeedSetting> settingLookup, out float desiredAmount)
        {
            desiredAmount = 0f;

            if (!settingLookup.HasBuffer(target))
                return default;

            DynamicBuffer<EM_BufferElement_NeedSetting> settings = settingLookup[target];

            for (int i = 0; i < settings.Length; i++)
            {
                if (!settings[i].NeedId.Equals(needId))
                    continue;

                desiredAmount = settings[i].RequestAmount;
                return settings[i].ResourceId;
            }

            return default;
        }
        #endregion
    }
}
