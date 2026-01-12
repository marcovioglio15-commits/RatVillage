namespace EmergentMechanics
{
    #region Lod
    public enum EmergenceLodTier
    {
        Full,
        Simplified,
        Aggregated
    }
    #endregion

    #region Effect
    public enum EmergenceEffectTarget
    {
        EventTarget,
        SocietyRoot,
        SignalTarget
    }


    public enum EmergenceEffectType
    {
        ModifyNeed,
        ModifyResource,
        ModifyReputation,
        ModifyCohesion,
        OverrideSchedule,
        ModifyRelationship,
        AddIntent,
        EmitSignal,
        ModifyHealth
    }
    #endregion

    #region Navigation
    public enum EM_NpcDestinationKind : byte
    {
        None = 0,
        Activity = 1,
        TradeMeeting = 2,
        TradeQueue = 3
    }
    #endregion

    #region Metric
    public enum EmergenceMetricScope
    {
        Society,
        Member
    }

    public enum EmergenceMetricAggregation
    {
        No_Aggregation,
        Average,
        Sum,
        Min,
        Max,
        Count,
        Rate
    }

    public enum EmergenceMetricNormalization
    {
        Clamp01,
        Invert01,
        Signed01,
        Abs01
    }
    #endregion

    #region Signal
    public enum EmergenceSignalKind
    {
        Event,
        State
    }
    #endregion

    #region Sampling
    public enum EmergenceMetricSamplingMode
    {
        Aggregate,
        Event
    }
    #endregion

    #region Editor Tool
    public enum EM_Categories
    {
        Signals,
        RuleSets,
        Effects,
        Metrics,
        Domains,
        Profiles
    }

    public enum EM_DebugEventType
    {
        SignalEmitted,
        IntentCreated,
        EffectApplied,
        InteractionAttempt,
        InteractionSuccess,
        InteractionFail,
        ScheduleWindow,
        ScheduleEnd,
        ScheduleTick
    }
    #endregion

    #region Narrative
    public enum EM_NarrativeEventType
    {
        NeedUrgency,
        NeedRelief,
        ScheduleStart,
        ScheduleEnd,
        ScheduleOverrideStart,
        ScheduleOverrideEnd,
        TradeAttempt,
        TradeSuccess,
        TradeFail,
        IntentCreated,
        ResourceChange,
        HealthValue,
        HealthDamage,
        HealthLow,
        HealthCritical,
        HealthRecovered,
        RelationshipChange,
        SignalRaw,
        EffectRaw
    }

    public enum EM_NarrativeSeverity
    {
        Info,
        Warning,
        Critical
    }

    public enum EM_NarrativeVisibility
    {
        Player,
        Designer,
        Both
    }

    public enum EM_NarrativeVerbosity
    {
        Low,
        Standard,
        Detailed
    }

    [System.Flags]
    public enum EM_NarrativeTagMask : int
    {
        None = 0,
        Need = 1 << 0,
        Resource = 1 << 1,
        Trade = 1 << 2,
        Schedule = 1 << 3,
        Health = 1 << 4,
        Relationship = 1 << 5,
        Intent = 1 << 6,
        Signal = 1 << 7,
        Effect = 1 << 8,
        Designer = 1 << 9
    }
    #endregion
}
