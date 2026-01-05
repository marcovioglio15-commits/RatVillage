using System.Collections.Generic;

namespace EmergentMechanics
{
    internal static partial class EM_StudioValidator
    {
        #region Schedule
        private static void AppendScheduleIssues(List<EM_StudioValidationIssue> issues, string rootFolder)
        {
            List<EM_NpcSchedulePreset> schedules = EM_StudioAssetUtility.FindAssets<EM_NpcSchedulePreset>(rootFolder);

            for (int i = 0; i < schedules.Count; i++)
            {
                EM_NpcSchedulePreset schedule = schedules[i];

                if (schedule == null)
                    continue;

                EM_NpcSchedulePreset.ScheduleEntry[] entries = schedule.Entries;

                if (entries == null)
                    continue;

                for (int e = 0; e < entries.Length; e++)
                {
                    EM_NpcSchedulePreset.ScheduleEntry entry = entries[e];

                    if (entry.ActivityIdDefinition == null && !string.IsNullOrWhiteSpace(entry.ActivityId))
                    {
                        issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                            "Schedule entry activity missing Id Definition: " + schedule.name, schedule));
                    }

                    EM_NpcSchedulePreset.ScheduleSignalEntry[] signalEntries = entry.SignalEntries;

                    if (signalEntries == null)
                        continue;

                    for (int s = 0; s < signalEntries.Length; s++)
                    {
                        EM_NpcSchedulePreset.ScheduleSignalEntry signalEntry = signalEntries[s];

                        if (signalEntry.StartSignalIdDefinition == null && !string.IsNullOrWhiteSpace(signalEntry.StartSignalId))
                        {
                            issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                                "Schedule entry start signal missing Id Definition: " + schedule.name, schedule));
                        }

                        if (signalEntry.TickSignalIdDefinition == null && !string.IsNullOrWhiteSpace(signalEntry.TickSignalId))
                        {
                            issues.Add(new EM_StudioValidationIssue(EM_StudioIssueSeverity.Info,
                                "Schedule entry tick signal missing Id Definition: " + schedule.name, schedule));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
