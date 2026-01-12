using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcNavigation
    {
        #region Movement
        private static void MoveAlongPath(RefRW<LocalTransform> transform, EM_Component_NpcMovementSettings settings, float speedScale, float deltaTime,
            RefRW<EM_Component_NpcMovementState> movementState, RefRW<EM_Component_NpcNavigationState> navigationState,
            EM_Component_LocationGrid grid, RefRW<EM_Component_NpcLocationState> locationState, DynamicBuffer<EM_BufferElement_LocationNode> nodes,
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy, DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes, Entity entity,
            ref NativeParallelHashMap<int, Entity> anchorMap)
        {
            if (navigationState.ValueRO.HasPath == 0 || pathNodes.Length == 0)
            {
                StopMovement(navigationState, movementState);
                return;
            }

            int pathIndex = navigationState.ValueRO.PathIndex;

            if (pathIndex < 0)
                pathIndex = 0;

            if (pathIndex >= pathNodes.Length)
            {
                ReachDestination(navigationState, movementState);
                return;
            }

            int nextNodeIndex = pathNodes[pathIndex].NodeIndex;

            if (nextNodeIndex < 0)
            {
                ReachDestination(navigationState, movementState);
                return;
            }

            if (nextNodeIndex == locationState.ValueRO.CurrentNodeIndex)
            {
                navigationState.ValueRW.PathIndex = pathIndex + 1;
                return;
            }

            if (IsNodeBlocked(nextNodeIndex, entity, occupancy))
            {
                movementState.ValueRW.CurrentSpeed = 0f;
                navigationState.ValueRW.IsMoving = 0;
                return;
            }

            float3 targetPosition = EM_Utility_LocationGrid.GetNodeCenter(nextNodeIndex, grid);
            float3 currentPosition = transform.ValueRO.Position;
            float3 toTarget = targetPosition - currentPosition;
            float distance = math.length(toTarget);
            float stopRadius = math.max(0.01f, settings.StopRadius);
            float maxSpeed = math.max(0f, settings.MaxSpeed) * speedScale;
            float acceleration = math.max(0f, settings.Acceleration) * speedScale;

            if (deltaTime <= 0f)
                return;

            float desiredSpeed = maxSpeed;
            float currentSpeed = movementState.ValueRO.CurrentSpeed;
            currentSpeed = MoveTowards(currentSpeed, desiredSpeed, acceleration * deltaTime);
            movementState.ValueRW.CurrentSpeed = currentSpeed;

            if (distance <= stopRadius || currentSpeed <= 0f)
            {
                transform.ValueRW.Position = targetPosition;
                UpdateNodeOccupancy(nextNodeIndex, entity, locationState, nodes, occupancy, ref anchorMap);
                navigationState.ValueRW.PathIndex = pathIndex + 1;
                return;
            }

            float3 direction = math.normalizesafe(toTarget);
            float step = currentSpeed * deltaTime;

            if (step >= distance)
            {
                transform.ValueRW.Position = targetPosition;
                UpdateNodeOccupancy(nextNodeIndex, entity, locationState, nodes, occupancy, ref anchorMap);
                navigationState.ValueRW.PathIndex = pathIndex + 1;
                return;
            }

            float3 nextPosition = currentPosition + direction * step;
            transform.ValueRW.Position = nextPosition;
            navigationState.ValueRW.IsMoving = 1;
        }

        private static void StopMovement(RefRW<EM_Component_NpcNavigationState> navigationState, RefRW<EM_Component_NpcMovementState> movementState)
        {
            navigationState.ValueRW.IsMoving = 0;
            movementState.ValueRW.CurrentSpeed = 0f;
        }

        private static void ReachDestination(RefRW<EM_Component_NpcNavigationState> navigationState, RefRW<EM_Component_NpcMovementState> movementState)
        {
            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.None;
            navigationState.ValueRW.HasPath = 0;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.IsMoving = 0;
            movementState.ValueRW.CurrentSpeed = 0f;
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (math.abs(target - current) <= maxDelta)
                return target;

            if (target > current)
                return current + maxDelta;

            return current - maxDelta;
        }
        #endregion
    }
}
