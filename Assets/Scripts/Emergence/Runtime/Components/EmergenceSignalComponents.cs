using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Signal event emitted by sources and processed by rule systems.
    /// </summary>
    public struct EmergenceSignalEvent : IBufferElementData
    {
        #region Data
        public FixedString64Bytes SignalId;
        public float Value;
        public Entity Target;
        public EmergenceLodTier LodTier;
        public double Time;
        #endregion
    }

    /// <summary>
    /// Marks an entity as a source of signal events.
    /// </summary>
    public struct EmergenceSignalEmitter : IComponentData
    {
    }
}
