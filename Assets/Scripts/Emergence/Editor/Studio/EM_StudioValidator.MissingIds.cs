using System.Collections.Generic;

namespace EmergentMechanics
{
    internal static partial class EM_StudioValidator
    {
        #region Missing IDs
        private static void AppendMissingIdIssues(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            AppendMissingSignalIds(issues, rootFolder);
            AppendMissingMetricIds(issues, rootFolder);
            AppendMissingEffectIds(issues, rootFolder);
            AppendMissingRuleSetIds(issues, rootFolder);
            AppendMissingDomainIds(issues, rootFolder);
            AppendMissingProfileIds(issues, rootFolder);
        }

        private static void AppendMissingSignalIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_SignalDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_SignalDefinition>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_SignalDefinition asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.SignalIdDefinition != null)
                    continue;

                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                    "Signal missing Id Definition: " + asset.name, asset));
            }
        }

        private static void AppendMissingMetricIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_MetricDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_MetricDefinition>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_MetricDefinition asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.MetricIdDefinition != null)
                    continue;

                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                    "Metric missing Id Definition: " + asset.name, asset));
            }
        }

        private static void AppendMissingEffectIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_EffectDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_EffectDefinition>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_EffectDefinition asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.EffectIdDefinition == null)
                {
                    issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                        "Effect missing Id Definition: " + asset.name, asset));
                }

                if (asset.ParameterIdDefinition == null && !string.IsNullOrWhiteSpace(asset.ParameterId))
                {
                    issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                        "Effect parameter id missing Id Definition: " + asset.name, asset));
                }

                if (asset.SecondaryIdDefinition == null && !string.IsNullOrWhiteSpace(asset.SecondaryId))
                {
                    issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                        "Effect secondary id missing Id Definition: " + asset.name, asset));
                }
            }
        }

        private static void AppendMissingRuleSetIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_RuleSetDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_RuleSetDefinition>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_RuleSetDefinition asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.RuleSetIdDefinition == null)
                {
                    issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                        "Rule Set missing Id Definition: " + asset.name, asset));
                }

                EM_RuleSetDefinition.RuleEntry[] rules = asset.Rules;

                if (rules == null)
                    continue;

                for (int r = 0; r < rules.Length; r++)
                {
                    EM_RuleSetDefinition.RuleEntry rule = rules[r];

                    if (rule.ContextIdDefinition != null)
                        continue;

                    if (string.IsNullOrWhiteSpace(rule.ContextIdFilter))
                        continue;

                    issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                        "Rule Set context id missing Id Definition: " + asset.name, asset));
                }
            }
        }

        private static void AppendMissingDomainIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_DomainDefinition> assets = EM_StudioAssetUtility.FindAssets<EM_DomainDefinition>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_DomainDefinition asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.DomainIdDefinition != null)
                    continue;

                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                    "Domain missing Id Definition: " + asset.name, asset));
            }
        }

        private static void AppendMissingProfileIds(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_SocietyProfile> assets = EM_StudioAssetUtility.FindAssets<EM_SocietyProfile>(rootFolder);

            for (int i = 0; i < assets.Count; i++)
            {
                EM_SocietyProfile asset = assets[i];

                if (asset == null)
                    continue;

                if (asset.ProfileIdDefinition != null)
                    continue;

                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning,
                    "Profile missing Id Definition: " + asset.name, asset));
            }
        }
        #endregion
    }
}
