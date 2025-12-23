using System;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Defines a group of rules evaluated against incoming signals.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Rule Set Definition", fileName = "EM_RuleSetDefinition")]
    public sealed class EM_RuleSetDefinition : ScriptableObject
    {
        [Serializable]
        public struct SignalRuleEntry
        {
            #region Serialized
            // Serialized mapping
            #region Serialized - Mapping
            [Tooltip("Signal that triggers this rule. Signals represent events (time-of-day ticks, trades, needs, etc.) and can be emitted by gameplay or systems.")]
            [SerializeField] private EM_SignalDefinition signal;

            [Tooltip("Effect applied when the rule fires. Effects are reusable outcome blocks that define what changes in the simulation.")]
            [SerializeField] private EM_EffectDefinition effect;
            #endregion

            // Serialized behavior
            #region Serialized - Behavior
            [Tooltip("Ordering within the same signal group. Higher priority rules evaluate first and can dominate outcomes.")]
            [SerializeField] private int priority;

            [Tooltip("Multiplier applied to the effect magnitude for this rule. Use to tune this rule relative to others triggered by the same signal.")]
            [SerializeField] private float weight;

            [Tooltip("Signal values below this threshold are ignored. Use to suppress low-intensity events or curve tails.")]
            [SerializeField] private float minimumSignalValue;

            [Tooltip("Minimum seconds between firings on the same target. Use to prevent rapid repeat effects and to pace emergent feedback loops.")]
            [SerializeField] private float cooldownSeconds;
            #endregion
            #endregion

            #region Public
            /// <summary>
            /// Gets the signal definition for this rule.
            /// </summary>
            public EM_SignalDefinition Signal
            {
                get
                {
                    return signal;
                }
            }

            /// <summary>
            /// Gets the effect definition for this rule.
            /// </summary>
            public EM_EffectDefinition Effect
            {
                get
                {
                    return effect;
                }
            }

            /// <summary>
            /// Gets the priority of this rule.
            /// </summary>
            public int Priority
            {
                get
                {
                    return priority;
                }
            }

            /// <summary>
            /// Gets the weight of this rule.
            /// </summary>
            public float Weight
            {
                get
                {
                    return weight;
                }
            }

            /// <summary>
            /// Gets the minimum signal value required to trigger this rule.
            /// </summary>
            public float MinimumSignalValue
            {
                get
                {
                    return minimumSignalValue;
                }
            }

            /// <summary>
            /// Gets the cooldown in seconds for this rule.
            /// </summary>
            public float CooldownSeconds
            {
                get
                {
                    return cooldownSeconds;
                }
            }
            #endregion
        }

        #region Serialized
        // Serialized identity
        #region Serialized - Identity
        [Tooltip("Unique key referenced by profiles and runtime masks. Keep stable once in use to avoid breaking profile mappings.")]
        [SerializeField] private string ruleSetId = "RuleSet.Id";

        [Tooltip("Designer-facing label shown in tools and debug views. Safe to rename without affecting runtime links.")]
        [SerializeField] private string displayName = "Rule Set";
        #endregion

        // Serialized configuration
        #region Serialized - Configuration
        [Tooltip("Default on/off state for this rule set. Profiles can still override using rule set masks.")]
        [SerializeField] private bool isEnabled = true;

        [Tooltip("Domain classification used for filtering and tuning. Rule sets inherit the domain's enable flags and budget weight.")]
        [SerializeField] private EM_DomainDefinition domain;

        [Tooltip("Signal-to-effect rules evaluated when matching signals are emitted. Use to map events into outcomes.")]
        [SerializeField] private SignalRuleEntry[] rules = new SignalRuleEntry[0];
        #endregion

        // Serialized notes
        #region Serialized - Notes
        [Tooltip("Notes about intent, tuning goals, and expected impact for this rule set.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets the rule set identifier.
        /// </summary>
        public string RuleSetId
        {
            get
            {
                return ruleSetId;
            }
        }

        /// <summary>
        /// Gets the display name of this rule set.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        /// <summary>
        /// Gets whether this rule set is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
        }

        /// <summary>
        /// Gets the domain of this rule set.
        /// </summary>
        public EM_DomainDefinition Domain
        {
            get
            {
                return domain;
            }
        }

        /// <summary>
        /// Gets the rules defined in this rule set.
        /// </summary>
        public SignalRuleEntry[] Rules
        {
            get
            {
                return rules;
            }
        }

        /// <summary>
        /// Gets the designer description for this rule set.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
        }
        #endregion
    }
}
