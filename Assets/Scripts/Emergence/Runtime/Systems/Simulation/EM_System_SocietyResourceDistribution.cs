using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NeedDecay))]
    [UpdateBefore(typeof(EM_System_Trade))]
    public partial struct EM_System_SocietyResourceDistribution : ISystem
    {
        #region Fields

        #region Lookup
        private BufferLookup<EM_BufferElement_Need> needLookup;
        private BufferLookup<EM_BufferElement_NeedRule> ruleLookup;
        private BufferLookup<EM_BufferElement_Resource> resourceLookup;
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_SocietyResourceDistributionSettings>();
            state.RequireForUpdate<EM_BufferElement_NeedRule>();
            needLookup = state.GetBufferLookup<EM_BufferElement_Need>(false);
            ruleLookup = state.GetBufferLookup<EM_BufferElement_NeedRule>(true);
            resourceLookup = state.GetBufferLookup<EM_BufferElement_Resource>(false);
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            double time = SystemAPI.Time.ElapsedTime;
            needLookup.Update(ref state);
            ruleLookup.Update(ref state);
            resourceLookup.Update(ref state);

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer<EM_Component_Event>(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;

            if (hasDebugBuffer)
            {
                clockLookup.Update(ref state);
                EM_Component_Log debugLog = SystemAPI.GetSingleton<EM_Component_Log>();
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

            foreach ((RefRW<EM_Component_SocietyResourceDistributionState> distState, RefRO<EM_Component_SocietyResourceDistributionSettings> settings,
                DynamicBuffer<EM_BufferElement_Resource> societyResources, Entity society)
                in SystemAPI.Query<RefRW<EM_Component_SocietyResourceDistributionState>, RefRO<EM_Component_SocietyResourceDistributionSettings>, DynamicBuffer<EM_BufferElement_Resource>>()
                    .WithAll<EM_Component_SocietyRoot>().WithEntityAccess())
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

                DynamicBuffer<EM_BufferElement_Resource> societyBuffer = societyResources;
                float timeOfDay = 0f;

                if (hasDebugBuffer)
                    timeOfDay = GetSocietyTime(society, ref clockLookup);

                for (int i = 0; i < members.Length; i++)
                {
                    if (memberSocieties[i] != society)
                        continue;

                    Entity member = members[i];

                    DynamicBuffer<EM_BufferElement_Need> needs = needLookup[member];
                    DynamicBuffer<EM_BufferElement_NeedRule> rules = ruleLookup[member];
                    DynamicBuffer<EM_BufferElement_Resource> memberResources = resourceLookup[member];

                    ApplyDistribution(societyBuffer, needs, rules, memberResources, settings.ValueRO, maxTransfers, hasDebugBuffer,
                        debugBuffer, maxEntries, timeOfDay, society, member);
                }
            }

            members.Dispose();
            memberSocieties.Dispose();
        }
        #endregion

        #endregion

    }
}
