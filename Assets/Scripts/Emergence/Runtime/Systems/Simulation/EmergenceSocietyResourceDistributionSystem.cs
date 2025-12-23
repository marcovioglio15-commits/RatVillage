using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Distributes society resources to members based on need urgency.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceNeedDecaySystem))]
    [UpdateBefore(typeof(EmergenceTradeSystem))]
    public partial struct EmergenceSocietyResourceDistributionSystem : ISystem
    {
        #region State
        private BufferLookup<EmergenceNeed> needLookup;
        private BufferLookup<EmergenceNeedRule> ruleLookup;
        private BufferLookup<EmergenceResource> resourceLookup;
        private ComponentLookup<EmergenceSocietyClock> clockLookup;
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceSocietyResourceDistributionSettings>();
            state.RequireForUpdate<EmergenceNeedRule>();
            needLookup = state.GetBufferLookup<EmergenceNeed>(false);
            ruleLookup = state.GetBufferLookup<EmergenceNeedRule>(true);
            resourceLookup = state.GetBufferLookup<EmergenceResource>(false);
            clockLookup = state.GetComponentLookup<EmergenceSocietyClock>(true);
        }

        /// <summary>
        /// Distributes society resources to members at a controlled tick rate.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            needLookup.Update(ref state);
            ruleLookup.Update(ref state);
            resourceLookup.Update(ref state);

            // Resolve debug log access
            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer<EmergenceDebugEvent>(out DynamicBuffer<EmergenceDebugEvent> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                clockLookup.Update(ref state);
                EmergenceDebugLog debugLog = SystemAPI.GetSingleton<EmergenceDebugLog>();
                maxEntries = debugLog.MaxEntries;
            }

            NativeList<Entity> members = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> memberSocieties = new NativeList<Entity>(Allocator.Temp);
            BuildMemberLists(ref state, ref members, ref memberSocieties);

            if (members.Length == 0)
            {
                members.Dispose();
                memberSocieties.Dispose();
                return;
            }

            foreach ((RefRW<EmergenceSocietyResourceDistributionState> distState, RefRO<EmergenceSocietyResourceDistributionSettings> settings,
                DynamicBuffer<EmergenceResource> societyResources, Entity society)
                in SystemAPI.Query<RefRW<EmergenceSocietyResourceDistributionState>, RefRO<EmergenceSocietyResourceDistributionSettings>, DynamicBuffer<EmergenceResource>>()
                    .WithAll<EmergenceSocietyRoot>().WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.DistributionTickRate);

                if (time < distState.ValueRO.NextTick)
                    continue;

                distState.ValueRW.NextTick = time + intervalSeconds;

                if (societyResources.Length == 0)
                    continue;

                int maxTransfers = math.max(0, settings.ValueRO.MaxTransfersPerMember);

                if (maxTransfers == 0)
                    continue;

                DynamicBuffer<EmergenceResource> societyBuffer = societyResources;
                float timeOfDay = 0f;

                if (hasDebugBuffer)
                    timeOfDay = GetSocietyTime(society, ref clockLookup);

                for (int i = 0; i < members.Length; i++)
                {
                    if (memberSocieties[i] != society)
                        continue;

                    Entity member = members[i];

                    DynamicBuffer<EmergenceNeed> needs = needLookup[member];
                    DynamicBuffer<EmergenceNeedRule> rules = ruleLookup[member];
                    DynamicBuffer<EmergenceResource> memberResources = resourceLookup[member];

                    ApplyDistribution(societyBuffer, needs, rules, memberResources, settings.ValueRO, maxTransfers, hasDebugBuffer,
                        debugBuffer, maxEntries, timeOfDay, society, member);
                }
            }

            members.Dispose();
            memberSocieties.Dispose();
        }
        #endregion

    }
}
