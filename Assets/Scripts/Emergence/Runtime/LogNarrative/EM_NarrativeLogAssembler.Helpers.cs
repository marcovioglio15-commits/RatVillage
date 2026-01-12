using System;
using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public sealed partial class EM_NarrativeLogAssembler
    {
        #region Thresholds
        private struct EM_NarrativeThresholds
        {
            public float NeedMedium;
            public float NeedHigh;
            public float NeedCritical;
            public float NeedRelief;
            public float NeedCooldownHours;
            public float HealthLow;
            public float HealthCritical;
            public float HealthRecovery;
            public float HealthDamageMin;
            public float HealthCooldownHours;
            public float ResourceDeltaMin;
            public float ResourceLow;
            public float ResourceDepleted;
            public float RelationshipDeltaMin;
            public float DedupWindowHours;
            public EM_NarrativeVerbosity Verbosity;
            public bool IncludeDesignerEntries;

            public static EM_NarrativeThresholds FromSettings(EM_NarrativeLogSettings settings)
            {
                EM_NarrativeThresholds thresholds = new EM_NarrativeThresholds
                {
                    NeedMedium = 0.6f,
                    NeedHigh = 0.8f,
                    NeedCritical = 0.95f,
                    NeedRelief = 0.4f,
                    NeedCooldownHours = 0.25f,
                    HealthLow = 0.35f,
                    HealthCritical = 0.15f,
                    HealthRecovery = 0.6f,
                    HealthDamageMin = 0.02f,
                    HealthCooldownHours = 0.25f,
                    ResourceDeltaMin = 1f,
                    ResourceLow = 1f,
                    ResourceDepleted = 0f,
                    RelationshipDeltaMin = 0.1f,
                    DedupWindowHours = 0.25f,
                    Verbosity = EM_NarrativeVerbosity.Standard,
                    IncludeDesignerEntries = false
                };

                if (settings == null)
                    return thresholds;

                thresholds.NeedMedium = settings.NeedMediumThreshold;
                thresholds.NeedHigh = settings.NeedHighThreshold;
                thresholds.NeedCritical = settings.NeedCriticalThreshold;
                thresholds.NeedRelief = settings.NeedReliefThreshold;
                thresholds.NeedCooldownHours = settings.NeedCooldownHours;
                thresholds.HealthLow = settings.HealthLowThreshold;
                thresholds.HealthCritical = settings.HealthCriticalThreshold;
                thresholds.HealthRecovery = settings.HealthRecoveryThreshold;
                thresholds.HealthDamageMin = settings.HealthDamageMin;
                thresholds.HealthCooldownHours = settings.HealthCooldownHours;
                thresholds.ResourceDeltaMin = settings.ResourceDeltaMin;
                thresholds.ResourceLow = settings.ResourceLowThreshold;
                thresholds.ResourceDepleted = settings.ResourceDepletedThreshold;
                thresholds.RelationshipDeltaMin = settings.RelationshipDeltaMin;
                thresholds.DedupWindowHours = settings.DedupWindowHours;
                thresholds.Verbosity = settings.Verbosity;
                thresholds.IncludeDesignerEntries = settings.IncludeDesignerEntries;
                return thresholds;
            }
        }
        #endregion

        #region Keys
        private enum EM_NarrativeNeedTier
        {
            Low,
            Medium,
            High,
            Critical
        }

        private enum EM_NarrativeHealthTier
        {
            Normal,
            Low,
            Critical
        }

        private struct EM_NarrativeNeedState
        {
            public EM_NarrativeNeedTier Tier;
            public float Urgency;
            public double LastEventTime;
        }

        private struct EM_NarrativeHealthState
        {
            public EM_NarrativeHealthTier Tier;
            public float Value;
            public double LastEventTime;
        }

        private struct EM_NarrativeNeedKey : IEquatable<EM_NarrativeNeedKey>
        {
            public Entity Subject;
            public FixedString64Bytes NeedId;

            public bool Equals(EM_NarrativeNeedKey other)
            {
                return Subject.Equals(other.Subject) && NeedId.Equals(other.NeedId);
            }

            public override bool Equals(object obj)
            {
                if (obj is EM_NarrativeNeedKey other)
                    return Equals(other);

                return false;
            }

            public override int GetHashCode()
            {
                int hash = Subject.GetHashCode();
                hash = (hash * 397) ^ NeedId.GetHashCode();
                return hash;
            }
        }

        private struct EM_NarrativeEventKey : IEquatable<EM_NarrativeEventKey>
        {
            public EM_NarrativeEventType EventType;
            public Entity Subject;
            public Entity Target;
            public FixedString64Bytes PrimaryId;

            public bool Equals(EM_NarrativeEventKey other)
            {
                return EventType == other.EventType && Subject.Equals(other.Subject) && Target.Equals(other.Target)
                    && PrimaryId.Equals(other.PrimaryId);
            }

            public override bool Equals(object obj)
            {
                if (obj is EM_NarrativeEventKey other)
                    return Equals(other);

                return false;
            }

            public override int GetHashCode()
            {
                int hash = (int)EventType;
                hash = (hash * 397) ^ Subject.GetHashCode();
                hash = (hash * 397) ^ Target.GetHashCode();
                hash = (hash * 397) ^ PrimaryId.GetHashCode();
                return hash;
            }
        }
        #endregion

        #region Resolution
        private EM_NarrativeEventKey BuildEventKey(EM_NarrativeEventType eventType, EM_BufferElement_NarrativeSignal signal)
        {
            FixedString64Bytes primaryId = ResolvePrimaryId(signal);

            return new EM_NarrativeEventKey
            {
                EventType = eventType,
                Subject = signal.Subject,
                Target = signal.Target,
                PrimaryId = primaryId
            };
        }

        private static FixedString64Bytes ResolvePrimaryId(EM_BufferElement_NarrativeSignal signal)
        {
            if (signal.NeedId.Length > 0)
                return signal.NeedId;

            if (signal.IntentId.Length > 0)
                return signal.IntentId;

            if (signal.ResourceId.Length > 0)
                return signal.ResourceId;

            if (signal.ActivityId.Length > 0)
                return signal.ActivityId;

            if (signal.SignalId.Length > 0)
                return signal.SignalId;

            if (signal.ContextId.Length > 0)
                return signal.ContextId;

            if (signal.ReasonId.Length > 0)
                return signal.ReasonId;

            return default;
        }

        private bool IsOnCooldown(EM_NarrativeEventKey key, double timeHours, float cooldownHours)
        {
            if (cooldownHours <= 0f)
                return false;

            if (timeHours < 0d)
                return false;

            double lastTime;

            if (!lastEventTimes.TryGetValue(key, out lastTime))
                return false;

            return timeHours - lastTime < cooldownHours;
        }

        private static bool IsTierOnCooldown(double lastEventTime, double timeHours, float cooldownHours)
        {
            if (cooldownHours <= 0f)
                return false;

            if (lastEventTime < 0d)
                return false;

            if (timeHours < 0d)
                return false;

            return timeHours - lastEventTime < cooldownHours;
        }

        private static EM_NarrativeNeedTier ResolveNeedTier(float urgency, EM_NarrativeThresholds thresholds)
        {
            if (urgency >= thresholds.NeedCritical)
                return EM_NarrativeNeedTier.Critical;

            if (urgency >= thresholds.NeedHigh)
                return EM_NarrativeNeedTier.High;

            if (urgency >= thresholds.NeedMedium)
                return EM_NarrativeNeedTier.Medium;

            return EM_NarrativeNeedTier.Low;
        }

        private static EM_NarrativeHealthTier ResolveHealthTier(float value, EM_NarrativeThresholds thresholds)
        {
            if (value <= thresholds.HealthCritical)
                return EM_NarrativeHealthTier.Critical;

            if (value <= thresholds.HealthLow)
                return EM_NarrativeHealthTier.Low;

            return EM_NarrativeHealthTier.Normal;
        }
        #endregion
    }
}
