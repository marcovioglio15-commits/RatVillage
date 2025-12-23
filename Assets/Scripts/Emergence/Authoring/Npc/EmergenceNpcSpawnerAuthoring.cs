using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Authoring component that configures an NPC spawner.
    /// </summary>
    public sealed class EmergenceNpcSpawnerAuthoring : MonoBehaviour
    {
        #region Serialized
        // Serialized references
        #region Serialized - References
        [Tooltip("Prefab converted to an NPC entity.")]
        [Header("References")]
        [SerializeField] private GameObject npcPrefab;

        [Tooltip("Optional society root assigned to spawned NPCs. Use the society root that owns EmergenceSocietyProfileAuthoring and EmergenceSocietySimulationAuthoring to bind members to schedules and shared resources.")]
        [SerializeField] private GameObject societyRoot;
        #endregion

        // Serialized spawn
        #region Serialized - Spawn
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
        [SerializeField] private string debugNamePrefix;
        #endregion
        #endregion

        #region Baker
        /// <summary>
        /// Bakes spawner data into ECS components.
        /// </summary>
        public sealed class Baker : Baker<EmergenceNpcSpawnerAuthoring>
        {
            /// <summary>
            /// Executes the baking process.
            /// </summary>
            public override void Bake(EmergenceNpcSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Entity prefabEntity = Entity.Null;
                Entity societyEntity = Entity.Null;

                if (authoring.npcPrefab != null)
                    prefabEntity = GetEntity(authoring.npcPrefab, TransformUsageFlags.Dynamic);

                if (authoring.societyRoot != null)
                    societyEntity = GetEntity(authoring.societyRoot, TransformUsageFlags.None);

                FixedString64Bytes debugPrefix = default;

                if (!string.IsNullOrWhiteSpace(authoring.debugNamePrefix))
                    debugPrefix = new FixedString64Bytes(authoring.debugNamePrefix);

                EmergenceNpcSpawner spawner = new EmergenceNpcSpawner
                {
                    Prefab = prefabEntity,
                    SocietyRoot = societyEntity,
                    Count = authoring.count,
                    Radius = authoring.radius,
                    Height = authoring.height,
                    Seed = GetSeed(authoring.randomSeed, authoring.name),
                    DebugNamePrefix = debugPrefix
                };

                EmergenceNpcSpawnerState state = new EmergenceNpcSpawnerState
                {
                    HasSpawned = 0
                };

                AddComponent(entity, spawner);
                AddComponent(entity, state);
            }
        }
        #endregion

        #region Helpers
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
