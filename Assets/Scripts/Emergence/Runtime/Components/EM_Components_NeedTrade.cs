using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_BufferElement_NeedSetting : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public FixedList128Bytes<float> RatePerHourSamples;
        public float RateMultiplier;
        public float MinValue;
        public float MaxValue;
        public float RequestAmount;
        public float NeedSatisfactionPerUnit;
        #endregion
    }

    public struct EM_BufferElement_NeedActivityRate : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ActivityId;
        public FixedList128Bytes<float> RatePerHourSamples;
        #endregion
    }

    public struct EM_BufferElement_Intent : IBufferElementData
    {
        #region Data
        public FixedString64Bytes IntentId;
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public float Urgency;
        public float DesiredAmount;
        public double CreatedTime;
        public double NextAttemptTime;
        public double LastAttemptTime;
        public int AttemptCount;
        public Entity PreferredTarget;
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

    public struct EM_Component_NpcTradePreferences : IComponentData
    {
        #region Data
        public FixedList128Bytes<float> AffinityMultiplierSamples;
        public float MinMultiplier;
        public float MaxMultiplier;
        #endregion
    }
}
