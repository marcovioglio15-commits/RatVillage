using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_SocietyClock))]
    public partial struct EM_System_NpcNavigation : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_SocietyClock> clockLookup;
        private ComponentLookup<EM_Component_LocationAnchor> anchorLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_LocationGrid>();
            state.RequireForUpdate<EM_Component_NpcNavigationState>();
            clockLookup = state.GetComponentLookup<EM_Component_SocietyClock>(true);
            anchorLookup = state.GetComponentLookup<EM_Component_LocationAnchor>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity;
            EM_Component_LocationGrid grid;

            if (!TryGetGrid(ref state, out gridEntity, out grid))
                return;

            DynamicBuffer<EM_BufferElement_LocationNode> nodes = SystemAPI.GetBuffer<EM_BufferElement_LocationNode>(gridEntity);
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy = SystemAPI.GetBuffer<EM_BufferElement_LocationOccupancy>(gridEntity);

            if (nodes.Length == 0 || nodes.Length != occupancy.Length)
                return;

            NativeParallelHashMap<int, Entity> anchorMap = BuildAnchorMap(ref state, gridEntity);

            clockLookup.Update(ref state);
            anchorLookup.Update(ref state);
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRW<LocalTransform> transform, RefRO<EM_Component_NpcMovementSettings> settings,
                RefRW<EM_Component_NpcMovementState> movementState, RefRW<EM_Component_NpcNavigationState> navigationState,
                RefRW<EM_Component_NpcLocationState> locationState, DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes,
                EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<RefRW<LocalTransform>, RefRO<EM_Component_NpcMovementSettings>,
                    RefRW<EM_Component_NpcMovementState>, RefRW<EM_Component_NpcNavigationState>,
                    RefRW<EM_Component_NpcLocationState>, DynamicBuffer<EM_BufferElement_NpcPathNode>,
                    EM_Component_SocietyMember>()
                    .WithEntityAccess())
            {
                float speedScale = ResolveSpeedScale(member.SocietyRoot);
                UpdateCurrentNode(transform.ValueRO.Position, entity, grid, nodes, occupancy, locationState, ref anchorMap);

                if (navigationState.ValueRO.DestinationKind == EM_NpcDestinationKind.None)
                {
                    StopMovement(navigationState, movementState);
                    continue;
                }

                UpdateDestinationAnchor(navigationState);
                EnsurePath(entity, grid, nodes, occupancy, navigationState, pathNodes, locationState.ValueRO);
                MoveAlongPath(transform, settings.ValueRO, speedScale, deltaTime, movementState, navigationState, grid, locationState,
                    nodes, occupancy, pathNodes, entity, ref anchorMap);
            }

            anchorMap.Dispose();
        }
        #endregion
    }
}
