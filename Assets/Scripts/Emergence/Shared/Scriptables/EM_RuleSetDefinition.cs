using System;
using UnityEngine;

namespace EmergentMechanics
{

    [CreateAssetMenu(menuName = "Emergence/Rule Set Definition", fileName = "EM_RuleSetDefinition")]
    public sealed class EM_RuleSetDefinition : ScriptableObject
    {
		#region Nested Types
		[Serializable]
        public struct MetricRuleEntry
        {
            #region Serialized
            #region Mapping
            [Tooltip("Metric sampled by this rule. Metrics define how signals are aggregated over time.")]
            [SerializeField] private EM_MetricDefinition metric;

            [Tooltip("Effect triggered when the rule fires. Effects are reusable outcome blocks that define what changes in the simulation.")]
            [SerializeField] private EM_EffectDefinition effect;
            #endregion

            #region Behavior
            [Tooltip("Probability curve evaluated using the normalized metric value (0-1). The curve result defines the chance to trigger the effect.")]
            [SerializeField] private AnimationCurve probabilityCurve;

            [Tooltip("Multiplier applied to the effect magnitude for this rule.")]
            [SerializeField] private float weight;

            [Tooltip("Minimum seconds between firings on the same target.")]
            [SerializeField] private float cooldownSeconds;
            #endregion
            #endregion

            #region Public Properties
            public EM_EffectDefinition Effect
            {
                get
                {
                    return effect;
                }
            }

            public EM_MetricDefinition Metric
            {
                get
                {
                    return metric;
                }
            }

            public AnimationCurve ProbabilityCurve
            {
                get
                {
                    return probabilityCurve;
                }
            }

            public float Weight
            {
                get
                {
                    return weight;
                }
            }

            public float CooldownSeconds
            {
                get
                {
                    return cooldownSeconds;
                }
            }

            #endregion

            #region Methods
            public bool EnsureDefaults()
            {
                if (probabilityCurve != null)
                    return false;

                probabilityCurve = EM_RuleSetDefinition.BuildDefaultProbabilityCurve();
                weight = 1f;
                return true;
            }
            #endregion
        }
		#endregion

		#region Serialized
		#region Identity
		[Tooltip("Unique key referenced by profiles and runtime masks. Keep stable once in use to avoid breaking profile mappings.")]
        [Header("Identity")]
        [SerializeField] private string ruleSetId = "RuleSet.Id";

        [Tooltip("Designer-facing label shown in tools and debug views. Safe to rename without affecting runtime links.")]
        [SerializeField] private string displayName = "Rule Set";
        #endregion

        #region Rules
        [Tooltip("Metric-to-effect rules evaluated on metric sample ticks. Use to map observations into outcomes.")]
        [Header("Rules")]
        [SerializeField] private MetricRuleEntry[] rules = new MetricRuleEntry[0];
        #endregion

        #region Notes
        [Tooltip("Notes about intent, tuning goals, and expected impact for this rule set.")]
        [Header("Notes")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public Properties

        public string RuleSetId
        {
            get
            {
                return ruleSetId;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public MetricRuleEntry[] Rules
        {
            get
            {
                return rules;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }
        #endregion

        #region Methods
        #region Unity Lifecycle
        private void OnValidate()
        {
            EnsureRuleDefaults();
        }
        #endregion

        #region Helpers
        private void EnsureRuleDefaults()
        {
            if (rules == null)
                return;

            // Initialize missing defaults for new or reset rule entries.
            for (int i = 0; i < rules.Length; i++)
            {
                MetricRuleEntry entry = rules[i];
                bool updated = entry.EnsureDefaults();

                if (!updated)
                    continue;

                rules[i] = entry;
            }
        }

        private static AnimationCurve BuildDefaultProbabilityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1f, 1f));
        }
        #endregion
        #endregion
    }
}
