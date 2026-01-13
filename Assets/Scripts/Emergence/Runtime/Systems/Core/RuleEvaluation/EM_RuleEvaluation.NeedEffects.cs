using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Need
        // Apply a delta to a need buffer entry with clamp support.
        private static bool ApplyNeedDelta(Entity target, FixedString64Bytes needId, float delta,
            ref BufferLookup<EM_BufferElement_Need> needLookup, ref BufferLookup<EM_BufferElement_NeedSetting> settingLookup,
            out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (needId.Length == 0)
                return false;

            if (!needLookup.HasBuffer(target))
                return false;

            DynamicBuffer<EM_BufferElement_Need> needs = needLookup[target];
            float minValue;
            float maxValue;
            bool hasClamp = TryGetNeedClamp(target, needId, ref settingLookup, out minValue, out maxValue);
            int index = FindNeedIndex(needs, needId);

            if (index < 0)
            {
                float baseValue = hasClamp ? minValue : 0f;
                before = baseValue;
                after = baseValue + delta;

                if (hasClamp)
                    after = math.clamp(after, minValue, maxValue);

                needs.Add(new EM_BufferElement_Need
                {
                    NeedId = needId,
                    Value = after
                });

                return true;
            }

            EM_BufferElement_Need entry = needs[index];
            before = entry.Value;
            after = entry.Value + delta;

            if (hasClamp)
                after = math.clamp(after, minValue, maxValue);

            entry.Value = after;
            needs[index] = entry;
            return true;
        }

        // Resolve clamp bounds for a specific need id.
        private static bool TryGetNeedClamp(Entity target, FixedString64Bytes needId,
            ref BufferLookup<EM_BufferElement_NeedSetting> settingLookup, out float minValue, out float maxValue)
        {
            minValue = 0f;
            maxValue = 0f;

            if (!settingLookup.HasBuffer(target))
                return false;

            DynamicBuffer<EM_BufferElement_NeedSetting> settings = settingLookup[target];

            for (int i = 0; i < settings.Length; i++)
            {
                if (!settings[i].NeedId.Equals(needId))
                    continue;

                minValue = math.min(settings[i].MinValue, settings[i].MaxValue);
                maxValue = math.max(settings[i].MinValue, settings[i].MaxValue);
                return true;
            }

            return false;
        }

        // Locate the index of a need entry by id.
        private static int FindNeedIndex(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId)
        {
            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }
        #endregion
    }
}
