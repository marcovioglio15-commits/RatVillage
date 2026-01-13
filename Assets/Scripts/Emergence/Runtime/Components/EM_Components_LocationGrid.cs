using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_LocationGrid : IComponentData
    {
        #region Data
        public int Width;
        public int Height;
        public float NodeSize;
        public float3 Origin;
        #endregion
    }

    public struct EM_Component_LocationAnchor : IComponentData
    {
        #region Data
        public Entity Grid;
        public int NodeIndex;
        public FixedString64Bytes LocationId;
        public float QueueRadius;
        public int QueueSlotCount;
        #endregion
    }

    public struct EM_BufferElement_LocationNode : IBufferElementData
    {
        #region Data
        public byte Walkable;
        public FixedString64Bytes LocationId;
        #endregion
    }

    public struct EM_BufferElement_LocationOccupancy : IBufferElementData
    {
        #region Data
        public Entity Occupant;
        #endregion
    }

    public struct EM_BufferElement_LocationReservation : IBufferElementData
    {
        #region Data
        public Entity ReservedBy;
        public double ReservedUntilTimeSeconds;
        #endregion
    }
    #endregion
}
