using System.Collections.Generic;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioValidator
    {
        #region Public API
        public static List<EM_StudioValidationIssue> BuildIssues(EM_MechanicLibrary library, string rootFolder)
        {
            List<EM_StudioValidationIssue> issues = new List<EM_StudioValidationIssue>();

            if (library == null)
            {
                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Error,
                    "No Mechanic Library assigned.", null));
                return issues;
            }

            AppendDuplicateIdIssues(issues, rootFolder);
            AppendLibraryCoverageIssues(issues, library, rootFolder);
            AppendMissingIdIssues(issues, rootFolder);
            AppendScheduleIssues(issues, rootFolder);

            return issues;
        }
        #endregion

        #region Duplicate IDs
        private static void AppendDuplicateIdIssues(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_IdDefinition> definitions = EM_StudioAssetUtility.FindAssets<EM_IdDefinition>(rootFolder);
            Dictionary<string, EM_IdDefinition> firstByKey = new Dictionary<string, EM_IdDefinition>();
            Dictionary<string, int> counts = new Dictionary<string, int>();

            for (int i = 0; i < definitions.Count; i++)
            {
                EM_IdDefinition definition = definitions[i];

                if (definition == null)
                    continue;

                string key = EM_StudioIdUtility.BuildKey(definition.Category, definition.Id);

                if (!counts.ContainsKey(key))
                    counts[key] = 0;

                counts[key] += 1;

                if (!firstByKey.ContainsKey(key))
                    firstByKey[key] = definition;
            }

            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (pair.Value <= 1)
                    continue;

                EM_IdDefinition first = firstByKey[pair.Key];
                string message = "Duplicate id definition detected: " + pair.Key + " (" + pair.Value + ")";
                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Warning, message, first));
            }
        }
        #endregion

        #region Library Coverage
        private static void AppendLibraryCoverageIssues(List<EM_StudioValidationIssue> issues, EM_MechanicLibrary library, string rootFolder)
        {
            AppendCoverageForCategory(issues, library.Signals, EM_StudioAssetUtility.FindAssets<EM_SignalDefinition>(rootFolder), "Signal");
            AppendCoverageForCategory(issues, library.Metrics, EM_StudioAssetUtility.FindAssets<EM_MetricDefinition>(rootFolder), "Metric");
            AppendCoverageForCategory(issues, library.Effects, EM_StudioAssetUtility.FindAssets<EM_EffectDefinition>(rootFolder), "Effect");
            AppendCoverageForCategory(issues, library.RuleSets, EM_StudioAssetUtility.FindAssets<EM_RuleSetDefinition>(rootFolder), "Rule Set");
            AppendCoverageForCategory(issues, library.Domains, EM_StudioAssetUtility.FindAssets<EM_DomainDefinition>(rootFolder), "Domain");
            AppendCoverageForCategory(issues, library.Profiles, EM_StudioAssetUtility.FindAssets<EM_SocietyProfile>(rootFolder), "Profile");
        }

        private static void AppendCoverageForCategory<T>(List<EM_StudioValidationIssue> issues, T[] libraryAssets, List<T> folderAssets, string label)
            where T : Object
        {
            HashSet<Object> librarySet = new HashSet<Object>();

            if (libraryAssets != null)
            {
                for (int i = 0; i < libraryAssets.Length; i++)
                {
                    if (libraryAssets[i] == null)
                        continue;

                    librarySet.Add(libraryAssets[i]);
                }
            }

            for (int i = 0; i < folderAssets.Count; i++)
            {
                T asset = folderAssets[i];

                if (asset == null)
                    continue;

                if (librarySet.Contains(asset))
                    continue;

                string message = label + " asset not listed in library: " + asset.name;
                issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info, message, asset));
            }
        }
        #endregion
    }
}
