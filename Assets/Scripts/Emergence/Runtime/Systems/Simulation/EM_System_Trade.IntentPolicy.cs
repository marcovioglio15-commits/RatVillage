using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region IntentPolicy
        private const float DefaultMinIntentUrgency = 0.6f;
        private const float DefaultMinIntentUrgencyToKeep = 0.2f;
        private const float DefaultIntentBackoffHours = 0.25f;
        private const float DefaultIntentBackoffMaxHours = 1f;
        private const float DefaultIntentBackoffJitterHours = 0.05f;
        private const int DefaultIntentMaxAttempts = 4;
        private const int DefaultProviderAttemptsPerTick = 3;
        private const int MaxProviderAttemptsPerTickCap = 8;

        private readonly struct IntentPolicy
        {
            public readonly float MinUrgency;
            public readonly float MinUrgencyToKeep;
            public readonly double BackoffBaseSeconds;
            public readonly double BackoffMaxSeconds;
            public readonly double BackoffJitterSeconds;
            public readonly int MaxAttempts;
            public readonly int MaxProviderAttemptsPerTick;
            public readonly bool ConsumeOnResolve;
            public readonly bool ConsumeInventoryFirst;
            public readonly bool ClampTransferToNeed;
            public readonly bool LockProviderPerTick;

            public IntentPolicy(float minUrgency, float minUrgencyToKeep, double backoffBaseSeconds, double backoffMaxSeconds,
                double backoffJitterSeconds, int maxAttempts, int maxProviderAttemptsPerTick, bool consumeOnResolve, bool consumeInventoryFirst,
                bool clampTransferToNeed, bool lockProviderPerTick)
            {
                MinUrgency = minUrgency;
                MinUrgencyToKeep = minUrgencyToKeep;
                BackoffBaseSeconds = backoffBaseSeconds;
                BackoffMaxSeconds = backoffMaxSeconds;
                BackoffJitterSeconds = backoffJitterSeconds;
                MaxAttempts = maxAttempts;
                MaxProviderAttemptsPerTick = maxProviderAttemptsPerTick;
                ConsumeOnResolve = consumeOnResolve;
                ConsumeInventoryFirst = consumeInventoryFirst;
                ClampTransferToNeed = clampTransferToNeed;
                LockProviderPerTick = lockProviderPerTick;
            }
        }

        private static IntentPolicy ResolveIntentPolicy(EM_Component_TradeSettings settings)
        {
            float minUrgency = settings.MinIntentUrgency;

            if (minUrgency < 0f)
                minUrgency = DefaultMinIntentUrgency;

            float minUrgencyToKeep = settings.MinIntentUrgencyToKeep;

            if (minUrgencyToKeep < 0f)
                minUrgencyToKeep = DefaultMinIntentUrgencyToKeep;

            double backoffBaseSeconds = ResolveHoursToSeconds(settings.IntentBackoffHours, DefaultIntentBackoffHours);
            double backoffMaxSeconds = ResolveHoursToSeconds(settings.IntentBackoffMaxHours, DefaultIntentBackoffMaxHours);
            double backoffJitterSeconds = ResolveHoursToSeconds(settings.IntentBackoffJitterHours, DefaultIntentBackoffJitterHours);

            if (backoffMaxSeconds > 0d && backoffMaxSeconds < backoffBaseSeconds)
                backoffMaxSeconds = backoffBaseSeconds;

            int maxAttempts = settings.IntentMaxAttempts;

            if (maxAttempts < 0)
                maxAttempts = DefaultIntentMaxAttempts;

            int maxProviderAttempts = settings.MaxProviderAttemptsPerTick;

            if (maxProviderAttempts <= 0)
                maxProviderAttempts = DefaultProviderAttemptsPerTick;

            if (maxProviderAttempts > MaxProviderAttemptsPerTickCap)
                maxProviderAttempts = MaxProviderAttemptsPerTickCap;

            bool consumeOnResolve = settings.ConsumeResourceOnResolve != 0;
            bool consumeInventoryFirst = settings.ConsumeInventoryFirst != 0;
            bool clampTransferToNeed = settings.ClampTransferToNeed != 0;
            bool lockProviderPerTick = settings.LockProviderPerTick != 0;

            return new IntentPolicy(minUrgency, minUrgencyToKeep, backoffBaseSeconds, backoffMaxSeconds, backoffJitterSeconds, maxAttempts,
                maxProviderAttempts, consumeOnResolve, consumeInventoryFirst, clampTransferToNeed, lockProviderPerTick);
        }

        private static double ResolveHoursToSeconds(float hoursValue, float fallbackHours)
        {
            float hours = hoursValue;

            if (hours < 0f)
                hours = fallbackHours;

            if (hours <= 0f)
                return 0d;

            return hours * 3600d;
        }

        private static bool SelectBestIntent(DynamicBuffer<EM_BufferElement_Intent> intents, double timeSeconds, float minUrgency,
            ref EM_Blob_NpcScheduleEntry tradeEntry, out int intentIndex, out EM_BufferElement_Intent intent)
        {
            intentIndex = -1;
            intent = default;
            float bestUrgency = 0f;
            float urgencyThreshold = minUrgency;
            EM_ScheduleTradePolicy policy = (EM_ScheduleTradePolicy)tradeEntry.TradePolicy;

            if (urgencyThreshold <= 0f)
                urgencyThreshold = DefaultMinIntentUrgency;

            if (policy == EM_ScheduleTradePolicy.BlockAll)
                return false;

            for (int i = 0; i < intents.Length; i++)
            {
                EM_BufferElement_Intent current = intents[i];

                if (timeSeconds < current.NextAttemptTime)
                    continue;

                if (current.NeedId.Length == 0)
                    continue;

                if (current.Urgency < urgencyThreshold)
                    continue;

                if (policy == EM_ScheduleTradePolicy.AllowOnlyListed &&
                    !IsNeedAllowed(current.NeedId, ref tradeEntry.AllowedTradeNeedIds))
                    continue;

                if (current.Urgency <= bestUrgency)
                    continue;

                bestUrgency = current.Urgency;
                intentIndex = i;
                intent = current;
            }

            return intentIndex >= 0;
        }

        private static void PruneIntents(DynamicBuffer<EM_BufferElement_Intent> intents, DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_NeedSetting> settings, float minUrgencyToKeep)
        {
            if (intents.Length == 0)
                return;

            float minKeep = minUrgencyToKeep;

            if (minKeep <= 0f)
                minKeep = DefaultMinIntentUrgencyToKeep;

            for (int i = intents.Length - 1; i >= 0; i--)
            {
                EM_BufferElement_Intent intent = intents[i];

                if (intent.IntentId.Length == 0 || intent.NeedId.Length == 0)
                {
                    intents.RemoveAt(i);
                    continue;
                }

                NeedResolutionData needData;
                bool hasNeedData = ResolveNeedData(intent, settings, out needData);

                if (!hasNeedData)
                {
                    intents.RemoveAt(i);
                    continue;
                }

                float urgency = ResolveNeedUrgency(needs, needData);
                intent.Urgency = urgency;

                if (intent.ResourceId.Length == 0 && needData.ResourceId.Length > 0)
                    intent.ResourceId = needData.ResourceId;

                if (intent.DesiredAmount <= 0f && needData.RequestAmount > 0f)
                    intent.DesiredAmount = needData.RequestAmount;

                intents[i] = intent;

                if (urgency < minKeep)
                    intents.RemoveAt(i);
            }
        }

        private static bool IsNeedAllowed(FixedString64Bytes needId, ref BlobArray<FixedString64Bytes> allowedNeeds)
        {
            for (int i = 0; i < allowedNeeds.Length; i++)
            {
                if (allowedNeeds[i].Equals(needId))
                    return true;
            }

            return false;
        }

        private static bool ContainsEntity(Entity entity, ref FixedList128Bytes<Entity> entities)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == entity)
                    return true;
            }

            return false;
        }

        private static int FindEntryByActivityId(ref BlobArray<EM_Blob_NpcScheduleEntry> entries, FixedString64Bytes activityId)
        {
            if (activityId.Length == 0)
                return -1;

            for (int i = 0; i < entries.Length; i++)
            {
                ref EM_Blob_NpcScheduleEntry entry = ref entries[i];

                if (entry.ActivityId.Equals(activityId))
                    return i;
            }

            return -1;
        }
        #endregion
    }
}
