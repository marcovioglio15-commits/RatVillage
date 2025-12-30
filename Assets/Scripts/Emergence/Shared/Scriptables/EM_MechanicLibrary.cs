using UnityEngine;

namespace EmergentMechanics
{
    [CreateAssetMenu(menuName = "Emergence/Mechanic Library", fileName = "EM_MechanicLibrary")]
    public sealed class EM_MechanicLibrary : ScriptableObject
    {
        #region Fields

        #region Serialized
        #region Collections
        [Tooltip("All signal definitions used by Emergence Studio and baking.")]
        [Header("Collections")]
        [SerializeField] private EM_SignalDefinition[] signals = new EM_SignalDefinition[0];

        [Tooltip("All rule set definitions used by Emergence Studio and baking.")]
        [SerializeField] private EM_RuleSetDefinition[] ruleSets = new EM_RuleSetDefinition[0];

        [Tooltip("All effect definitions referenced by rules.")]
        [SerializeField] private EM_EffectDefinition[] effects = new EM_EffectDefinition[0];

        [Tooltip("All metric definitions sampled by runtime telemetry.")]
        [SerializeField] private EM_MetricDefinition[] metrics = new EM_MetricDefinition[0];

        [Tooltip("All domain definitions used to group and tune systems.")]
        [SerializeField] private EM_DomainDefinition[] domains = new EM_DomainDefinition[0];

        [Tooltip("All society profiles used to tune and scale behavior.")]
        [SerializeField] private EM_SocietyProfile[] profiles = new EM_SocietyProfile[0];
        #endregion
        #endregion

        #region Public Properties
        public EM_SignalDefinition[] Signals
        {
            get
            {
                return signals;
            }
        }
        public EM_RuleSetDefinition[] RuleSets
        {
            get
            {
                return ruleSets;
            }
        }

        public EM_EffectDefinition[] Effects
        {
            get
            {
                return effects;
            }
        }


        public EM_MetricDefinition[] Metrics
        {
            get
            {
                return metrics;
            }
        }

        public EM_DomainDefinition[] Domains
        {
            get
            {
                return domains;
            }
        }


        public EM_SocietyProfile[] Profiles
        {
            get
            {
                return profiles;
            }
        }
        #endregion

        #endregion

        #region Methods

        #region Unity LyfeCycle

        private void OnValidate()
        {
            signals = RemoveNullEntries(signals);
            ruleSets = RemoveNullEntries(ruleSets);
            effects = RemoveNullEntries(effects);
            metrics = RemoveNullEntries(metrics);
            domains = RemoveNullEntries(domains);
            profiles = RemoveNullEntries(profiles);
        }
        #endregion

        #region Helpers

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
        #endregion
    }
}
