using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    #region Enums
    public enum EM_TradeRequestStage : byte
    {
        None = 0,
        Traveling = 1,
        Queued = 2,
        Ready = 3
    }
    #endregion

    #region Components
    public struct EM_Component_NpcTradeInteraction : IComponentData
    {
        #region Data
        public float InteractionDistance;
        public float WaitSeconds;
        public byte AllowMidwayTradeAnywhere;
        #endregion
    }

    public struct EM_Component_TradeRequestState : IComponentData
    {
        #region Data
        public FixedString64Bytes IntentId;
        public FixedString64Bytes NeedId;
        public FixedString64Bytes ResourceId;
        public float DesiredAmount;
        public float Urgency;
        public Entity Provider;
        public Entity TargetAnchor;
        public double StartTimeSeconds;
        public double WaitStartTimeSeconds;
        public int QueueSlotIndex;
        public int QueueSlotNodeIndex;
        public byte IsOverrideRequest;
        public EM_TradeRequestStage Stage;
        #endregion
    }

    public struct EM_Component_TradeProviderState : IComponentData
    {
        #region Data
        public Entity ActiveRequester;
        public double BusyUntilSeconds;
        #endregion
    }
    #endregion

    #region Buffers
    public struct EM_BufferElement_TradeAttemptedProvider : IBufferElementData
    {
        #region Data
        public Entity Provider;
        #endregion
    }

    public struct EM_BufferElement_TradeQueueEntry : IBufferElementData
    {
        #region Data
        public Entity Requester;
        public double EnqueueTimeSeconds;
        public int SlotIndex;
        #endregion
    }
    #endregion
}
