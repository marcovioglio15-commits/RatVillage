using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_Trade))]
    [UpdateBefore(typeof(EM_System_MetricCollect))]
    public partial struct EM_System_NpcScheduleOverrideCooldown : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private ComponentLookup<EM_Component_SocietyMember> memberLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcScheduleOverrideCooldownState>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            memberLookup = state.GetComponentLookup<EM_Component_SocietyMember>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            if (deltaTime <= 0f)
                return;

            clockLookup.Update(ref state);
            memberLookup.Update(ref state);

            foreach ((RefRO<EM_Component_NpcScheduleOverride> scheduleOverride,
                RefRW<EM_Component_NpcScheduleOverrideCooldownState> cooldownState,
                Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcScheduleOverride>, RefRW<EM_Component_NpcScheduleOverrideCooldownState>>()
                    .WithEntityAccess())
            {
                if (!memberLookup.HasComponent(entity))
                    continue;

                EM_Component_SocietyMember member = memberLookup[entity];
                Entity societyRoot = member.SocietyRoot;

                if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                    continue;

                double timeSeconds = clockLookup[societyRoot].SimulatedTimeSeconds;
                bool isOverrideActive = scheduleOverride.ValueRO.RemainingHours > 0f && scheduleOverride.ValueRO.ActivityId.Length > 0;

                if (isOverrideActive)
                {
                    cooldownState.ValueRW.WasOverrideActive = 1;
                    cooldownState.ValueRW.ActiveOverrideActivityId = scheduleOverride.ValueRO.ActivityId;
                    continue;
                }

                if (cooldownState.ValueRO.WasOverrideActive == 0)
                    continue;

                cooldownState.ValueRW.WasOverrideActive = 0;
                cooldownState.ValueRW.LastOverrideEndTimeSeconds = timeSeconds;
                cooldownState.ValueRW.LastOverrideActivityId = cooldownState.ValueRO.ActiveOverrideActivityId;
                cooldownState.ValueRW.ActiveOverrideActivityId = default;
            }
        }
        #endregion
    }
}
