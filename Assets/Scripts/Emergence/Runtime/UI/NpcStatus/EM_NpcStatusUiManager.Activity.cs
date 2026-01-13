using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcStatusUiManager
    {
        #region Activity
        private void AppendActivityLine(Entity entity, StringBuilder builder)
        {
            builder.Append("Current Activity: ");

            if (TryAppendMovingActivity(entity, builder))
                return;

            FixedString64Bytes activityId = ResolveCurrentActivityId(entity);

            if (activityId.Length == 0)
            {
                builder.Append("None");
                return;
            }

            builder.Append(EM_NarrativeLogFormatter.FormatId(activityId));
        }

        private bool TryAppendMovingActivity(Entity entity, StringBuilder builder)
        {
            if (!entityManager.HasComponent<EM_Component_NpcNavigationState>(entity))
                return false;

            EM_Component_NpcNavigationState navigationState = entityManager.GetComponentData<EM_Component_NpcNavigationState>(entity);

            if (navigationState.IsMoving == 0)
                return false;

            EM_NpcDestinationKind destinationKind = navigationState.DestinationKind;

            if (destinationKind == EM_NpcDestinationKind.TradeMeeting || destinationKind == EM_NpcDestinationKind.TradeQueue)
            {
                builder.Append("moving to trade");
                return true;
            }

            if (destinationKind == EM_NpcDestinationKind.Activity)
            {
                FixedString64Bytes targetActivityId = ResolveTargetActivityId(entity);

                if (targetActivityId.Length > 0)
                {
                    builder.Append("moving to ");
                    builder.Append(EM_NarrativeLogFormatter.FormatId(targetActivityId));
                    return true;
                }

                builder.Append("moving to activity");
                return true;
            }

            builder.Append("moving");
            return true;
        }

        private FixedString64Bytes ResolveCurrentActivityId(Entity entity)
        {
            if (!entityManager.HasComponent<EM_Component_NpcScheduleState>(entity))
                return default;

            EM_Component_NpcScheduleState scheduleState = entityManager.GetComponentData<EM_Component_NpcScheduleState>(entity);
            return scheduleState.CurrentActivityId;
        }

        private FixedString64Bytes ResolveTargetActivityId(Entity entity)
        {
            if (!entityManager.HasComponent<EM_Component_NpcScheduleTarget>(entity))
                return default;

            EM_Component_NpcScheduleTarget target = entityManager.GetComponentData<EM_Component_NpcScheduleTarget>(entity);
            return target.ActivityId;
        }
        #endregion
    }
}
