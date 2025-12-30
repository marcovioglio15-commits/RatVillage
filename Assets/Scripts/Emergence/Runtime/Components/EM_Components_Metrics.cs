using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_BufferElement_MetricAccumulator : IBufferElementData
    {
        #region Data
        public int MetricIndex;
        public float Sum;
        public float Min;
        public float Max;
        public float Last;
        public int Count;
        #endregion
    }

    public struct EM_BufferElement_MetricSample : IBufferElementData
    {
        #region Data
        public FixedString64Bytes MetricId;
        public float Value;
        public float NormalizedValue;
        public double Time;
        public Entity Subject;
        public Entity SocietyRoot;
        #endregion
    }

    public struct EM_BufferElement_MetricTimer : IBufferElementData
    {
        #region Data
        public int MetricIndex;
        public double NextSampleTime;
        #endregion
    }

    public struct EM_Component_MetricInitialized : IComponentData
    {
    }
}
