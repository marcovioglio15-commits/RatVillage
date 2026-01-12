namespace EmergentMechanics
{
    internal static partial class EM_NarrativeLogTemplatePresets
    {
        #region Template Build
        private static EM_NarrativeTemplateDefinition[] BuildSampleDefinitions()
        {
            return new EM_NarrativeTemplateDefinition[]
            {
                CreateDefinition("Need Urgency - Generic", EM_NarrativeEventType.NeedUrgency, EM_NarrativeSeverity.Warning,
                    EM_NarrativeTagMask.Need, "[{time}] {subject} needs {need}", "Urgency reached {value}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Need Relief - Generic", EM_NarrativeEventType.NeedRelief, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Need, "[{time}] {subject} feels relief about {need}", "Urgency dropped to {value}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Schedule Start - Generic", EM_NarrativeEventType.ScheduleStart, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Schedule, "[{time}] {subject} started {activity}", "Current focus: {activity}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Low),

                CreateDefinition("Schedule End - Generic", EM_NarrativeEventType.ScheduleEnd, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Schedule, "[{time}] {subject} finished {activity}", "Returning to schedule.")
                    .WithVerbosity(EM_NarrativeVerbosity.Low),

                CreateDefinition("Schedule Override Start", EM_NarrativeEventType.ScheduleOverrideStart, EM_NarrativeSeverity.Warning,
                    EM_NarrativeTagMask.Schedule, "[{time}] {subject} interrupted schedule", "Override activity: {activity}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Schedule Override End", EM_NarrativeEventType.ScheduleOverrideEnd, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Schedule, "[{time}] {subject} ended override {activity}", "Back to regular routine.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Trade Attempt", EM_NarrativeEventType.TradeAttempt, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Trade, "[{time}] {subject} looks for {resource}", "Trying to resolve {need} with {target}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Trade Success", EM_NarrativeEventType.TradeSuccess, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Trade, "[{time}] {subject} obtained {resource}",
                    "Resolved {need} with {target} (amount {value}).")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Trade Fail", EM_NarrativeEventType.TradeFail, EM_NarrativeSeverity.Warning,
                    EM_NarrativeTagMask.Trade, "[{time}] {subject} couldn't secure {resource}",
                    "Failed to resolve {need} with {target}. Reason: {reason}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Intent Created", EM_NarrativeEventType.IntentCreated, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Intent, "[{time}] {subject} formed intent {intent}",
                    "Need: {need} Resource: {resource} Desired: {value} Urgency: {delta}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Detailed),

                CreateDefinition("Resource Change", EM_NarrativeEventType.ResourceChange, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Resource, "[{time}] {subject} resource change", "{resource} changed by {delta} (now {after}).")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Health Low", EM_NarrativeEventType.HealthLow, EM_NarrativeSeverity.Warning,
                    EM_NarrativeTagMask.Health, "[{time}] {subject} looks unwell", "Health at {value}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Health Critical", EM_NarrativeEventType.HealthCritical, EM_NarrativeSeverity.Critical,
                    EM_NarrativeTagMask.Health, "[{time}] {subject} is in critical condition", "Health at {value}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Health Recovered", EM_NarrativeEventType.HealthRecovered, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Health, "[{time}] {subject} is recovering", "Health back to {value}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Standard),

                CreateDefinition("Health Damage", EM_NarrativeEventType.HealthDamage, EM_NarrativeSeverity.Warning,
                    EM_NarrativeTagMask.Health, "[{time}] {subject} is hurting", "Damage severity {value} from {need}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Detailed),

                CreateDefinition("Relationship Change", EM_NarrativeEventType.RelationshipChange, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Relationship, "[{time}] {subject} relationship shifted",
                    "Affinity with {target} changed by {delta} (now {after}).")
                    .WithVerbosity(EM_NarrativeVerbosity.Detailed),

                CreateDesignerDefinition("Signal Raw", EM_NarrativeEventType.SignalRaw, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Signal | EM_NarrativeTagMask.Designer, "[{time}] Signal {signal} from {subject}",
                    "Context {context} Value {value} Target {target}.")
                    .WithVerbosity(EM_NarrativeVerbosity.Detailed),

                CreateDesignerDefinition("Effect Raw", EM_NarrativeEventType.EffectRaw, EM_NarrativeSeverity.Info,
                    EM_NarrativeTagMask.Effect | EM_NarrativeTagMask.Designer, "[{time}] Effect {effect} applied",
                    "Target {target} Context {context} Delta {delta} (from {before} to {after}).")
                    .WithVerbosity(EM_NarrativeVerbosity.Detailed)
            };
        }

        private static EM_NarrativeTemplateDefinition CreateDefinition(string name, EM_NarrativeEventType eventType,
            EM_NarrativeSeverity severity, EM_NarrativeTagMask tags, string title, string body)
        {
            EM_NarrativeTemplateDefinition definition = EM_NarrativeTemplateDefinition.CreateDefault();
            definition.Name = name;
            definition.EventType = eventType;
            definition.Visibility = EM_NarrativeVisibility.Player;
            definition.Severity = severity;
            definition.Tags = tags;
            definition.TitleTemplate = title;
            definition.BodyTemplate = body;
            return definition;
        }

        private static EM_NarrativeTemplateDefinition CreateDesignerDefinition(string name, EM_NarrativeEventType eventType,
            EM_NarrativeSeverity severity, EM_NarrativeTagMask tags, string title, string body)
        {
            EM_NarrativeTemplateDefinition definition = CreateDefinition(name, eventType, severity, tags, title, body);
            definition.Visibility = EM_NarrativeVisibility.Designer;
            return definition;
        }
        #endregion
    }
}
