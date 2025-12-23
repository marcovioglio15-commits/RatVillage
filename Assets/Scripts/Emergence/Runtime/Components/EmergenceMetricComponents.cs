using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Captures a sampled metric value for diagnostics.
    /// </summary>
    public struct EmergenceMetricSample : IBufferElementData
    {
        #region Data
        public FixedString64Bytes MetricId;
        public float Value;
        public double Time;
        public Entity Society;
        #endregion
    }

    /// <summary>
    /// Stores the next sample time for a metric.
    /// </summary>
    public struct EmergenceMetricTimer : IBufferElementData
    {
        #region Data
        public int MetricIndex;
        public double NextSampleTime;
        #endregion
    }
}
