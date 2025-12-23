using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Defines spawn parameters for NPC instantiation.
    /// </summary>
    public struct EmergenceNpcSpawner : IComponentData
    {
        #region Data
        public Entity Prefab;
        public Entity SocietyRoot;
        public int Count;
        public float Radius;
        public float Height;
        public uint Seed;
        public FixedString64Bytes DebugNamePrefix;
        #endregion
    }

    /// <summary>
    /// Tracks whether a spawner has already executed.
    /// </summary>
    public struct EmergenceNpcSpawnerState : IComponentData
    {
        #region Data
        public byte HasSpawned;
        #endregion
    }
}
