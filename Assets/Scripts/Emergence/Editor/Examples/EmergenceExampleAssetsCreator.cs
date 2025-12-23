using UnityEditor;
using UnityEngine;

namespace Emergence
{
    /// <summary>
    /// Creates example assets for a small Emergence village setup.
    /// </summary>
    internal static partial class EmergenceExampleAssetsCreator
    {
        #region Constants
        private const string RootFolder = "Assets/Scriptable Objects";
        #endregion

        #region Menu
        /// <summary>
        /// Creates example assets and adds them to the library.
        /// </summary>
        [MenuItem("Tools/Emergence/Create Example Village Assets")]
        public static void CreateExampleAssets()
        {
            EM_MechanicLibrary library = FindOrCreateLibrary();

            if (library == null)
                return;

            EM_DomainDefinition domainNeeds = CreateOrLoadDomain("EM_Domain_Needs.asset", "Domain.Needs", "Needs", new Color(0.9f, 0.6f, 0.2f, 1f));
            EM_DomainDefinition domainEconomy = CreateOrLoadDomain("EM_Domain_Economy.asset", "Domain.Economy", "Economy", new Color(0.2f, 0.7f, 0.3f, 1f));
            EM_DomainDefinition domainSocial = CreateOrLoadDomain("EM_Domain_Social.asset", "Domain.Social", "Social", new Color(0.2f, 0.5f, 0.8f, 1f));
            EM_DomainDefinition domainSchedule = CreateOrLoadDomain("EM_Domain_Schedule.asset", "Domain.Schedule", "Schedule", new Color(0.8f, 0.7f, 0.2f, 1f));

            EM_SignalDefinition signalSleepWindow = CreateOrLoadSignal("EM_Signal_Time_SleepWindow.asset", "Time.SleepWindow", "Sleep Window", domainSchedule);
            EM_SignalDefinition signalWorkWindow = CreateOrLoadSignal("EM_Signal_Time_WorkWindow.asset", "Time.WorkWindow", "Work Window", domainSchedule);
            EM_SignalDefinition signalLeisureWindow = CreateOrLoadSignal("EM_Signal_Time_LeisureWindow.asset", "Time.LeisureWindow", "Leisure Window", domainSchedule);
            EM_SignalDefinition signalSleepTick = CreateOrLoadSignal("EM_Signal_Time_SleepTick.asset", "Time.SleepTick", "Sleep Tick", domainSchedule);
            EM_SignalDefinition signalWorkTick = CreateOrLoadSignal("EM_Signal_Time_WorkTick.asset", "Time.WorkTick", "Work Tick", domainSchedule);
            EM_SignalDefinition signalLeisureTick = CreateOrLoadSignal("EM_Signal_Time_LeisureTick.asset", "Time.LeisureTick", "Leisure Tick", domainSchedule);
            EM_SignalDefinition signalTradeSuccess = CreateOrLoadSignal("EM_Signal_Trade_Success.asset", "Trade.Success", "Trade Success", domainEconomy);
            EM_SignalDefinition signalTradeFail = CreateOrLoadSignal("EM_Signal_Trade_Fail.asset", "Trade.Fail", "Trade Fail", domainEconomy);
            EM_SignalDefinition signalNeedHunger = CreateOrLoadSignal("EM_Signal_Need_Hunger.asset", "Need.Hunger", "Need Hunger", domainNeeds);
            EM_SignalDefinition signalNeedThirst = CreateOrLoadSignal("EM_Signal_Need_Thirst.asset", "Need.Thirst", "Need Thirst", domainNeeds);
            EM_SignalDefinition signalNeedSleep = CreateOrLoadSignal("EM_Signal_Need_Sleep.asset", "Need.Sleep", "Need Sleep", domainNeeds);

            EM_EffectDefinition effectSleepRecover = CreateOrLoadEffect("EM_Effect_SleepRecover.asset", "Effect.SleepRecover", "Sleep Recover",
                EmergenceEffectType.ModifyNeed, EmergenceEffectTarget.EventTarget, "Need.Sleep", -0.15f);
            EM_EffectDefinition effectWorkHunger = CreateOrLoadEffect("EM_Effect_WorkHunger.asset", "Effect.WorkHunger", "Work Hunger",
                EmergenceEffectType.ModifyNeed, EmergenceEffectTarget.EventTarget, "Need.Hunger", 0.05f);
            EM_EffectDefinition effectWorkThirst = CreateOrLoadEffect("EM_Effect_WorkThirst.asset", "Effect.WorkThirst", "Work Thirst",
                EmergenceEffectType.ModifyNeed, EmergenceEffectTarget.EventTarget, "Need.Thirst", 0.06f);
            EM_EffectDefinition effectLeisureHunger = CreateOrLoadEffect("EM_Effect_LeisureHunger.asset", "Effect.LeisureHunger", "Leisure Hunger",
                EmergenceEffectType.ModifyNeed, EmergenceEffectTarget.EventTarget, "Need.Hunger", 0.02f);
            EM_EffectDefinition effectLeisureThirst = CreateOrLoadEffect("EM_Effect_LeisureThirst.asset", "Effect.LeisureThirst", "Leisure Thirst",
                EmergenceEffectType.ModifyNeed, EmergenceEffectTarget.EventTarget, "Need.Thirst", 0.03f);
            EM_EffectDefinition effectTradeRepUp = CreateOrLoadEffect("EM_Effect_TradeReputationUp.asset", "Effect.TradeReputationUp", "Trade Reputation Up",
                EmergenceEffectType.ModifyReputation, EmergenceEffectTarget.EventTarget, string.Empty, 0.05f);
            EM_EffectDefinition effectTradeRepDown = CreateOrLoadEffect("EM_Effect_TradeReputationDown.asset", "Effect.TradeReputationDown", "Trade Reputation Down",
                EmergenceEffectType.ModifyReputation, EmergenceEffectTarget.EventTarget, string.Empty, -0.05f);

            ExampleRuleEntry[] scheduleEntries = new ExampleRuleEntry[]
            {
                new ExampleRuleEntry
                {
                    Signal = signalSleepTick,
                    Effect = effectSleepRecover,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                },
                new ExampleRuleEntry
                {
                    Signal = signalWorkTick,
                    Effect = effectWorkHunger,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                },
                new ExampleRuleEntry
                {
                    Signal = signalWorkTick,
                    Effect = effectWorkThirst,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                },
                new ExampleRuleEntry
                {
                    Signal = signalLeisureTick,
                    Effect = effectLeisureHunger,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                },
                new ExampleRuleEntry
                {
                    Signal = signalLeisureTick,
                    Effect = effectLeisureThirst,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                }
            };

            EM_RuleSetDefinition scheduleRules = CreateOrLoadRuleSet("EM_RuleSet_Schedule.asset", "RuleSet.Schedule", "Schedule Rules", domainSchedule,
                scheduleEntries);

            EM_RuleSetDefinition tradeSocialRules = CreateOrLoadRuleSet("EM_RuleSet_TradeSocial.asset", "RuleSet.TradeSocial", "Trade Social Rules", domainSocial,
                new ExampleRuleEntry
                {
                    Signal = signalTradeSuccess,
                    Effect = effectTradeRepUp,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                },
                new ExampleRuleEntry
                {
                    Signal = signalTradeFail,
                    Effect = effectTradeRepDown,
                    Priority = 0,
                    Weight = 1f,
                    MinimumSignalValue = 0f,
                    CooldownSeconds = 0f
                });

            EM_MetricDefinition metricPopulation = CreateOrLoadMetric("EM_Metric_Population.asset", "Metric.Population", "Population", EmergenceMetricType.PopulationCount, 5f);
            EM_MetricDefinition metricAverageNeed = CreateOrLoadMetric("EM_Metric_AverageNeed.asset", "Metric.AverageNeed", "Average Need", EmergenceMetricType.AverageNeed, 3f);
            EM_MetricDefinition metricCohesion = CreateOrLoadMetric("EM_Metric_Cohesion.asset", "Metric.Cohesion", "Social Cohesion", EmergenceMetricType.SocialCohesion, 5f);
            EM_MetricDefinition metricSignalRate = CreateOrLoadMetric("EM_Metric_SignalRate.asset", "Metric.SignalRate", "Signal Rate", EmergenceMetricType.SignalRate, 2f);

            EM_SocietyProfile profile = CreateOrLoadProfile("EM_SocietyProfile_ExampleVillage.asset", "Society.ExampleVillage", "Example Village",
                new EM_DomainDefinition[] { domainNeeds, domainEconomy, domainSocial, domainSchedule },
                new EM_RuleSetDefinition[] { scheduleRules, tradeSocialRules },
                new EM_MetricDefinition[] { metricPopulation, metricAverageNeed, metricCohesion, metricSignalRate });

            ConfigureDomainRuleSets(domainSchedule, scheduleRules);
            ConfigureDomainRuleSets(domainSocial, tradeSocialRules);

            AddToLibraryIfMissing(library, domainNeeds, "domains");
            AddToLibraryIfMissing(library, domainEconomy, "domains");
            AddToLibraryIfMissing(library, domainSocial, "domains");
            AddToLibraryIfMissing(library, domainSchedule, "domains");
            AddToLibraryIfMissing(library, signalSleepWindow, "signals");
            AddToLibraryIfMissing(library, signalWorkWindow, "signals");
            AddToLibraryIfMissing(library, signalLeisureWindow, "signals");
            AddToLibraryIfMissing(library, signalSleepTick, "signals");
            AddToLibraryIfMissing(library, signalWorkTick, "signals");
            AddToLibraryIfMissing(library, signalLeisureTick, "signals");
            AddToLibraryIfMissing(library, signalTradeSuccess, "signals");
            AddToLibraryIfMissing(library, signalTradeFail, "signals");
            AddToLibraryIfMissing(library, signalNeedHunger, "signals");
            AddToLibraryIfMissing(library, signalNeedThirst, "signals");
            AddToLibraryIfMissing(library, signalNeedSleep, "signals");
            AddToLibraryIfMissing(library, effectSleepRecover, "effects");
            AddToLibraryIfMissing(library, effectWorkHunger, "effects");
            AddToLibraryIfMissing(library, effectWorkThirst, "effects");
            AddToLibraryIfMissing(library, effectLeisureHunger, "effects");
            AddToLibraryIfMissing(library, effectLeisureThirst, "effects");
            AddToLibraryIfMissing(library, effectTradeRepUp, "effects");
            AddToLibraryIfMissing(library, effectTradeRepDown, "effects");
            AddToLibraryIfMissing(library, scheduleRules, "ruleSets");
            AddToLibraryIfMissing(library, tradeSocialRules, "ruleSets");
            AddToLibraryIfMissing(library, metricPopulation, "metrics");
            AddToLibraryIfMissing(library, metricAverageNeed, "metrics");
            AddToLibraryIfMissing(library, metricCohesion, "metrics");
            AddToLibraryIfMissing(library, metricSignalRate, "metrics");
            AddToLibraryIfMissing(library, profile, "profiles");

            CreateExamplePrefabs();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        #endregion
    }
}
