using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NpcSchedule))]
    [UpdateBefore(typeof(EM_System_NpcNavigation))]
    public partial struct EM_System_NpcActivityNavigation : ISystem
    {
        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LocationAnchor>();
            state.RequireForUpdate<EM_Component_NpcScheduleTarget>();
            state.RequireForUpdate<EM_Component_NpcNavigationState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<FixedString64Bytes, Entity> anchorMap = BuildLocationAnchorMap(ref state);

            foreach ((RefRO<EM_Component_NpcScheduleTarget> target, RefRO<EM_Component_NpcLocationState> locationState,
                RefRO<EM_Component_TradeRequestState> tradeRequest, RefRW<EM_Component_NpcNavigationState> navigationState)
                in SystemAPI.Query<RefRO<EM_Component_NpcScheduleTarget>, RefRO<EM_Component_NpcLocationState>,
                    RefRO<EM_Component_TradeRequestState>, RefRW<EM_Component_NpcNavigationState>>())
            {
                if (tradeRequest.ValueRO.Stage != EM_TradeRequestStage.None)
                    continue;

                FixedString64Bytes locationId = target.ValueRO.LocationId;

                if (locationId.Length == 0)
                {
                    ClearActivityDestination(navigationState);
                    continue;
                }

                if (locationState.ValueRO.CurrentLocationId.Equals(locationId))
                {
                    ClearActivityDestination(navigationState);
                    continue;
                }

                Entity anchorEntity;
                bool hasAnchor = anchorMap.TryGetValue(locationId, out anchorEntity);

                if (!hasAnchor)
                {
                    ClearActivityDestination(navigationState);
                    continue;
                }

                if (navigationState.ValueRO.DestinationKind == EM_NpcDestinationKind.Activity &&
                    navigationState.ValueRO.DestinationAnchor == anchorEntity)
                    continue;

                SetActivityDestination(navigationState, anchorEntity);
            }

            anchorMap.Dispose();
        }
        #endregion

        #region Helpers
        private NativeParallelHashMap<FixedString64Bytes, Entity> BuildLocationAnchorMap(ref SystemState state)
        {
            NativeParallelHashMap<FixedString64Bytes, Entity> map = new NativeParallelHashMap<FixedString64Bytes, Entity>(32, Allocator.Temp);

            foreach ((RefRO<EM_Component_LocationAnchor> anchor, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_LocationAnchor>>().WithEntityAccess())
            {
                FixedString64Bytes locationId = anchor.ValueRO.LocationId;

                if (locationId.Length == 0)
                    continue;

                if (map.ContainsKey(locationId))
                    continue;

                map.TryAdd(locationId, entity);
            }

            return map;
        }

        private static void SetActivityDestination(RefRW<EM_Component_NpcNavigationState> navigationState, Entity anchorEntity)
        {
            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.Activity;
            navigationState.ValueRW.DestinationAnchor = anchorEntity;
            navigationState.ValueRW.DestinationPosition = float3.zero;
            navigationState.ValueRW.DestinationNodeIndex = -1;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }

        private static void ClearActivityDestination(RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            if (navigationState.ValueRO.DestinationKind != EM_NpcDestinationKind.Activity)
                return;

            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.None;
            navigationState.ValueRW.DestinationAnchor = Entity.Null;
            navigationState.ValueRW.DestinationPosition = float3.zero;
            navigationState.ValueRW.DestinationNodeIndex = -1;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }
        #endregion
    }
}
