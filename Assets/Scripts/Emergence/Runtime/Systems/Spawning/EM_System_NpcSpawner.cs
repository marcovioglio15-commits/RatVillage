using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EM_SystemNpcSpawner : ISystem
    {
        #region Fields

        #region Lookup
        private ComponentLookup<LocalTransform> localTransformLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        private ComponentLookup<EM_Component_NpcType> LogNameLookup;
        private BufferLookup<EM_BufferElement_NpcSpawnEntry> spawnEntryLookup;
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcSpawner>();
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            LogNameLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            spawnEntryLookup = state.GetBufferLookup<EM_BufferElement_NpcSpawnEntry>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            localTransformLookup.Update(ref state);
            memberLookup.Update(ref state);
            LogNameLookup.Update(ref state);
            spawnEntryLookup.Update(ref state);

            foreach ((RefRW<EM_Component_NpcSpawner> spawner, RefRW<EM_Component_NpcSpawnerState> spawnerState, Entity entity)
                in SystemAPI.Query<RefRW<EM_Component_NpcSpawner>, RefRW<EM_Component_NpcSpawnerState>>().WithEntityAccess())
            {
                if (spawnerState.ValueRO.HasSpawned != 0)
                    continue;

                int spawnCount = math.max(0, spawner.ValueRO.Count);

                if (spawnCount == 0)
                {
                    spawnerState.ValueRW.HasSpawned = 1;
                    continue;
                }

                NativeList<SpawnEntryCache> spawnEntries = new NativeList<SpawnEntryCache>(Allocator.Temp);
                float totalWeight = BuildSpawnEntries(entity, spawner.ValueRO, ref spawnEntryLookup, ref localTransformLookup,
                    ref memberLookup, ref LogNameLookup, ref spawnEntries);

                if (spawnEntries.Length == 0 || totalWeight <= 0f)
                {
                    spawnerState.ValueRW.HasSpawned = 1;
                    spawnEntries.Dispose();
                    continue;
                }

                float radius = math.max(0f, spawner.ValueRO.Radius);
                float height = spawner.ValueRO.Height;
                uint seed = spawner.ValueRO.Seed;
                float3 origin = float3.zero;

                if (localTransformLookup.HasComponent(entity))
                    origin = localTransformLookup[entity].Position;

                if (seed == 0u)
                    seed = 1u;

                Random random = Random.CreateFromIndex(seed);
                int digits = GetSpawnDigits(spawnCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    int entryIndex = PickSpawnEntryIndex(ref spawnEntries, totalWeight, ref random);
                    SpawnEntryCache entry = spawnEntries[entryIndex];
                    Entity prefab = entry.Prefab;
                    Entity instance = commandBuffer.Instantiate(prefab);
                    float3 position = GetRandomPosition(ref random, radius, height) + origin;

                    if (entry.HasTransform != 0)
                    {
                        LocalTransform localTransform = localTransformLookup[prefab];
                        localTransform.Position = position;
                        commandBuffer.SetComponent(instance, localTransform);
                    }

                    if (spawner.ValueRO.SocietyRoot != Entity.Null)
                    {
                        EM_Component_SocietyMember member = new EM_Component_SocietyMember
                        {
                            SocietyRoot = spawner.ValueRO.SocietyRoot
                        };

                        if (entry.HasMember != 0)
                            commandBuffer.SetComponent(instance, member);
                        else
                            commandBuffer.AddComponent(instance, member);
                    }

                    if (entry.HasLogName != 0)
                    {
                        EM_Component_NpcType LogName = new EM_Component_NpcType
                        {
                            TypeId = BuildLogName(entry.BaseLogName, i + 1, digits)
                        };

                        if (entry.PrefabHasLogName != 0)
                            commandBuffer.SetComponent(instance, LogName);
                        else
                            commandBuffer.AddComponent(instance, LogName);
                    }
                }

                spawner.ValueRW.Seed = random.NextUInt();
                spawnerState.ValueRW.HasSpawned = 1;
                spawnEntries.Dispose();
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
        #endregion

        #region Helpers
        // Weighted spawn entry cache for prefabs.
        private struct SpawnEntryCache
        {
            public Entity Prefab;
            public float Weight;
            public byte HasTransform;
            public byte HasMember;
            public byte HasLogName;
            public byte PrefabHasLogName;
            public FixedString64Bytes BaseLogName;
        }

        private static float BuildSpawnEntries(Entity spawnerEntity, EM_Component_NpcSpawner spawner,
            ref BufferLookup<EM_BufferElement_NpcSpawnEntry> spawnEntryLookup, ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<EM_Component_SocietyMember> memberLookup, ref ComponentLookup<EM_Component_NpcType> NpcNameLookup,
            ref NativeList<SpawnEntryCache> entries)
        {
            float totalWeight = 0f;
            FixedString64Bytes spawnerPrefix = spawner.LogNamePrefix;
            bool hasSpawnerPrefix = spawnerPrefix.Length > 0;

            if (spawnEntryLookup.HasBuffer(spawnerEntity))
            {
                DynamicBuffer<EM_BufferElement_NpcSpawnEntry> buffer = spawnEntryLookup[spawnerEntity];

                for (int i = 0; i < buffer.Length; i++)
                {
                    EM_BufferElement_NpcSpawnEntry entry = buffer[i];
                    totalWeight += AddSpawnEntry(entry.Prefab, entry.Weight, spawnerPrefix, hasSpawnerPrefix,
                        ref localTransformLookup, ref memberLookup, ref NpcNameLookup, ref entries);
                }
            }

            if (entries.Length == 0 && spawner.Prefab != Entity.Null)
                totalWeight += AddSpawnEntry(spawner.Prefab, 1f, spawnerPrefix, hasSpawnerPrefix,
                    ref localTransformLookup, ref memberLookup, ref NpcNameLookup, ref entries);

            return totalWeight;
        }

        private static float AddSpawnEntry(Entity prefab, float weight, FixedString64Bytes spawnerPrefix, bool hasSpawnerPrefix,
            ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<EM_Component_SocietyMember> memberLookup,
            ref ComponentLookup<EM_Component_NpcType> LogNameLookup, ref NativeList<SpawnEntryCache> entries)
        {
            if (prefab == Entity.Null)
                return 0f;

            if (weight <= 0f)
                return 0f;

            byte hasTransform = (byte)(localTransformLookup.HasComponent(prefab) ? 1 : 0);
            byte hasMember = (byte)(memberLookup.HasComponent(prefab) ? 1 : 0);
            byte prefabHasLogName = (byte)(LogNameLookup.HasComponent(prefab) ? 1 : 0);
            byte hasLogName = 0;
            FixedString64Bytes baseLogName = default;

            if (hasSpawnerPrefix)
            {
                baseLogName = spawnerPrefix;
                hasLogName = 1;
            }
            else if (prefabHasLogName != 0)
            {
                EM_Component_NpcType LogName = LogNameLookup[prefab];

                if (LogName.TypeId.Length > 0)
                {
                    baseLogName = LogName.TypeId;
                    hasLogName = 1;
                }
            }

            SpawnEntryCache entry = new SpawnEntryCache
            {
                Prefab = prefab,
                Weight = weight,
                HasTransform = hasTransform,
                HasMember = hasMember,
                HasLogName = hasLogName,
                PrefabHasLogName = prefabHasLogName,
                BaseLogName = baseLogName
            };

            entries.Add(entry);
            return weight;
        }

        private static int PickSpawnEntryIndex(ref NativeList<SpawnEntryCache> entries, float totalWeight, ref Random random)
        {
            int count = entries.Length;

            if (count <= 1)
                return 0;

            float roll = random.NextFloat(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < count; i++)
            {
                cumulative += entries[i].Weight;

                if (roll <= cumulative)
                    return i;
            }

            return count - 1;
        }

        private static float3 GetRandomPosition(ref Random random, float radius, float height)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float distance = math.sqrt(random.NextFloat()) * radius;
            float x = math.cos(angle) * distance;
            float z = math.sin(angle) * distance;

            return new float3(x, height, z);
        }

        private static FixedString64Bytes BuildLogName(FixedString64Bytes baseName, int index, int digits)
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

        #endregion
    }
}
