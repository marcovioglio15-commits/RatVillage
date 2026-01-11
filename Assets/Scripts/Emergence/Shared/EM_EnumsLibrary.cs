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
        EmitSignal
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
}
