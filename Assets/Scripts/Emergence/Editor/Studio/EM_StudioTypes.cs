using UnityEngine;

namespace EmergentMechanics
{
    #region Enums
    internal enum EM_StudioTab
    {
        Inspector,
        IdRegistry,
        Validation,
        Presets
    }

    internal enum EM_StudioPresetType
    {
        Slow,
        Neutral,
        Aggressive
    }

    internal enum EM_StudioIssueSeverity
    {
        Info,
        Warning,
        Error
    }
    #endregion

    #region Structs
    internal struct EM_StudioValidationIssue
    {
        #region Fields
        public EM_StudioIssueSeverity Severity;
        public string Message;
        public Object Target;
        #endregion

        #region Constructors
        public EM_StudioValidationIssue(EM_StudioIssueSeverity severity, string message, Object target)
        {
            Severity = severity;
            Message = message;
            Target = target;
        }
        #endregion
    }
    #endregion
}
