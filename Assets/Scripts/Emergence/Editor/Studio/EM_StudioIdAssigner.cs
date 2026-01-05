using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_StudioIdAssigner
    {
        #region Public API
        public static int AssignMissingIds(string rootFolder)
        {
            Dictionary<string, EM_IdDefinition> lookup = EM_StudioIdUtility.BuildIdLookup(rootFolder);
            int updatedCount = 0;

            updatedCount += AssignSignalIds(rootFolder, lookup);
            updatedCount += AssignMetricIds(rootFolder, lookup);
            updatedCount += AssignEffectIds(rootFolder, lookup);
            updatedCount += AssignRuleSetIds(rootFolder, lookup);
            updatedCount += AssignDomainIds(rootFolder, lookup);
            updatedCount += AssignProfileIds(rootFolder, lookup);
            updatedCount += AssignScheduleIds(rootFolder, lookup);

            if (updatedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return updatedCount;
        }
        #endregion
    }
}
