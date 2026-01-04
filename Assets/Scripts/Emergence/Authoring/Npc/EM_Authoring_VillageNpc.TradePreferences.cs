using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Trade Preferences
        // Samples the affinity multiplier curve into a fixed-size buffer.
        private const int AffinityMultiplierSamples = 31;

        private static void AddTradePreferences(AnimationCurve curve, float minMultiplier, float maxMultiplier,
            Entity entity, Baker<EM_Authoring_VillageNpc> baker)
        {
            EM_Component_NpcTradePreferences preferences = new EM_Component_NpcTradePreferences
            {
                AffinityMultiplierSamples = BuildAffinityMultiplierSamples(curve, minMultiplier, maxMultiplier),
                MinMultiplier = Mathf.Min(minMultiplier, maxMultiplier),
                MaxMultiplier = Mathf.Max(minMultiplier, maxMultiplier)
            };

            baker.AddComponent(entity, preferences);
        }

        private static FixedList128Bytes<float> BuildAffinityMultiplierSamples(AnimationCurve curve, float minMultiplier, float maxMultiplier)
        {
            FixedList128Bytes<float> samples = new FixedList128Bytes<float>();
            float minValue = Mathf.Min(minMultiplier, maxMultiplier);
            float maxValue = Mathf.Max(minMultiplier, maxMultiplier);

            for (int i = 0; i < AffinityMultiplierSamples; i++)
            {
                float t = AffinityMultiplierSamples > 1 ? (float)i / (AffinityMultiplierSamples - 1) : 0f;
                float value = EvaluateMultiplierCurve(curve, t);
                float clamped = Mathf.Clamp(value, minValue, maxValue);

                if (clamped < 0f)
                    clamped = 0f;

                samples.Add(clamped);
            }

            return samples;
        }

        private static float EvaluateMultiplierCurve(AnimationCurve curve, float t)
        {
            if (curve == null || curve.length == 0)
                return 1f;

            return curve.Evaluate(t);
        }
        #endregion
    }
}
