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
        private ComponentLookup<EM_Component_RandomSeed> randomSeedLookup;
        private BufferLookup<EM_BufferElement_NpcSpawnEntry> spawnEntryLookup;
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle
        // Configure component lookups for spawn processing.
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcSpawner>();
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
            LogNameLookup = state.GetComponentLookup<EM_Component_NpcType>(true);
            randomSeedLookup = state.GetComponentLookup<EM_Component_RandomSeed>(true);
            spawnEntryLookup = state.GetBufferLookup<EM_BufferElement_NpcSpawnEntry>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            localTransformLookup.Update(ref state);
            memberLookup.Update(ref state);
            LogNameLookup.Update(ref state);
            randomSeedLookup.Update(ref state);
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
                    ref memberLookup, ref LogNameLookup, ref randomSeedLookup, ref spawnEntries);

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

                    EM_Component_RandomSeed seedComponent = new EM_Component_RandomSeed
                    {
                        Value = GetNonZeroSeed(ref random)
                    };

                    if (entry.PrefabHasRandomSeed != 0)
                        commandBuffer.SetComponent(instance, seedComponent);
                    else
                        commandBuffer.AddComponent(instance, seedComponent);
                }

                spawner.ValueRW.Seed = random.NextUInt();
                spawnerState.ValueRW.HasSpawned = 1;
                spawnEntries.Dispose();
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }
        #endregion

        #endregion
    }
}
