using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_NpcNavigation
    {
        #region Navigation
        private float ResolveSpeedScale(Entity societyRoot)
        {
            if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                return 1f;

            EM_Component_SocietyClock clock = clockLookup[societyRoot];
            float speed = math.max(0f, clock.BaseSimulationSpeed * clock.SimulationSpeedMultiplier);

            if (speed <= 0f)
                return 1f;

            return speed;
        }

        private double ResolveSimulatedTimeSeconds(Entity societyRoot, double fallbackTimeSeconds)
        {
            if (societyRoot == Entity.Null || !clockLookup.HasComponent(societyRoot))
                return fallbackTimeSeconds;

            return clockLookup[societyRoot].SimulatedTimeSeconds;
        }

        private void UpdateDestinationAnchor(RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            if (navigationState.ValueRO.DestinationAnchor == Entity.Null)
                return;

            if (!anchorLookup.HasComponent(navigationState.ValueRO.DestinationAnchor))
                return;

            EM_Component_LocationAnchor anchor = anchorLookup[navigationState.ValueRO.DestinationAnchor];
            navigationState.ValueRW.DestinationNodeIndex = anchor.NodeIndex;
        }

        private static void EnsurePath(Entity entity, EM_Component_LocationGrid grid, DynamicBuffer<EM_BufferElement_LocationNode> nodes,
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy, DynamicBuffer<EM_BufferElement_LocationReservation> reservations,
            RefRW<EM_Component_NpcNavigationState> navigationState, DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes,
            EM_Component_NpcLocationState locationState, double timeSeconds)
        {
            int destinationIndex = navigationState.ValueRO.DestinationNodeIndex;

            if (destinationIndex < 0 && navigationState.ValueRO.DestinationKind == EM_NpcDestinationKind.TradeQueue)
            {
                int positionIndex;
                bool hasPosition = EM_Utility_LocationGrid.TryGetNodeIndex(navigationState.ValueRO.DestinationPosition, grid, out positionIndex);

                if (hasPosition)
                {
                    navigationState.ValueRW.DestinationNodeIndex = positionIndex;
                    destinationIndex = positionIndex;
                }
            }

            if (destinationIndex < 0)
                return;

            if (locationState.CurrentNodeIndex < 0)
                return;

            bool needsPath = navigationState.ValueRO.HasPath == 0 || pathNodes.Length == 0;

            if (!needsPath)
            {
                int lastIndex = pathNodes[pathNodes.Length - 1].NodeIndex;

                if (lastIndex != destinationIndex)
                    needsPath = true;
            }

            if (!needsPath)
                return;

            pathNodes.Clear();
            bool built = TryBuildPath(locationState.CurrentNodeIndex, destinationIndex, entity, grid, nodes, occupancy, reservations,
                pathNodes, timeSeconds);

            if (!built)
            {
                navigationState.ValueRW.HasPath = 0;
                navigationState.ValueRW.PathIndex = 0;
                return;
            }

            navigationState.ValueRW.HasPath = 1;
            navigationState.ValueRW.PathIndex = 0;
        }
        #endregion
    }
}
