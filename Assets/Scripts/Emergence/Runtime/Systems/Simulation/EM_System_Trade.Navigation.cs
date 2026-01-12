using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Navigation
        private static void SetTradeMeetingDestination(RefRW<EM_Component_NpcNavigationState> navigationState, Entity anchorEntity)
        {
            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.TradeMeeting;
            navigationState.ValueRW.DestinationAnchor = anchorEntity;
            navigationState.ValueRW.DestinationPosition = float3.zero;
            navigationState.ValueRW.DestinationNodeIndex = -1;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }

        private static void SetTradeQueueDestination(RefRW<EM_Component_NpcNavigationState> navigationState, float3 position)
        {
            navigationState.ValueRW.DestinationKind = EM_NpcDestinationKind.TradeQueue;
            navigationState.ValueRW.DestinationAnchor = Entity.Null;
            navigationState.ValueRW.DestinationPosition = position;
            navigationState.ValueRW.DestinationNodeIndex = -1;
            navigationState.ValueRW.PathIndex = 0;
            navigationState.ValueRW.HasPath = 0;
        }

        private static void ClearTradeDestination(RefRW<EM_Component_NpcNavigationState> navigationState)
        {
            if (navigationState.ValueRO.DestinationKind != EM_NpcDestinationKind.TradeMeeting &&
                navigationState.ValueRO.DestinationKind != EM_NpcDestinationKind.TradeQueue)
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
