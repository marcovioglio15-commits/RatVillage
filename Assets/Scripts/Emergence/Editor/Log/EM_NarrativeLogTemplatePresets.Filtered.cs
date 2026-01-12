namespace EmergentMechanics
{
    internal static partial class EM_NarrativeLogTemplatePresets
    {
        #region Filtered Template Build
        private static EM_NarrativeTemplateDefinition[] BuildFilteredDefinitions()
        {
            EM_NarrativeTemplateDefinition hungerUrgency = CreateDefinition("Need Urgency - Hunger", EM_NarrativeEventType.NeedUrgency,
                EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Need, "[{time}] {subject} is starving",
                "Hunger urgency reached {value}.");
            hungerUrgency.NeedIdEquals = "Hunger";

            EM_NarrativeTemplateDefinition waterUrgency = CreateDefinition("Need Urgency - Water", EM_NarrativeEventType.NeedUrgency,
                EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Need, "[{time}] {subject} needs water",
                "Thirst urgency reached {value}.");
            waterUrgency.NeedIdEquals = "Water";

            EM_NarrativeTemplateDefinition sleepUrgency = CreateDefinition("Need Urgency - Sleep", EM_NarrativeEventType.NeedUrgency,
                EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Need, "[{time}] {subject} is exhausted",
                "Sleep urgency reached {value}.");
            sleepUrgency.NeedIdEquals = "Sleep";

            EM_NarrativeTemplateDefinition hungerRelief = CreateDefinition("Need Relief - Hunger", EM_NarrativeEventType.NeedRelief,
                EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Need, "[{time}] {subject} satisfied hunger",
                "Hunger urgency dropped to {value}.");
            hungerRelief.NeedIdEquals = "Hunger";

            EM_NarrativeTemplateDefinition waterRelief = CreateDefinition("Need Relief - Water", EM_NarrativeEventType.NeedRelief,
                EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Need, "[{time}] {subject} quenched thirst",
                "Water urgency dropped to {value}.");
            waterRelief.NeedIdEquals = "Water";

            EM_NarrativeTemplateDefinition sleepRelief = CreateDefinition("Need Relief - Sleep", EM_NarrativeEventType.NeedRelief,
                EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Need, "[{time}] {subject} feels rested",
                "Sleep urgency dropped to {value}.");
            sleepRelief.NeedIdEquals = "Sleep";

            EM_NarrativeTemplateDefinition scheduleStart = CreateDefinition("Schedule Start - Regular",
                EM_NarrativeEventType.ScheduleStart, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} starts {activity}", "Starting scheduled activity.");
            scheduleStart.Verbosity = EM_NarrativeVerbosity.Low;

            EM_NarrativeTemplateDefinition scheduleEnd = CreateDefinition("Schedule End - Regular",
                EM_NarrativeEventType.ScheduleEnd, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} ends {activity}", "Returning to schedule.");
            scheduleEnd.Verbosity = EM_NarrativeVerbosity.Low;

            EM_NarrativeTemplateDefinition seekFoodOverride = CreateDefinition("Override - Seek Food",
                EM_NarrativeEventType.ScheduleOverrideStart, EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} interrupts to seek food", "Override activity: {activity}.");
            seekFoodOverride.ActivityIdEquals = "Override.SeekFood";

            EM_NarrativeTemplateDefinition seekWaterOverride = CreateDefinition("Override - Seek Water",
                EM_NarrativeEventType.ScheduleOverrideStart, EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} interrupts to seek water", "Override activity: {activity}.");
            seekWaterOverride.ActivityIdEquals = "Override.SeekWater";

            EM_NarrativeTemplateDefinition sleepOverride = CreateDefinition("Override - Sleep",
                EM_NarrativeEventType.ScheduleOverrideStart, EM_NarrativeSeverity.Warning, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} interrupts to sleep", "Override activity: {activity}.");
            sleepOverride.ActivityIdEquals = "Override.Sleep";

            EM_NarrativeTemplateDefinition overrideEnd = CreateDefinition("Override End - Return to Schedule",
                EM_NarrativeEventType.ScheduleOverrideEnd, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Schedule,
                "[{time}] {subject} returns to schedule", "Override {activity} ended.");

            EM_NarrativeTemplateDefinition foodResource = CreateDefinition("Resource Change - Food",
                EM_NarrativeEventType.ResourceChange, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Resource,
                "[{time}] {subject} food updated", "Food changed by {delta} (now {after}).");
            foodResource.ResourceIdEquals = "Food";

            EM_NarrativeTemplateDefinition waterResource = CreateDefinition("Resource Change - Water",
                EM_NarrativeEventType.ResourceChange, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Resource,
                "[{time}] {subject} water updated", "Water changed by {delta} (now {after}).");
            waterResource.ResourceIdEquals = "Water";

            EM_NarrativeTemplateDefinition sleepResource = CreateDefinition("Resource Change - Sleep",
                EM_NarrativeEventType.ResourceChange, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Resource,
                "[{time}] {subject} rest updated", "Sleep changed by {delta} (now {after}).");
            sleepResource.ResourceIdEquals = "Sleep";

            EM_NarrativeTemplateDefinition tradeFood = CreateDefinition("Trade Success - Food",
                EM_NarrativeEventType.TradeSuccess, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Trade,
                "[{time}] {subject} obtained food", "Resolved {need} with {target} (amount {value}).");
            tradeFood.ResourceIdEquals = "Food";

            EM_NarrativeTemplateDefinition tradeWater = CreateDefinition("Trade Success - Water",
                EM_NarrativeEventType.TradeSuccess, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Trade,
                "[{time}] {subject} obtained water", "Resolved {need} with {target} (amount {value}).");
            tradeWater.ResourceIdEquals = "Water";

            EM_NarrativeTemplateDefinition workFood = CreateDesignerDefinition("Signal - Work Tick Food",
                EM_NarrativeEventType.SignalRaw, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Signal | EM_NarrativeTagMask.Designer,
                "[{time}] Work tick", "{signal} value {value} from {subject}.");
            workFood.SignalIdEquals = "Signal.Work.Tick.ProduceFood";
            workFood.Verbosity = EM_NarrativeVerbosity.Detailed;

            EM_NarrativeTemplateDefinition workWater = CreateDesignerDefinition("Signal - Work Tick Water",
                EM_NarrativeEventType.SignalRaw, EM_NarrativeSeverity.Info, EM_NarrativeTagMask.Signal | EM_NarrativeTagMask.Designer,
                "[{time}] Work tick", "{signal} value {value} from {subject}.");
            workWater.SignalIdEquals = "Signal.Work.Tick.ProduceWater";
            workWater.Verbosity = EM_NarrativeVerbosity.Detailed;

            return new EM_NarrativeTemplateDefinition[]
            {
                hungerUrgency,
                waterUrgency,
                sleepUrgency,
                hungerRelief,
                waterRelief,
                sleepRelief,
                scheduleStart,
                scheduleEnd,
                seekFoodOverride,
                seekWaterOverride,
                sleepOverride,
                overrideEnd,
                foodResource,
                waterResource,
                sleepResource,
                tradeFood,
                tradeWater,
                workFood,
                workWater
            };
        }
        #endregion
    }
}
