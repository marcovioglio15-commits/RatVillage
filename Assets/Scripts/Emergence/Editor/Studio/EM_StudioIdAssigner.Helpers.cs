using System.Collections.Generic;
using UnityEditor;

namespace EmergentMechanics
{
    internal static partial class EM_StudioIdAssigner
    {
        #region Helpers
        private static bool AssignIdDefinition(SerializedProperty idDefinitionProperty, SerializedProperty legacyProperty,
            EM_IdCategory category, string rootFolder, Dictionary<string, EM_IdDefinition> lookup,
            string fallbackName, string prefix, bool allowFallback)
        {
            if (idDefinitionProperty == null)
                return false;

            if (idDefinitionProperty.objectReferenceValue != null)
                return false;

            string legacyValue = string.Empty;

            if (legacyProperty != null)
                legacyValue = legacyProperty.stringValue;

            string idValue = legacyValue;

            if (string.IsNullOrWhiteSpace(idValue) && allowFallback)
                idValue = BuildFallbackId(fallbackName, prefix);

            if (string.IsNullOrWhiteSpace(idValue))
                return false;

            EM_IdDefinition definition = EM_StudioIdUtility.FindOrCreateId(rootFolder, category, idValue, fallbackName, lookup);

            if (definition == null)
                return false;

            idDefinitionProperty.objectReferenceValue = definition;
            return true;
        }

        private static string BuildFallbackId(string assetName, string prefix)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                return string.Empty;

            string cleaned = assetName.Trim();

            if (cleaned.StartsWith("EM_"))
                cleaned = cleaned.Substring(3);

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                string prefixToken = prefix + "_";
                string prefixDot = prefix + ".";

                if (cleaned.StartsWith(prefixToken))
                    cleaned = cleaned.Substring(prefixToken.Length);
                else if (cleaned.StartsWith(prefixDot))
                    cleaned = cleaned.Substring(prefixDot.Length);
            }

            cleaned = cleaned.Replace('_', '.');

            if (string.IsNullOrWhiteSpace(prefix))
                return cleaned;

            return prefix + "." + cleaned;
        }

        private static EmergenceEffectType GetEffectType(SerializedObject serialized)
        {
            SerializedProperty effectTypeProperty = serialized.FindProperty("effectType");

            if (effectTypeProperty == null)
                return EmergenceEffectType.ModifyNeed;

            return (EmergenceEffectType)effectTypeProperty.enumValueIndex;
        }

        private static EM_IdCategory ResolveEffectParameterCategory(EmergenceEffectType effectType)
        {
            if (effectType == EmergenceEffectType.ModifyNeed)
                return EM_IdCategory.Need;

            if (effectType == EmergenceEffectType.ModifyResource)
                return EM_IdCategory.Resource;

            if (effectType == EmergenceEffectType.ModifyHealth)
                return EM_IdCategory.Context;

            if (effectType == EmergenceEffectType.OverrideSchedule)
                return EM_IdCategory.Activity;

            if (effectType == EmergenceEffectType.AddIntent)
                return EM_IdCategory.Intent;

            if (effectType == EmergenceEffectType.EmitSignal)
                return EM_IdCategory.Signal;

            return EM_IdCategory.Context;
        }

        private static EM_IdCategory ResolveEffectSecondaryCategory(EmergenceEffectType effectType)
        {
            if (effectType == EmergenceEffectType.AddIntent)
                return EM_IdCategory.Resource;

            if (effectType == EmergenceEffectType.EmitSignal)
                return EM_IdCategory.Context;

            return EM_IdCategory.Context;
        }
        #endregion
    }
}
