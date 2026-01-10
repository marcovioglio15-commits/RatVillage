using System;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EM_SystemNpcSpawner))]
    public partial struct EM_System_RandomSeedInitialization : ISystem
    {
        #region Fields
        private bool hasInitialized;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_RandomSeed>();
            hasInitialized = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (hasInitialized)
                return;

            uint baseSeed = GetRuntimeSeed();
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex(baseSeed);

            foreach (RefRW<EM_Component_RandomSeed> seed in SystemAPI.Query<RefRW<EM_Component_RandomSeed>>())
            {
                uint value = random.NextUInt();

                if (value == 0u)
                    value = 1u;

                EM_Component_RandomSeed seedComponent = seed.ValueRO;
                seedComponent.Value = value;
                seed.ValueRW = seedComponent;
            }

            hasInitialized = true;
        }
        #endregion

        #region Seed
        private static uint GetRuntimeSeed()
        {
            long ticks = DateTime.UtcNow.Ticks;
            uint seed = (uint)(ticks ^ (ticks >> 32));

            if (seed == 0u)
                seed = 1u;

            return seed;
        }
        #endregion
    }
}
