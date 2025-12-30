using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

namespace EmergentMechanics
{
    public struct EM_BufferElement_NeedRule : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public FixedList128Bytes<float> RatePerHourSamples;
        public float MinValue;
        public float MaxValue;
        public float StartThreshold;
        public float MaxProbability;
        public float ProbabilityExponent;
        public float CooldownSeconds;
        public float ResourceTransferAmount;
        public float NeedSatisfactionAmount;
        #endregion
    }

    public struct EM_BufferElement_NeedResolutionState : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public double NextAttemptTime;
        #endregion
    }

    public struct EM_BufferElement_Relationship : IBufferElementData
    {
        #region Data
        public Entity Other;
        public float Affinity;
        #endregion
    }

    public struct EM_BufferElement_RelationshipType : IBufferElementData
    {
        #region Data
        public FixedString64Bytes TypeId;
        public float Affinity;
        #endregion
    }

    public struct EM_Component_NpcType : IComponentData
    {
        #region Data
        public FixedString64Bytes TypeId;
        #endregion
    }

    public struct EM_Component_RandomSeed : IComponentData
    {
        #region Data
        public uint Value;
        #endregion
    }
}
