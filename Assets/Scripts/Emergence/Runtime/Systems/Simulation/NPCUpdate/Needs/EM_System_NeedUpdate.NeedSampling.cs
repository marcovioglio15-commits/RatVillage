using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NeedUpdate : ISystem
    {
        #region NeedSampling
        // Need rate curve sampling for activity time.
        #region RateSampling
        private static float SampleRatePerHour(in FixedList128Bytes<float> samples, float normalizedTime)
        {
            int count = samples.Length;

            if (count <= 0)
                return 0f;

            if (count == 1)
                return samples[0];

            float scaled = normalizedTime * (count - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, count - 1);
            float lerp = scaled - index;

            return math.lerp(samples[index], samples[nextIndex], lerp);
        }
        #endregion

        #region NeedLookup
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
        #endregion
    }
}
