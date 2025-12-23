using System.Collections.Generic;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Central registry for emergence definitions used by tools and baking.
    /// </summary>
    [CreateAssetMenu(menuName = "Emergence/Mechanic Library", fileName = "EM_MechanicLibrary")]
    public sealed class EM_MechanicLibrary : ScriptableObject
    {
        #region Serialized
        // Serialized collections
        #region Serialized - Collections
        [Tooltip("All signal definitions used by Emergence Studio and baking.")]
        [SerializeField] private EM_SignalDefinition[] signals = new EM_SignalDefinition[0];

        [Tooltip("All rule set definitions used by Emergence Studio and baking.")]
        [SerializeField] private EM_RuleSetDefinition[] ruleSets = new EM_RuleSetDefinition[0];

        [Tooltip("All effect definitions referenced by rules.")]
        [SerializeField] private EM_EffectDefinition[] effects = new EM_EffectDefinition[0];

        [Tooltip("All metric definitions sampled by runtime telemetry.")]
        [SerializeField] private EM_MetricDefinition[] metrics = new EM_MetricDefinition[0];

        [Tooltip("All norm definitions used by institutions.")]
        [SerializeField] private EM_NormDefinition[] norms = new EM_NormDefinition[0];

        [Tooltip("All institution definitions that enforce norms.")]
        [SerializeField] private EM_InstitutionDefinition[] institutions = new EM_InstitutionDefinition[0];

        [Tooltip("All domain definitions used to group and tune systems.")]
        [SerializeField] private EM_DomainDefinition[] domains = new EM_DomainDefinition[0];

        [Tooltip("All society profiles used to tune and scale behavior.")]
        [SerializeField] private EM_SocietyProfile[] profiles = new EM_SocietyProfile[0];
        #endregion
        #endregion

        #region Public
        /// <summary>
        /// Gets all signal definitions.
        /// </summary>
        public EM_SignalDefinition[] Signals
        {
            get
            {
                return signals;
            }
        }

        /// <summary>
        /// Gets all rule set definitions.
        /// </summary>
        public EM_RuleSetDefinition[] RuleSets
        {
            get
            {
                return ruleSets;
            }
        }

        /// <summary>
        /// Gets all effect definitions.
        /// </summary>
        public EM_EffectDefinition[] Effects
        {
            get
            {
                return effects;
            }
        }

        /// <summary>
        /// Gets all metric definitions.
        /// </summary>
        public EM_MetricDefinition[] Metrics
        {
            get
            {
                return metrics;
            }
        }

        /// <summary>
        /// Gets all norm definitions.
        /// </summary>
        public EM_NormDefinition[] Norms
        {
            get
            {
                return norms;
            }
        }

        /// <summary>
        /// Gets all institution definitions.
        /// </summary>
        public EM_InstitutionDefinition[] Institutions
        {
            get
            {
                return institutions;
            }
        }

        /// <summary>
        /// Gets all domain definitions.
        /// </summary>
        public EM_DomainDefinition[] Domains
        {
            get
            {
                return domains;
            }
        }

        /// <summary>
        /// Gets all society profiles.
        /// </summary>
        public EM_SocietyProfile[] Profiles
        {
            get
            {
                return profiles;
            }
        }
        #endregion

        #region Unity
        /// <summary>
        /// Validates the library content and removes null references.
        /// </summary>
        private void OnValidate()
        {
            signals = RemoveNullEntries(signals);
            ruleSets = RemoveNullEntries(ruleSets);
            effects = RemoveNullEntries(effects);
            metrics = RemoveNullEntries(metrics);
            norms = RemoveNullEntries(norms);
            institutions = RemoveNullEntries(institutions);
            domains = RemoveNullEntries(domains);
            profiles = RemoveNullEntries(profiles);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Removes null entries from an array without allocations in play mode.
        /// </summary>
        private static T[] RemoveNullEntries<T>(T[] source) where T : UnityEngine.Object
        {
            if (source == null)
                return new T[0];

            int count = 0;
            int index = 0;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                count++;
            }

            if (count == source.Length)
                return source;

            T[] result = new T[count];

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                result[index] = source[i];
                index++;
            }

            return result;
        }
        #endregion
    }
}
