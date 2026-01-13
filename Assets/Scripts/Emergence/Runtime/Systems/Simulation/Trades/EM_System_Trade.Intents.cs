using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Constants
        // Reason keys used in interaction debug events.
        private static readonly FixedString64Bytes ReasonNoResource = new FixedString64Bytes("NoResource");
        private static readonly FixedString64Bytes ReasonNoPartner = new FixedString64Bytes("NoPartner");
        private static readonly FixedString64Bytes ReasonRejected = new FixedString64Bytes("Rejected");
        private static readonly FixedString64Bytes ReasonProviderMissing = new FixedString64Bytes("ProviderMissing");
        private static readonly FixedString64Bytes ReasonProviderTimeout = new FixedString64Bytes("ProviderTimeout");
        private static readonly FixedString64Bytes ReasonQueueFull = new FixedString64Bytes("QueueFull");
        #endregion
    }
}
