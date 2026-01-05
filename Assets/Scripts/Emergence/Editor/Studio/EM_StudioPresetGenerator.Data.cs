using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Preset Tuning
        private struct EM_StudioPresetTuning
        {
            public float MetricIntervalMultiplier;
            public float EffectMagnitudeMultiplier;
            public float ScheduleTickMultiplier;
        }

        private static EM_StudioPresetTuning GetTuning(EM_StudioPresetType preset)
        {
            EM_StudioPresetTuning tuning = new EM_StudioPresetTuning
            {
                MetricIntervalMultiplier = 1f,
                EffectMagnitudeMultiplier = 1f,
                ScheduleTickMultiplier = 1f
            };

            if (preset == EM_StudioPresetType.Slow)
            {
                tuning.MetricIntervalMultiplier = 2f;
                tuning.EffectMagnitudeMultiplier = 0.75f;
                tuning.ScheduleTickMultiplier = 1.5f;
            }
            else if (preset == EM_StudioPresetType.Aggressive)
            {
                tuning.MetricIntervalMultiplier = 0.5f;
                tuning.EffectMagnitudeMultiplier = 1.25f;
                tuning.ScheduleTickMultiplier = 0.5f;
            }

            return tuning;
        }
        #endregion

        #region Curves
        private static AnimationCurve BuildUrgencyCurve()
        {
            Keyframe[] keys = new Keyframe[3];
            keys[0] = new Keyframe(0f, 0f);
            keys[1] = new Keyframe(0.6f, 0f);
            keys[2] = new Keyframe(1f, 1f);
            return new AnimationCurve(keys);
        }

        private static AnimationCurve BuildAlwaysCurve()
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0f, 1f);
            keys[1] = new Keyframe(1f, 1f);
            return new AnimationCurve(keys);
        }

        private static AnimationCurve BuildFlatCurve(float value)
        {
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0f, value);
            keys[1] = new Keyframe(1f, value);
            return new AnimationCurve(keys);
        }
        #endregion
    }
}
