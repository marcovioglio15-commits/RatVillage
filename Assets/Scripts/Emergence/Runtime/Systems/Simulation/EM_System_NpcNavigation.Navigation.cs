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

        private void UpdateDestinationAnchor(ref RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            if (navigationState.ValueRO.DestinationAnchor == Entity.Null)
                return;

            if (!anchorLookup.HasComponent(navigationState.ValueRO.DestinationAnchor))
                return;

            EM_Component_LocationAnchor anchor = anchorLookup[navigationState.ValueRO.DestinationAnchor];
            navigationState.ValueRW.DestinationNodeIndex = anchor.NodeIndex;
        }

        private static void EnsurePath(Entity entity, ref EM_Component_LocationGrid grid, DynamicBuffer<EM_BufferElement_LocationNode> nodes,
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy, ref RefRW<EM_Component_NpcNavigationState> navigationState,
            ref DynamicBuffer<EM_BufferElement_NpcPathNode> pathNodes, EM_Component_NpcLocationState locationState)
        {
            int destinationIndex = navigationState.ValueRO.DestinationNodeIndex;

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
            bool built = TryBuildPath(locationState.CurrentNodeIndex, destinationIndex, entity, ref grid, nodes, occupancy, ref pathNodes);

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
