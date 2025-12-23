using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Helper methods for society simulation authoring.
    /// </summary>
    public sealed partial class EmergenceSocietySimulationAuthoring
    {
        #region Helpers
        private static BlobAssetReference<EmergenceScheduleCurveBlob> BuildScheduleCurveBlob(EmergenceSocietySimulationAuthoring authoring)
        {
            int sampleCount = GetSampleCount(authoring.scheduleCurveSamples);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref EmergenceScheduleCurveBlob root = ref builder.ConstructRoot<EmergenceScheduleCurveBlob>();
            root.SampleCount = sampleCount;

            BlobBuilderArray<float> sleepSamples = builder.Allocate(ref root.SleepCurve, sampleCount);
            BlobBuilderArray<float> workSamples = builder.Allocate(ref root.WorkCurve, sampleCount);
            BlobBuilderArray<float> leisureSamples = builder.Allocate(ref root.LeisureCurve, sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount > 1 ? (float)i / (sampleCount - 1) : 0f;
                sleepSamples[i] = EvaluateCurve(authoring.sleepCurve, t);
                workSamples[i] = EvaluateCurve(authoring.workCurve, t);
                leisureSamples[i] = EvaluateCurve(authoring.leisureCurve, t);
            }

            BlobAssetReference<EmergenceScheduleCurveBlob> curveBlob = builder.CreateBlobAssetReference<EmergenceScheduleCurveBlob>(Allocator.Persistent);
            builder.Dispose();

            return curveBlob;
        }

        private static int GetSampleCount(int value)
        {
            if (value < 4)
                return 4;

            if (value > 128)
                return 128;

            return value;
        }

        private static float EvaluateCurve(AnimationCurve curve, float t)
        {
            if (curve == null)
                return Mathf.Clamp01(t);

            return Mathf.Clamp01(curve.Evaluate(t));
        }

        private static void AddSocietyResources(ResourceEntry[] source, ref DynamicBuffer<EmergenceResource> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i].ResourceId))
                    continue;

                EmergenceResource resource = new EmergenceResource
                {
                    ResourceId = new FixedString64Bytes(source[i].ResourceId),
                    Amount = source[i].Amount
                };

                buffer.Add(resource);
            }
        }

        private static FixedString64Bytes ToFixed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new FixedString64Bytes(string.Empty);

            return new FixedString64Bytes(value);
        }
        #endregion
    }
}
