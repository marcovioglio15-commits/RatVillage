using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Constants
        private const string LibraryAssetName = "EM_MechanicLibrary";
        private const string ScheduleAssetName = "EM_NpcSchedulePreset";
        #endregion

        #region Public API
        public static int CleanEmergenceScriptables(string rootFolder)
        {
            string resolvedRoot = EM_StudioAssetUtility.ResolveRootFolder(rootFolder);
            int deleted = 0;

            deleted += DeleteAssetsOfType<EM_MechanicLibrary>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_SignalDefinition>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_MetricDefinition>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_EffectDefinition>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_RuleSetDefinition>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_DomainDefinition>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_SocietyProfile>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_NpcSchedulePreset>(resolvedRoot);
            deleted += DeleteAssetsOfType<EM_IdDefinition>(resolvedRoot);

            if (deleted > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return deleted;
        }

        public static bool GeneratePreset(EM_StudioPresetType preset, string rootFolder, out EM_MechanicLibrary library,
            out EM_NpcSchedulePreset schedule)
        {
            library = null;
            schedule = null;

            Dictionary<string, EM_IdDefinition> idLookup = EM_StudioIdUtility.BuildIdLookup(rootFolder);
            EM_StudioPresetTuning tuning = GetTuning(preset);

            EM_IdDefinition signalNeedUrgencyId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Need.Urgency", "Need urgency signal", idLookup);
            EM_IdDefinition signalTradeSuccessId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Trade.Success", "Trade success signal", idLookup);
            EM_IdDefinition signalTradeFailId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Trade.Fail", "Trade fail signal", idLookup);
            EM_IdDefinition signalHealthValueId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Health.Value", "Health value signal", idLookup);
            EM_IdDefinition signalHealthDamageId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Health.Damage", "Health damage signal", idLookup);
            EM_IdDefinition signalWorkProduceFoodId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Work.Tick.ProduceFood", "Work food production tick", idLookup);
            EM_IdDefinition signalWorkProduceWaterId = EnsureId(rootFolder, EM_IdCategory.Signal, "Signal.Work.Tick.ProduceWater", "Work water production tick", idLookup);

            EM_IdDefinition metricNeedUrgencyId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Need.Urgency", "Need urgency metric", idLookup);
            EM_IdDefinition metricTradeSuccessId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Trade.Success", "Trade success metric", idLookup);
            EM_IdDefinition metricTradeFailId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Trade.Fail", "Trade fail metric", idLookup);
            EM_IdDefinition metricHealthValueId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Health.Value", "Health value metric", idLookup);
            EM_IdDefinition metricHealthDamageId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Health.Damage", "Health damage metric", idLookup);
            EM_IdDefinition metricWorkProduceFoodId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Work.ProduceFood", "Work food production metric", idLookup);
            EM_IdDefinition metricWorkProduceWaterId = EnsureId(rootFolder, EM_IdCategory.Metric, "Metric.Work.ProduceWater", "Work water production metric", idLookup);

            EM_IdDefinition effectOverrideSeekFoodId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Override.SeekFood", "Override seek food", idLookup);
            EM_IdDefinition effectOverrideSeekWaterId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Override.SeekWater", "Override seek water", idLookup);
            EM_IdDefinition effectOverrideSleepId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Override.Sleep", "Override sleep", idLookup);
            EM_IdDefinition effectResolveNeedId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Intent.ResolveNeed", "Resolve need intent", idLookup);
            EM_IdDefinition effectRelationshipProviderUpId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Relationship.Provider.Up", "Provider relationship up", idLookup);
            EM_IdDefinition effectRelationshipProviderDownId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Relationship.Provider.Down", "Provider relationship down", idLookup);
            EM_IdDefinition effectRelationshipRequesterUpId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Relationship.Requester.Up", "Requester relationship up", idLookup);
            EM_IdDefinition effectRelationshipRequesterDownId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Relationship.Requester.Down", "Requester relationship down", idLookup);
            EM_IdDefinition effectHealthModifyId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Health.Modify", "Modify health", idLookup);
            EM_IdDefinition effectProduceFoodId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Resource.Produce.Food", "Produce food", idLookup);
            EM_IdDefinition effectProduceWaterId = EnsureId(rootFolder, EM_IdCategory.Effect, "Effect.Resource.Produce.Water", "Produce water", idLookup);

            EM_IdDefinition ruleSetNeedUrgencyId = EnsureId(rootFolder, EM_IdCategory.RuleSet, "RuleSet.Needs.Urgency", "Need urgency rules", idLookup);
            EM_IdDefinition ruleSetTradeSuccessId = EnsureId(rootFolder, EM_IdCategory.RuleSet, "RuleSet.Trade.Success", "Trade response rules", idLookup);
            EM_IdDefinition ruleSetHealthDamageId = EnsureId(rootFolder, EM_IdCategory.RuleSet, "RuleSet.Health.Damage", "Health damage rules", idLookup);
            EM_IdDefinition ruleSetWorkProductionId = EnsureId(rootFolder, EM_IdCategory.RuleSet, "RuleSet.Work.Production", "Work production rules", idLookup);

            EM_IdDefinition domainSurvivalId = EnsureId(rootFolder, EM_IdCategory.Domain, "Domain.Survival", "Survival domain", idLookup);
            EM_IdDefinition domainEconomyId = EnsureId(rootFolder, EM_IdCategory.Domain, "Domain.Economy", "Economy domain", idLookup);

            EM_IdDefinition profileRatVillageId = EnsureId(rootFolder, EM_IdCategory.Profile, "Society.RatVillage", "Default society profile", idLookup);

            EM_IdDefinition activitySeekFoodId = EnsureId(rootFolder, EM_IdCategory.Activity, "Override.SeekFood", "Override seek food", idLookup);
            EM_IdDefinition activitySeekWaterId = EnsureId(rootFolder, EM_IdCategory.Activity, "Override.SeekWater", "Override seek water", idLookup);
            EM_IdDefinition activitySleepId = EnsureId(rootFolder, EM_IdCategory.Activity, "Override.Sleep", "Override sleep", idLookup);
            EM_IdDefinition activityWorkId = EnsureId(rootFolder, EM_IdCategory.Activity, "Work", "Work activity", idLookup);

            EM_IdDefinition needHungerId = EnsureId(rootFolder, EM_IdCategory.Need, "Hunger", "Hunger need", idLookup);
            EM_IdDefinition needWaterId = EnsureId(rootFolder, EM_IdCategory.Need, "Water", "Water need", idLookup);
            EM_IdDefinition needSleepId = EnsureId(rootFolder, EM_IdCategory.Need, "Sleep", "Sleep need", idLookup);

            EM_IdDefinition resourceFoodId = EnsureId(rootFolder, EM_IdCategory.Resource, "Food", "Food resource", idLookup);
            EM_IdDefinition resourceWaterId = EnsureId(rootFolder, EM_IdCategory.Resource, "Water", "Water resource", idLookup);

            EM_IdDefinition intentResolveNeedId = EnsureId(rootFolder, EM_IdCategory.Intent, "Intent.ResolveNeed", "Resolve need intent", idLookup);

            EM_SignalDefinition signalNeedUrgency = CreateSignal(rootFolder, "EM_Signal_Need.Urgency", signalNeedUrgencyId, "Signal.Need.Urgency",
                "Emitted when a need urgency value changes.");
            EM_SignalDefinition signalTradeSuccess = CreateSignal(rootFolder, "EM_Signal_Trade.Success", signalTradeSuccessId, "Signal.Trade.Success",
                "Emitted when a trade succeeds.");
            EM_SignalDefinition signalTradeFail = CreateSignal(rootFolder, "EM_Signal_Trade.Fail", signalTradeFailId, "Signal.Trade.Fail",
                "Emitted when a trade fails.");
            EM_SignalDefinition signalHealthValue = CreateSignal(rootFolder, "EM_Signal_Health.Value", signalHealthValueId, "Signal.Health.Value",
                "Emitted with the current normalized health value.");
            EM_SignalDefinition signalHealthDamage = CreateSignal(rootFolder, "EM_Signal_Health.Damage", signalHealthDamageId, "Signal.Health.Damage",
                "Emitted when health damage is applied (normalized to max health).");
            EM_SignalDefinition signalWorkProduceFood = CreateSignal(rootFolder, "EM_Signal_Work.Tick.ProduceFood", signalWorkProduceFoodId,
                "Signal.Work.Tick.ProduceFood", "Emitted on work ticks to produce food.");
            EM_SignalDefinition signalWorkProduceWater = CreateSignal(rootFolder, "EM_Signal_Work.Tick.ProduceWater", signalWorkProduceWaterId,
                "Signal.Work.Tick.ProduceWater", "Emitted on work ticks to produce water.");

            float sampleIntervalHours = 0.5f * tuning.MetricIntervalMultiplier;
            EM_MetricDefinition metricNeedUrgency = CreateMetric(rootFolder, "EM_Metric_Need.Urgency", metricNeedUrgencyId, "Metric.Need.Urgency",
                signalNeedUrgency, sampleIntervalHours);
            EM_MetricDefinition metricTradeSuccess = CreateMetric(rootFolder, "EM_Metric_Trade.Success", metricTradeSuccessId, "Metric.Trade.Success",
                signalTradeSuccess, sampleIntervalHours);
            EM_MetricDefinition metricTradeFail = CreateMetric(rootFolder, "EM_Metric_Trade.Fail", metricTradeFailId, "Metric.Trade.Fail",
                signalTradeFail, sampleIntervalHours);
            EM_MetricDefinition metricHealthValue = CreateMetric(rootFolder, "EM_Metric_Health.Value", metricHealthValueId, "Metric.Health.Value",
                signalHealthValue, sampleIntervalHours);
            EM_MetricDefinition metricHealthDamage = CreateMetric(rootFolder, "EM_Metric_Health.Damage", metricHealthDamageId, "Metric.Health.Damage",
                signalHealthDamage, sampleIntervalHours);
            EM_MetricDefinition metricWorkProduceFood = CreateMetric(rootFolder, "EM_Metric_Work.ProduceFood", metricWorkProduceFoodId,
                "Metric.Work.ProduceFood", signalWorkProduceFood, sampleIntervalHours);
            EM_MetricDefinition metricWorkProduceWater = CreateMetric(rootFolder, "EM_Metric_Work.ProduceWater", metricWorkProduceWaterId,
                "Metric.Work.ProduceWater", signalWorkProduceWater, sampleIntervalHours);

            float overrideScale = tuning.EffectMagnitudeMultiplier;
            EM_EffectDefinition effectOverrideSeekFood = CreateOverrideEffect(rootFolder, "EM_Effect_Override.SeekFood", effectOverrideSeekFoodId,
                "Effect.Override.SeekFood", activitySeekFoodId, resourceFoodId, 1.5f * overrideScale);
            EM_EffectDefinition effectOverrideSeekWater = CreateOverrideEffect(rootFolder, "EM_Effect_Override.SeekWater", effectOverrideSeekWaterId,
                "Effect.Override.SeekWater", activitySeekWaterId, resourceWaterId, 1.5f * overrideScale);
            EM_EffectDefinition effectOverrideSleep = CreateOverrideEffect(rootFolder, "EM_Effect_Override.Sleep", effectOverrideSleepId,
                "Effect.Override.Sleep", activitySleepId, needSleepId, 6f * overrideScale);
            EM_EffectDefinition effectResolveNeed = CreateIntentEffect(rootFolder, "EM_Effect_Intent.ResolveNeed", effectResolveNeedId,
                "Effect.Intent.ResolveNeed", intentResolveNeedId, 1f * tuning.EffectMagnitudeMultiplier);

            float relationshipScale = tuning.EffectMagnitudeMultiplier;
            EM_EffectDefinition effectRelationshipProviderUp = CreateRelationshipEffect(rootFolder, "EM_Effect_Relationship.Provider.Up",
                effectRelationshipProviderUpId, "Effect.Relationship.Provider.Up", 0.01f * relationshipScale, EmergenceEffectTarget.SignalTarget);
            EM_EffectDefinition effectRelationshipProviderDown = CreateRelationshipEffect(rootFolder, "EM_Effect_Relationship.Provider.Down",
                effectRelationshipProviderDownId, "Effect.Relationship.Provider.Down", -0.01f * relationshipScale, EmergenceEffectTarget.SignalTarget);
            EM_EffectDefinition effectRelationshipRequesterUp = CreateRelationshipEffect(rootFolder, "EM_Effect_Relationship.Requester.Up",
                effectRelationshipRequesterUpId, "Effect.Relationship.Requester.Up", 0.05f * relationshipScale, EmergenceEffectTarget.EventTarget);
            EM_EffectDefinition effectRelationshipRequesterDown = CreateRelationshipEffect(rootFolder, "EM_Effect_Relationship.Requester.Down",
                effectRelationshipRequesterDownId, "Effect.Relationship.Requester.Down", -0.05f * relationshipScale, EmergenceEffectTarget.EventTarget);

            float healthScale = tuning.EffectMagnitudeMultiplier;
            EM_EffectDefinition effectHealthModify = CreateHealthEffect(rootFolder, "EM_Effect_Health.Modify", effectHealthModifyId,
                "Effect.Health.Modify", -1f * healthScale);

            float productionScale = tuning.EffectMagnitudeMultiplier;
            EM_EffectDefinition effectProduceFood = CreateResourceEffect(rootFolder, "EM_Effect_Resource.Produce.Food", effectProduceFoodId,
                "Effect.Resource.Produce.Food", resourceFoodId, 1f * productionScale);
            EM_EffectDefinition effectProduceWater = CreateResourceEffect(rootFolder, "EM_Effect_Resource.Produce.Water", effectProduceWaterId,
                "Effect.Resource.Produce.Water", resourceWaterId, 1f * productionScale);

            EM_RuleSetDefinition ruleSetNeedUrgency = CreateNeedUrgencyRuleSet(rootFolder, ruleSetNeedUrgencyId, "RuleSet.Needs.Urgency",
                metricNeedUrgency, effectOverrideSeekFood, effectOverrideSeekWater, effectOverrideSleep, effectResolveNeed,
                needHungerId, needWaterId, needSleepId);

            EM_RuleSetDefinition ruleSetTradeSuccess = CreateTradeRuleSet(rootFolder, ruleSetTradeSuccessId, "RuleSet.Trade.Success",
                metricTradeSuccess, metricTradeFail, effectRelationshipProviderUp, effectRelationshipProviderDown,
                effectRelationshipRequesterUp, effectRelationshipRequesterDown);

            EM_RuleSetDefinition ruleSetHealthDamage = CreateHealthDamageRuleSet(rootFolder, ruleSetHealthDamageId, "RuleSet.Health.Damage",
                metricHealthDamage, effectHealthModify);

            EM_RuleSetDefinition ruleSetWorkProduction = CreateWorkProductionRuleSet(rootFolder, ruleSetWorkProductionId, "RuleSet.Work.Production",
                metricWorkProduceFood, metricWorkProduceWater, effectProduceFood, effectProduceWater);

            EM_DomainDefinition domainSurvival = CreateDomain(rootFolder, "EM_Domain_Survival", domainSurvivalId, "Domain.Survival",
                new EM_RuleSetDefinition[] { ruleSetNeedUrgency, ruleSetHealthDamage }, new Color(1f, 0.608f, 0f, 1f));
            EM_DomainDefinition domainEconomy = CreateDomain(rootFolder, "EM_Domain_Economy", domainEconomyId, "Domain.Economy",
                new EM_RuleSetDefinition[] { ruleSetTradeSuccess, ruleSetWorkProduction }, new Color(0.2f, 0.6f, 1f, 1f));

            EM_SocietyProfile profile = CreateProfile(rootFolder, "EM_SocietyProfile", profileRatVillageId, "Society.RatVillage",
                new EM_DomainDefinition[] { domainSurvival, domainEconomy });

            library = CreateLibrary(rootFolder, new EM_SignalDefinition[]
                {
                    signalNeedUrgency,
                    signalTradeSuccess,
                    signalTradeFail,
                    signalHealthValue,
                    signalHealthDamage,
                    signalWorkProduceFood,
                    signalWorkProduceWater
                },
                new EM_MetricDefinition[]
                {
                    metricNeedUrgency,
                    metricTradeSuccess,
                    metricTradeFail,
                    metricHealthValue,
                    metricHealthDamage,
                    metricWorkProduceFood,
                    metricWorkProduceWater
                },
                new EM_EffectDefinition[]
                {
                    effectOverrideSeekFood,
                    effectOverrideSeekWater,
                    effectOverrideSleep,
                    effectResolveNeed,
                    effectRelationshipProviderUp,
                    effectRelationshipProviderDown,
                    effectRelationshipRequesterUp,
                    effectRelationshipRequesterDown,
                    effectHealthModify,
                    effectProduceFood,
                    effectProduceWater
                },
                new EM_RuleSetDefinition[] { ruleSetNeedUrgency, ruleSetTradeSuccess, ruleSetHealthDamage, ruleSetWorkProduction },
                new EM_DomainDefinition[] { domainSurvival, domainEconomy },
                new EM_SocietyProfile[] { profile });

            schedule = CreateSchedule(rootFolder, ScheduleAssetName, tuning, activitySeekFoodId, activitySeekWaterId, activitySleepId, activityWorkId,
                signalWorkProduceFoodId, signalWorkProduceWaterId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return library != null && schedule != null;
        }
        #endregion

        #region Helpers
        private static EM_IdDefinition EnsureId(string rootFolder, EM_IdCategory category, string id, string description,
            Dictionary<string, EM_IdDefinition> lookup)
        {
            return EM_StudioIdUtility.FindOrCreateId(rootFolder, category, id, description, lookup);
        }
        #endregion
    }
}
