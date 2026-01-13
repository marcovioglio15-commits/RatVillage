using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    #region Components
    public struct EM_Component_NpcActivityTargetSettings : IComponentData
    {
        #region Data
        public float ReservationTimeoutSeconds;
        public float RecheckIntervalSeconds;
        #endregion
    }

    public struct EM_Component_NpcActivityTargetState : IComponentData
    {
        #region Data
        public FixedString64Bytes LocationId;
        public int ReservedNodeIndex;
        public int ApproachNodeIndex;
        public double ReservationExpiryTimeSeconds;
        public double NextRecheckTimeSeconds;
        public byte HasReservation;
        public byte IsWaitingForSlot;
        #endregion
    }
    #endregion
}
