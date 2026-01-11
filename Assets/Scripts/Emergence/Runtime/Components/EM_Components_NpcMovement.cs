using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_NpcMovementSettings : IComponentData
    {
        #region Data
        public float MaxSpeed;
        public float Acceleration;
        public float TurnSpeed;
        public float StopRadius;
        #endregion
    }

    public struct EM_Component_NpcNavigationState : IComponentData
    {
        #region Data
        public EM_NpcDestinationKind DestinationKind;
        public Entity DestinationAnchor;
        public float3 DestinationPosition;
        public int DestinationNodeIndex;
        public int PathIndex;
        public byte HasPath;
        public byte IsMoving;
        #endregion
    }

    public struct EM_Component_NpcLocationState : IComponentData
    {
        #region Data
        public int CurrentNodeIndex;
        public FixedString64Bytes CurrentLocationId;
        public Entity CurrentLocationAnchor;
        public Entity LastTradeAnchor;
        #endregion
    }

    public struct EM_Component_NpcMovementState : IComponentData
    {
        #region Data
        public float CurrentSpeed;
        #endregion
    }
    #endregion

    #region Buffers
    public struct EM_BufferElement_NpcPathNode : IBufferElementData
    {
        #region Data
        public int NodeIndex;
        #endregion
    }
    #endregion
}
