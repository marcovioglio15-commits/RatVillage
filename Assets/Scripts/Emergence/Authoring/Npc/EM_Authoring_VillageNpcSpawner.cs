using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed class EM_Authoring_VillageNpcSpawner : MonoBehaviour
    {
        #region Nested Types
        // Serialized data blocks for weighted prefab spawning.
        [Serializable]
        public struct SpawnEntry
        {
            [Tooltip("NPC prefab converted to an entity.")]
            public GameObject Prefab;

            [Tooltip("Relative spawn weight. Values <= 0 are ignored.")]
            public float Weight;
        }
        #endregion

        #region Fields
        #region Serialized
        #region References
        [Tooltip("Legacy single prefab used when Spawn Entries are empty.")]
        [Header("References")]
        [SerializeField] private GameObject npcPrefab;

        [Tooltip("Spawnable NPC prefabs with relative weights. If empty, the legacy prefab is used.")]
        [SerializeField] private SpawnEntry[] spawnEntries = new SpawnEntry[0];

        [Tooltip("Optional society root assigned to spawned NPCs. Use the society root that owns EM_Authoring_SocietyProfile and EM_Authoring_SocietySimulation to bind members to schedules and shared resources.")]
        [SerializeField] private GameObject societyRoot;
        #endregion

        #region Spawn
        [Tooltip("Number of NPCs to spawn.")]
        [Header("Spawn")]
        [SerializeField] private int count = 12;

        [Tooltip("Spawn radius around the spawner position.")]
        [SerializeField] private float radius = 12f;

        [Tooltip("Spawn height offset.")]
        [SerializeField] private float height;

        [Tooltip("Random seed (0 to auto-generate).")]
        [SerializeField] private uint randomSeed;

        [Tooltip("Optional debug name prefix for spawned NPCs. Overrides the prefab debug name and will be suffixed with an index in the debug HUD.")]
        [SerializeField] private string LogNamePrefix;
        #endregion
        #endregion
        #endregion

        #region Baker
        public sealed class Baker : Baker<EM_Authoring_VillageNpcSpawner>
        {
            public override void Bake(EM_Authoring_VillageNpcSpawner authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Entity prefabEntity = Entity.Null;
                Entity societyEntity = Entity.Null;

                if (authoring.npcPrefab != null)
                    prefabEntity = GetEntity(authoring.npcPrefab, TransformUsageFlags.Dynamic);

                if (authoring.societyRoot != null)
                    societyEntity = GetEntity(authoring.societyRoot, TransformUsageFlags.None);

                FixedString64Bytes debugPrefix = default;

                if (!string.IsNullOrWhiteSpace(authoring.LogNamePrefix))
                    debugPrefix = new FixedString64Bytes(authoring.LogNamePrefix);

                EM_Component_NpcSpawner spawner = new EM_Component_NpcSpawner
                {
                    Prefab = prefabEntity,
                    SocietyRoot = societyEntity,
                    Count = authoring.count,
                    Radius = authoring.radius,
                    Height = authoring.height,
                    Seed = GetSeed(authoring.randomSeed, authoring.name),
                    LogNamePrefix = debugPrefix
                };

                EM_Component_NpcSpawnerState state = new EM_Component_NpcSpawnerState
                {
                    HasSpawned = 0
                };

                DynamicBuffer<EM_BufferElement_NpcSpawnEntry> spawnBuffer = AddBuffer<EM_BufferElement_NpcSpawnEntry>(entity);
                int spawnEntryCount = AddSpawnEntries(authoring.spawnEntries, ref spawnBuffer, this);

                if (spawnEntryCount == 0 && authoring.npcPrefab != null)
                    AddSpawnEntry(authoring.npcPrefab, 1f, ref spawnBuffer, this);

                AddComponent(entity, spawner);
                AddComponent(entity, state);
            }
        }
        #endregion

        #region Helpers
        private static int AddSpawnEntries(SpawnEntry[] source, ref DynamicBuffer<EM_BufferElement_NpcSpawnEntry> buffer,
            Baker<EM_Authoring_VillageNpcSpawner> baker)
        {
            if (source == null)
                return 0;

            int count = 0;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].Prefab == null)
                    continue;

                if (source[i].Weight <= 0f)
                    continue;

                AddSpawnEntry(source[i].Prefab, source[i].Weight, ref buffer, baker);
                count++;
            }

            return count;
        }

        private static void AddSpawnEntry(GameObject prefab, float weight, ref DynamicBuffer<EM_BufferElement_NpcSpawnEntry> buffer,
            Baker<EM_Authoring_VillageNpcSpawner> baker)
        {
            if (prefab == null)
                return;

            Entity prefabEntity = baker.GetEntity(prefab, TransformUsageFlags.Dynamic);

            EM_BufferElement_NpcSpawnEntry entry = new EM_BufferElement_NpcSpawnEntry
            {
                Prefab = prefabEntity,
                Weight = weight
            };

            buffer.Add(entry);
        }

        private static uint GetSeed(uint seed, string name)
        {
            if (seed != 0u)
                return seed;

            FixedString64Bytes fixedName = new FixedString64Bytes(name);
            uint hashed = (uint)fixedName.GetHashCode();

            if (hashed == 0u)
                return 1u;

            return hashed;
        }
        #endregion
    }
}
