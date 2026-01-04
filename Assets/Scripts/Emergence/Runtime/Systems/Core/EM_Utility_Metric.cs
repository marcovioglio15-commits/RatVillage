using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static class EM_Utility_Metric
    {
        #region Utility
        // Normalizes metric values into 0-1 space based on the configured policy.
        public static float Normalize(EmergenceMetricNormalization normalization, float value)
        {
            if (normalization == EmergenceMetricNormalization.Invert01)
                return 1f - math.clamp(value, 0f, 1f);

            if (normalization == EmergenceMetricNormalization.Signed01)
                return math.clamp((value * 0.5f) + 0.5f, 0f, 1f);

            if (normalization == EmergenceMetricNormalization.Abs01)
                return math.clamp(math.abs(value), 0f, 1f);

            return math.clamp(value, 0f, 1f);
        }

        // Ensures a valid sampling interval in seconds.
        public static float GetInterval(float interval)
        {
            if (interval <= 0f)
                return 1f;

            return interval;
        }
        #endregion
    }
}
