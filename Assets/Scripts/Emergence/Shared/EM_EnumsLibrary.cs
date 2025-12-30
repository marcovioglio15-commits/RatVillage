namespace EmergentMechanics
{
    #region Lod
    public enum EmergenceLodTier
    {
        Full = 0,
        Simplified = 1,
        Aggregated = 2
    }
    #endregion

    #region Effect
    public enum EmergenceEffectTarget
    {
        EventTarget = 0,
        SocietyRoot = 1
    }


    public enum EmergenceEffectType
    {
        ModifyNeed = 0,
        ModifyResource = 1,
        ModifyReputation = 2,
        ModifyCohesion = 3,
        OverrideSchedule = 4
    }
    #endregion

    #region Metric
    public enum EmergenceMetricScope
    {
        Society = 0,
        Member = 1
    }

    public enum EmergenceMetricAggregation
    {
        Last = 0,
        Average = 1,
        Sum = 2,
        Min = 3,
        Max = 4,
        Count = 5,
        Rate = 6
    }

    public enum EmergenceMetricNormalization
    {
        Clamp01 = 0,
        Invert01 = 1,
        Signed01 = 2,
        Abs01 = 3
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
        ScheduleWindow,
        ScheduleTick,
        TradeAttempt,
        TradeSuccess,
        TradeFail,
        DistributionTransfer
    }
    #endregion
}
