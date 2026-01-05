using Unity.Collections;

namespace EmergentMechanics
{
    public static class EM_IdUtility
    {
        #region Public Methods
        public static string ResolveId(EM_IdDefinition definition, string fallback)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                return definition.Id;

            if (string.IsNullOrWhiteSpace(fallback))
                return string.Empty;

            return fallback;
        }

        public static FixedString64Bytes ToFixed(EM_IdDefinition definition, string fallback)
        {
            string value = ResolveId(definition, fallback);

            if (string.IsNullOrWhiteSpace(value))
                return new FixedString64Bytes(string.Empty);

            return new FixedString64Bytes(value);
        }

        public static bool HasId(EM_IdDefinition definition, string fallback)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                return true;

            return !string.IsNullOrWhiteSpace(fallback);
        }
        #endregion
    }
}
