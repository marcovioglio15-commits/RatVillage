using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public struct EM_Component_NpcSpawner : IComponentData
    {
        #region Data
        public Entity Prefab;
        public Entity SocietyRoot;
        public int Count;
        public float Radius;
        public float Height;
        public uint Seed;
        public FixedString64Bytes LogNamePrefix;
        #endregion
    }

    public struct EM_BufferElement_NpcSpawnEntry : IBufferElementData
    {
        #region Data
        public Entity Prefab;
        public float Weight;
        #endregion
    }

    public struct EM_Component_NpcSpawnerState : IComponentData
    {
        #region Data
        public byte HasSpawned;
        #endregion
    }
}
