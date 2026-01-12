using UnityEditor;
using UnityEngine;

namespace EmergentMechanics
{
    internal static partial class EM_StudioPresetGenerator
    {
        #region Rule Sets
        private static EM_RuleSetDefinition CreateNeedUrgencyRuleSet(string rootFolder, EM_IdDefinition ruleSetIdDefinition, string legacyId,
            EM_MetricDefinition metric, EM_EffectDefinition overrideFood, EM_EffectDefinition overrideWater,
            EM_EffectDefinition overrideSleep, EM_EffectDefinition resolveNeed,
            EM_IdDefinition hungerId, EM_IdDefinition waterId, EM_IdDefinition sleepId)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.RuleSets, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/EM_RuleSet_Needs.Urgency.asset");
            EM_RuleSetDefinition asset = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("ruleSetIdDefinition").objectReferenceValue = ruleSetIdDefinition;
            serialized.FindProperty("ruleSetId").stringValue = legacyId;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 3;

            ConfigureRuleEntry(rulesProperty, 0, metric, hungerId, new EM_EffectDefinition[] { overrideFood, resolveNeed }, BuildUrgencyCurve());
            ConfigureRuleEntry(rulesProperty, 1, metric, sleepId, new EM_EffectDefinition[] { overrideSleep, resolveNeed }, BuildUrgencyCurve());
            ConfigureRuleEntry(rulesProperty, 2, metric, waterId, new EM_EffectDefinition[] { overrideWater, resolveNeed }, BuildUrgencyCurve());

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_RuleSetDefinition CreateTradeRuleSet(string rootFolder, EM_IdDefinition ruleSetIdDefinition, string legacyId,
            EM_MetricDefinition tradeSuccessMetric, EM_MetricDefinition tradeFailMetric,
            EM_EffectDefinition providerUp, EM_EffectDefinition providerDown,
            EM_EffectDefinition requesterUp, EM_EffectDefinition requesterDown)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.RuleSets, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/EM_RuleSet_Trades.asset");
            EM_RuleSetDefinition asset = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("ruleSetIdDefinition").objectReferenceValue = ruleSetIdDefinition;
            serialized.FindProperty("ruleSetId").stringValue = legacyId;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 2;

            ConfigureRuleEntry(rulesProperty, 0, tradeSuccessMetric, null,
                new EM_EffectDefinition[] { providerUp, requesterUp }, BuildAlwaysCurve());
            ConfigureRuleEntry(rulesProperty, 1, tradeFailMetric, null,
                new EM_EffectDefinition[] { requesterDown }, BuildAlwaysCurve());

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_RuleSetDefinition CreateHealthDamageRuleSet(string rootFolder, EM_IdDefinition ruleSetIdDefinition, string legacyId,
            EM_MetricDefinition damageMetric, EM_EffectDefinition healthModify)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.RuleSets, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/EM_RuleSet_Health.Damage.asset");
            EM_RuleSetDefinition asset = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("ruleSetIdDefinition").objectReferenceValue = ruleSetIdDefinition;
            serialized.FindProperty("ruleSetId").stringValue = legacyId;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 1;

            ConfigureRuleEntry(rulesProperty, 0, damageMetric, null, new EM_EffectDefinition[] { healthModify }, BuildAlwaysCurve());

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static EM_RuleSetDefinition CreateWorkProductionRuleSet(string rootFolder, EM_IdDefinition ruleSetIdDefinition, string legacyId,
            EM_MetricDefinition produceFoodMetric, EM_MetricDefinition produceWaterMetric,
            EM_EffectDefinition produceFoodEffect, EM_EffectDefinition produceWaterEffect)
        {
            string folder = EM_StudioAssetUtility.GetCategoryFolder(EM_Categories.RuleSets, rootFolder);
            EM_StudioAssetUtility.EnsureFolderExists(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/EM_RuleSet_Work.Production.asset");
            EM_RuleSetDefinition asset = ScriptableObject.CreateInstance<EM_RuleSetDefinition>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("ruleSetIdDefinition").objectReferenceValue = ruleSetIdDefinition;
            serialized.FindProperty("ruleSetId").stringValue = legacyId;

            SerializedProperty rulesProperty = serialized.FindProperty("rules");
            rulesProperty.arraySize = 2;

            ConfigureRuleEntry(rulesProperty, 0, produceFoodMetric, null,
                new EM_EffectDefinition[] { produceFoodEffect }, BuildAlwaysCurve());
            ConfigureRuleEntry(rulesProperty, 1, produceWaterMetric, null,
                new EM_EffectDefinition[] { produceWaterEffect }, BuildAlwaysCurve());

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void ConfigureRuleEntry(SerializedProperty rulesProperty, int index, EM_MetricDefinition metric,
            EM_IdDefinition contextIdDefinition, EM_EffectDefinition[] effects, AnimationCurve curve)
        {
            SerializedProperty ruleProperty = rulesProperty.GetArrayElementAtIndex(index);
            SerializedProperty metricProperty = ruleProperty.FindPropertyRelative("metric");
            SerializedProperty contextDefinitionProperty = ruleProperty.FindPropertyRelative("contextIdDefinition");
            SerializedProperty contextLegacyProperty = ruleProperty.FindPropertyRelative("contextIdFilter");
            SerializedProperty effectsProperty = ruleProperty.FindPropertyRelative("effects");
            SerializedProperty curveProperty = ruleProperty.FindPropertyRelative("probabilityCurve");
            SerializedProperty weightProperty = ruleProperty.FindPropertyRelative("weight");
            SerializedProperty cooldownProperty = ruleProperty.FindPropertyRelative("cooldownHours");

            metricProperty.objectReferenceValue = metric;
            contextDefinitionProperty.objectReferenceValue = contextIdDefinition;
            contextLegacyProperty.stringValue = contextIdDefinition != null ? contextIdDefinition.Id : string.Empty;

            effectsProperty.arraySize = effects.Length;

            for (int e = 0; e < effects.Length; e++)
            {
                SerializedProperty effectEntry = effectsProperty.GetArrayElementAtIndex(e);
                effectEntry.FindPropertyRelative("effect").objectReferenceValue = effects[e];
                effectEntry.FindPropertyRelative("weight").floatValue = 1f;
            }

            curveProperty.animationCurveValue = curve;
            weightProperty.floatValue = 1f;
            cooldownProperty.floatValue = 0f;
        }
        #endregion
    }
}
