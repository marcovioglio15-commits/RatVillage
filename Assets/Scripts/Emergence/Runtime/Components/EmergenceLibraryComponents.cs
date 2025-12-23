using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Holds the compiled emergence library blob asset.
    /// </summary>
    public struct EmergenceLibraryReference : IComponentData
    {
        #region Data
        public BlobAssetReference<EmergenceLibraryBlob> Value;
        #endregion
    }

    /// <summary>
    /// Stores global emergence settings used by runtime systems.
    /// </summary>
    public struct EmergenceGlobalSettings : IComponentData
    {
        #region Data
        public int MaxSignalQueue;
        public float FullSimTickRate;
        public float SimplifiedSimTickRate;
        public float AggregatedSimTickRate;
        #endregion
    }

    /// <summary>
    /// Tracks next tick times for each LOD tier.
    /// </summary>
    public struct EmergenceTierTickState : IComponentData
    {
        #region Data
        public double NextFullTick;
        public double NextSimplifiedTick;
        public double NextAggregatedTick;
        #endregion
    }
}
