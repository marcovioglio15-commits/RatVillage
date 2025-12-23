using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Emergence
{
    /// <summary>
    /// Spawns NPC entities from a prefab using a single-use spawner component.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EmergenceNpcSpawnerSystem : ISystem
    {
        #region State
        private ComponentLookup<LocalTransform> localTransformLookup;
        private ComponentLookup<EmergenceSocietyMember> memberLookup;
        private ComponentLookup<EmergenceDebugName> debugNameLookup;
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceNpcSpawner>();
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            memberLookup = state.GetComponentLookup<EmergenceSocietyMember>(true);
            debugNameLookup = state.GetComponentLookup<EmergenceDebugName>(true);
        }

        /// <summary>
        /// Instantiates NPCs when spawners have not yet fired.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            localTransformLookup.Update(ref state);
            memberLookup.Update(ref state);
            debugNameLookup.Update(ref state);

            foreach ((RefRW<EmergenceNpcSpawner> spawner, RefRW<EmergenceNpcSpawnerState> spawnerState, Entity entity)
                in SystemAPI.Query<RefRW<EmergenceNpcSpawner>, RefRW<EmergenceNpcSpawnerState>>().WithEntityAccess())
            {
                if (spawnerState.ValueRO.HasSpawned != 0)
                    continue;

                Entity prefab = spawner.ValueRO.Prefab;
                int spawnCount = math.max(0, spawner.ValueRO.Count);

                if (prefab == Entity.Null || spawnCount == 0)
                {
                    spawnerState.ValueRW.HasSpawned = 1;
                    continue;
                }

                bool hasTransform = localTransformLookup.HasComponent(prefab);
                bool hasMember = memberLookup.HasComponent(prefab);
                float radius = math.max(0f, spawner.ValueRO.Radius);
                float height = spawner.ValueRO.Height;
                uint seed = spawner.ValueRO.Seed;
                float3 origin = float3.zero;
                bool hasDebugName = false;
                FixedString64Bytes baseDebugName = default;
                bool prefabHasDebugName = debugNameLookup.HasComponent(prefab);
                FixedString64Bytes spawnerPrefix = spawner.ValueRO.DebugNamePrefix;
                bool hasSpawnerPrefix = spawnerPrefix.Length > 0;

                if (localTransformLookup.HasComponent(entity))
                    origin = localTransformLookup[entity].Position;

                if (seed == 0u)
                    seed = 1u;

                if (hasSpawnerPrefix)
                {
                    baseDebugName = spawnerPrefix;
                    hasDebugName = true;
                }
                else if (prefabHasDebugName)
                {
                    EmergenceDebugName debugName = debugNameLookup[prefab];

                    if (debugName.Value.Length > 0)
                    {
                        baseDebugName = debugName.Value;
                        hasDebugName = true;
                    }
                }

                Random random = Random.CreateFromIndex(seed);
                int digits = GetSpawnDigits(spawnCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    Entity instance = commandBuffer.Instantiate(prefab);
                    float3 position = GetRandomPosition(ref random, radius, height) + origin;

                    if (hasTransform)
                    {
                        LocalTransform localTransform = localTransformLookup[prefab];
                        localTransform.Position = position;
                        commandBuffer.SetComponent(instance, localTransform);
                    }

                    if (spawner.ValueRO.SocietyRoot != Entity.Null)
                    {
                        EmergenceSocietyMember member = new EmergenceSocietyMember
                        {
                            SocietyRoot = spawner.ValueRO.SocietyRoot
                        };

                        if (hasMember)
                            commandBuffer.SetComponent(instance, member);
                        else
                            commandBuffer.AddComponent(instance, member);
                    }

                    if (hasDebugName)
                    {
                        EmergenceDebugName debugName = new EmergenceDebugName
                        {
                            Value = BuildDebugName(baseDebugName, i + 1, digits)
                        };

                        if (prefabHasDebugName)
                            commandBuffer.SetComponent(instance, debugName);
                        else
                            commandBuffer.AddComponent(instance, debugName);
                    }
                }

                spawner.ValueRW.Seed = random.NextUInt();
                spawnerState.ValueRW.HasSpawned = 1;
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
        #endregion

        #region Helpers
        private static float3 GetRandomPosition(ref Random random, float radius, float height)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float distance = math.sqrt(random.NextFloat()) * radius;
            float x = math.cos(angle) * distance;
            float z = math.sin(angle) * distance;

            return new float3(x, height, z);
        }

        private static FixedString64Bytes BuildDebugName(FixedString64Bytes baseName, int index, int digits)
        {
            string prefix = baseName.ToString();
            string format = "D" + digits.ToString(CultureInfo.InvariantCulture);
            string number = index.ToString(format, CultureInfo.InvariantCulture);
            string combined = prefix + " " + number;
            return new FixedString64Bytes(combined);
        }

        private static int GetSpawnDigits(int spawnCount)
        {
            int digits = 1;
            int value = math.max(0, spawnCount);

            while (value >= 10)
            {
                digits++;
                value /= 10;
            }

            if (digits < 2)
                return 2;

            return digits;
        }
        #endregion
    }
}
