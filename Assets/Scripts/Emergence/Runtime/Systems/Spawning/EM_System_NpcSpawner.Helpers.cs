using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_SystemNpcSpawner : ISystem
    {
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
            public byte PrefabHasRandomSeed;
            public FixedString64Bytes BaseLogName;
        }

        // Build the weighted list of spawnable prefabs.
        private static float BuildSpawnEntries(Entity spawnerEntity, EM_Component_NpcSpawner spawner,
            ref BufferLookup<EM_BufferElement_NpcSpawnEntry> spawnEntryLookup, ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<EM_Component_SocietyMember> memberLookup, ref ComponentLookup<EM_Component_NpcType> NpcNameLookup,
            ref ComponentLookup<EM_Component_RandomSeed> randomSeedLookup,
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
                        ref localTransformLookup, ref memberLookup, ref NpcNameLookup, ref randomSeedLookup, ref entries);
                }
            }

            if (entries.Length == 0 && spawner.Prefab != Entity.Null)
                totalWeight += AddSpawnEntry(spawner.Prefab, 1f, spawnerPrefix, hasSpawnerPrefix,
                    ref localTransformLookup, ref memberLookup, ref NpcNameLookup, ref randomSeedLookup, ref entries);

            return totalWeight;
        }

        // Register a spawnable prefab into the weighted list.
        private static float AddSpawnEntry(Entity prefab, float weight, FixedString64Bytes spawnerPrefix, bool hasSpawnerPrefix,
            ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<EM_Component_SocietyMember> memberLookup,
            ref ComponentLookup<EM_Component_NpcType> LogNameLookup, ref ComponentLookup<EM_Component_RandomSeed> randomSeedLookup,
            ref NativeList<SpawnEntryCache> entries)
        {
            if (prefab == Entity.Null)
                return 0f;

            if (weight <= 0f)
                return 0f;

            byte hasTransform = (byte)(localTransformLookup.HasComponent(prefab) ? 1 : 0);
            byte hasMember = (byte)(memberLookup.HasComponent(prefab) ? 1 : 0);
            byte prefabHasLogName = (byte)(LogNameLookup.HasComponent(prefab) ? 1 : 0);
            byte prefabHasRandomSeed = (byte)(randomSeedLookup.HasComponent(prefab) ? 1 : 0);
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
                PrefabHasRandomSeed = prefabHasRandomSeed,
                BaseLogName = baseLogName
            };

            entries.Add(entry);
            return weight;
        }

        // Pick a prefab index based on weighted probability.
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

        // Build a random position around the spawner origin.
        private static float3 GetRandomPosition(ref Random random, float radius, float height)
        {
            float angle = random.NextFloat(0f, math.PI * 2f);
            float distance = math.sqrt(random.NextFloat()) * radius;
            float x = math.cos(angle) * distance;
            float z = math.sin(angle) * distance;

            return new float3(x, height, z);
        }

        // Create a readable debug name with padded index digits.
        private static FixedString64Bytes BuildLogName(FixedString64Bytes baseName, int index, int digits)
        {
            string prefix = baseName.ToString();
            string format = "D" + digits.ToString(CultureInfo.InvariantCulture);
            string number = index.ToString(format, CultureInfo.InvariantCulture);
            string combined = prefix + " " + number;
            return new FixedString64Bytes(combined);
        }

        // Compute the number of digits needed for log name formatting.
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

        // Ensure random seed values are non-zero.
        private static uint GetNonZeroSeed(ref Random random)
        {
            uint seed = random.NextUInt();

            if (seed == 0u)
                return 1u;

            return seed;
        }
        #endregion
    }
}
