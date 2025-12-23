namespace Emergence
{
    /// <summary>
    /// Defines simulation LOD tiers for emergence processing.
    /// </summary>
    public enum EmergenceLodTier
    {
        Full = 0,
        Simplified = 1,
        Aggregated = 2
    }

    /// <summary>
    /// Defines effect application targets.
    /// </summary>
    public enum EmergenceEffectTarget
    {
        EventTarget = 0,
        SocietyRoot = 1
    }

    /// <summary>
    /// Defines supported effect types for rule execution.
    /// </summary>
    public enum EmergenceEffectType
    {
        ModifyNeed = 0,
        ModifyResource = 1,
        ModifyReputation = 2,
        ModifyCohesion = 3
    }

    /// <summary>
    /// Defines supported metric types for observation.
    /// </summary>
    public enum EmergenceMetricType
    {
        PopulationCount = 0,
        AverageNeed = 1,
        ResourceTotal = 2,
        SignalRate = 3,
        SocialCohesion = 4
    }
}
