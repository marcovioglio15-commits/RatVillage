using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_BufferElement_RuleCooldown : IBufferElementData
    {
        #region Data
        public int RuleIndex;
        public double NextAllowedTime;
        #endregion
    }
}
