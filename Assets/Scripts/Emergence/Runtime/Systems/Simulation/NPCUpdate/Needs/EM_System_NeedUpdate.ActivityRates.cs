using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_NeedUpdate : ISystem
    {
        #region ActivityRates
        // Resolve per-activity rate samples for a need.
        private static bool TryGetActivityRateSamples(FixedString64Bytes needId, FixedString64Bytes activityId,
            DynamicBuffer<EM_BufferElement_NeedActivityRate> activityRates, out FixedList128Bytes<float> samples)
        {
            samples = default;

            if (needId.Length == 0 || activityId.Length == 0)
                return false;

            for (int i = 0; i < activityRates.Length; i++)
            {
                if (!activityRates[i].NeedId.Equals(needId))
                    continue;

                if (!activityRates[i].ActivityId.Equals(activityId))
                    continue;

                samples = activityRates[i].RatePerHourSamples;
                return true;
            }

            return false;
        }
        #endregion
    }
}
