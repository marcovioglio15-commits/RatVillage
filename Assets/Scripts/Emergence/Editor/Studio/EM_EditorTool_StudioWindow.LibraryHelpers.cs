using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_EditorTool_StudioWindow
    {
        #region Library Helpers
        private void AddItemsForCategory()
        {
            if (library == null)
                return;

            if (selectedCategory == EM_Categories.Signals)
                AddItems(library.Signals);
            else if (selectedCategory == EM_Categories.RuleSets)
                AddItems(library.RuleSets);
            else if (selectedCategory == EM_Categories.Effects)
                AddItems(library.Effects);
            else if (selectedCategory == EM_Categories.Metrics)
                AddItems(library.Metrics);
            else if (selectedCategory == EM_Categories.Domains)
                AddItems(library.Domains);
            else if (selectedCategory == EM_Categories.Profiles)
                AddItems(library.Profiles);
        }

        private void AddItems<T>(T[] source) where T : Object
        {
            if (source == null)
                return;

            string filter = string.IsNullOrWhiteSpace(itemSearchFilter)
                ? string.Empty
                : itemSearchFilter.Trim().ToLowerInvariant();

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    string name = source[i].name != null ? source[i].name.ToLowerInvariant() : string.Empty;

                    if (!name.Contains(filter))
                        continue;
                }

                items.Add(source[i]);
            }
        }

        private void UpdateCreateButtonLabel()
        {
            if (createAssetButton == null)
                return;

            createAssetButton.text = "Create " + GetCategoryLabel(selectedCategory);
        }

        private void UpdateStatusLabel()
        {
            if (statusLabel == null)
                return;

            if (library == null)
            {
                statusLabel.text = "No library assigned.";
                return;
            }

            statusLabel.text = "Items: " + items.Count + " | Category: " + GetCategoryLabel(selectedCategory);
        }
        #endregion

        #region Id Checks
        private bool IsMissingIdDefinition(Object item)
        {
            EM_SignalDefinition signal = item as EM_SignalDefinition;

            if (signal != null)
                return signal.SignalIdDefinition == null;

            EM_MetricDefinition metric = item as EM_MetricDefinition;

            if (metric != null)
                return metric.MetricIdDefinition == null;

            EM_EffectDefinition effect = item as EM_EffectDefinition;

            if (effect != null)
                return effect.EffectIdDefinition == null;

            EM_RuleSetDefinition ruleSet = item as EM_RuleSetDefinition;

            if (ruleSet != null)
                return ruleSet.RuleSetIdDefinition == null;

            EM_DomainDefinition domain = item as EM_DomainDefinition;

            if (domain != null)
                return domain.DomainIdDefinition == null;

            EM_SocietyProfile profile = item as EM_SocietyProfile;

            if (profile != null)
                return profile.ProfileIdDefinition == null;

            return false;
        }
        #endregion

        #region Text Helpers
        private static string GetCategoryLabel(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return "Signal";

            if (category == EM_Categories.RuleSets)
                return "Rule Set";

            if (category == EM_Categories.Effects)
                return "Effect";

            if (category == EM_Categories.Metrics)
                return "Metric";

            if (category == EM_Categories.Domains)
                return "Domain";

            if (category == EM_Categories.Profiles)
                return "Profile";

            return "Asset";
        }

        private static string GetCategoryTooltip(EM_Categories category)
        {
            if (category == EM_Categories.Signals)
                return "Signals are observable values emitted by gameplay or systems.";

            if (category == EM_Categories.RuleSets)
                return "Rule Sets bind metric samples to effects using probability curves.";

            if (category == EM_Categories.Effects)
                return "Effects are reusable outcome blocks triggered by rules.";

            if (category == EM_Categories.Metrics)
                return "Metrics sample a single signal on a fixed interval.";

            if (category == EM_Categories.Domains)
                return "Domains group related rule sets for readability and profiling.";

            if (category == EM_Categories.Profiles)
                return "Profiles select the active domains for a society.";

            return "Emergence asset category.";
        }
        #endregion
    }
}
