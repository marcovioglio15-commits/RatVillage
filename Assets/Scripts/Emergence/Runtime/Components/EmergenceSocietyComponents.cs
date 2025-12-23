using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Marks an entity as the root of a society.
    /// </summary>
    public struct EmergenceSocietyRoot : IComponentData
    {
    }

    /// <summary>
    /// Associates an entity with a society root.
    /// </summary>
    public struct EmergenceSocietyMember : IComponentData
    {
        #region Data
        public Entity SocietyRoot;
        #endregion
    }

    /// <summary>
    /// Stores the LOD tier used for emergence processing.
    /// </summary>
    public struct EmergenceSocietyLod : IComponentData
    {
        #region Data
        public EmergenceLodTier Tier;
        #endregion
    }

    /// <summary>
    /// Holds the compiled society profile blob asset.
    /// </summary>
    public struct EmergenceSocietyProfileReference : IComponentData
    {
        #region Data
        public BlobAssetReference<EmergenceSocietyProfileBlob> Value;
        #endregion
    }

    /// <summary>
    /// Represents a need value for an entity.
    /// </summary>
    public struct EmergenceNeed : IBufferElementData
    {
        #region Data
        public FixedString64Bytes NeedId;
        public float Value;
        #endregion
    }

    /// <summary>
    /// Represents a resource amount for an entity.
    /// </summary>
    public struct EmergenceResource : IBufferElementData
    {
        #region Data
        public FixedString64Bytes ResourceId;
        public float Amount;
        #endregion
    }

    /// <summary>
    /// Represents a reputation value for an entity.
    /// </summary>
    public struct EmergenceReputation : IComponentData
    {
        #region Data
        public float Value;
        #endregion
    }

    /// <summary>
    /// Represents a cohesion value for a society.
    /// </summary>
    public struct EmergenceCohesion : IComponentData
    {
        #region Data
        public float Value;
        #endregion
    }

    /// <summary>
    /// Represents a population count for a society.
    /// </summary>
    public struct EmergencePopulation : IComponentData
    {
        #region Data
        public int Value;
        #endregion
    }
}
