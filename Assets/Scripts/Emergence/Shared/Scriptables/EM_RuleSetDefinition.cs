using System;
using UnityEngine;

namespace EmergentMechanics
{

    [CreateAssetMenu(menuName = "Emergence/Rule Set Definition", fileName = "EM_RuleSetDefinition")]
    public sealed class EM_RuleSetDefinition : ScriptableObject
    {
		#region Nested Types
        [Serializable]
        public struct RuleEffectEntry
        {
            #region Serialized
            [Tooltip("Effect triggered when the rule fires. Effects are reusable outcome blocks that define what changes in the simulation.")]
            [SerializeField] private EM_EffectDefinition effect;

            [Tooltip("Multiplier applied to the effect magnitude after the rule weight.")]
            [SerializeField] private float weight;
            #endregion

            #region Public Properties
            public EM_EffectDefinition Effect
            {
                get
                {
                    return effect;
                }
            }

            public float Weight
            {
                get
                {
                    return weight;
                }
            }
            #endregion

            #region Methods
            public bool EnsureDefaults()
            {
                if (weight > 0f)
                    return false;

                weight = 1f;
                return true;
            }
            #endregion
        }

        [Serializable]
        public struct RuleEntry
        {
            #region Serialized
            #region Mapping
            [Tooltip("Metric sampled by this rule. Metrics define how signals are aggregated over time.")]
            [SerializeField] private EM_MetricDefinition metric;

            [Tooltip("Optional id definition used as a context filter for metric samples.")]
            [EM_IdSelector(EM_IdCategory.Any)]
            [SerializeField] private EM_IdDefinition contextIdDefinition;

            [Tooltip("Legacy context id string (auto-migrated when missing an id definition).")]
            [SerializeField]
            [HideInInspector] private string contextIdFilter;

            [Tooltip("Effects applied when the rule fires. All effects in the list are applied together.")]
            [SerializeField] private RuleEffectEntry[] effects;
            #endregion

            #region Behavior
            [Tooltip("Probability curve evaluated using the normalized metric value (0-1). The curve result defines the chance to trigger the effects.")]
            [SerializeField] private AnimationCurve probabilityCurve;

            [Tooltip("Multiplier applied to all effects when this rule fires.")]
            [SerializeField] private float weight;

            [Tooltip("Minimum hours between firings on the same target (simulated time).")]
            [SerializeField] private float cooldownHours;
            #endregion
            #endregion

            #region Public Properties
            public EM_MetricDefinition Metric
            {
                get
                {
                    return metric;
                }
            }

            public RuleEffectEntry[] Effects
            {
                get
                {
                    return effects;
                }
            }

            public string ContextIdFilter
            {
                get
                {
                    return EM_IdUtility.ResolveId(contextIdDefinition, contextIdFilter);
                }
            }

            public EM_IdDefinition ContextIdDefinition
            {
                get
                {
                    return contextIdDefinition;
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
                    return Mathf.Max(0f, cooldownHours) * 3600f;
                }
            }

            #endregion

            #region Methods
            public bool EnsureDefaults()
            {
                bool updated = false;

                if (probabilityCurve == null)
                {
                    probabilityCurve = EM_RuleSetDefinition.BuildDefaultProbabilityCurve();
                    updated = true;
                }

                if (weight <= 0f)
                {
                    weight = 1f;
                    updated = true;
                }

                if (effects == null)
                    return updated;

                for (int i = 0; i < effects.Length; i++)
                {
                    RuleEffectEntry entry = effects[i];
                    bool entryUpdated = entry.EnsureDefaults();

                    if (!entryUpdated)
                        continue;

                    effects[i] = entry;
                    updated = true;
                }

                return updated;
            }
            #endregion
        }
		#endregion

		#region Serialized
		#region Identity
		[Tooltip("Id definition that supplies the unique key referenced by profiles and runtime masks.")]
        [Header("Identity")]
        [EM_IdSelector(EM_IdCategory.RuleSet)]
        [SerializeField] private EM_IdDefinition ruleSetIdDefinition;

        [Tooltip("Legacy rule set id string (auto-migrated when missing an id definition).")]
        [SerializeField]
        [HideInInspector] private string ruleSetId = "RuleSet.Id";
        #endregion

        #region Rules
        [Tooltip("Rules evaluated on metric samples. Each rule can apply multiple effects together.")]
        [Header("Rules")]
        [SerializeField] private RuleEntry[] rules = new RuleEntry[0];
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
                return EM_IdUtility.ResolveId(ruleSetIdDefinition, ruleSetId);
            }
        }

        public EM_IdDefinition RuleSetIdDefinition
        {
            get
            {
                return ruleSetIdDefinition;
            }
        }

        public RuleEntry[] Rules
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
                RuleEntry entry = rules[i];
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
