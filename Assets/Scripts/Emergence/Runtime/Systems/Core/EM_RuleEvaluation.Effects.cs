using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Effects
        // Applies a single effect and writes debug events when enabled.
        public static bool ApplyEffect(EM_Blob_Effect effect, float magnitude, float rulePriority, Entity target, Entity subject, Entity signalTarget,
            Entity societyRoot, FixedString64Bytes contextId, double timeSeconds, ref EM_RuleEvaluationLookups lookups,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (target == Entity.Null)
                return false;

            float before = 0f;
            float after = 0f;
            float delta = 0f;
            bool applied = false;

            if (effect.EffectType == EmergenceEffectType.ModifyNeed)
            {
                applied = ApplyNeedDelta(target, effect.ParameterId, magnitude, ref lookups.NeedLookup, ref lookups.NeedSettingLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.ModifyResource)
            {
                applied = ApplyResourceDelta(target, effect.ParameterId, magnitude, ref lookups.ResourceLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.ModifyReputation)
            {
                applied = ApplyReputationDelta(target, magnitude, ref lookups.ReputationLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.ModifyCohesion)
            {
                applied = ApplyCohesionDelta(target, magnitude, ref lookups.CohesionLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.OverrideSchedule)
            {
                applied = ApplyScheduleOverride(target, effect.ParameterId, magnitude, timeSeconds, rulePriority, societyRoot,
                    ref lookups.MemberLookup, ref lookups.ScheduleOverrideSettingsLookup, ref lookups.ScheduleLookup,
                    ref lookups.ScheduleOverrideLookup, ref lookups.ScheduleOverrideGateLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.ModifyRelationship)
            {
                Entity other = ResolveRelationshipOther(target, subject, signalTarget);
                applied = ApplyRelationshipDelta(target, other, magnitude, ref lookups.RelationshipLookup, ref lookups.RelationshipTypeLookup,
                    ref lookups.NpcTypeLookup, out before, out after);
                delta = after - before;
            }
            else if (effect.EffectType == EmergenceEffectType.AddIntent)
            {
                bool created;
                FixedString64Bytes needId;
                FixedString64Bytes resourceId;
                float desiredAmount;

                applied = ApplyIntent(target, effect.ParameterId, effect.SecondaryId, contextId, magnitude, timeSeconds, ref lookups.IntentLookup,
                    ref lookups.NeedSettingLookup, out before, out after, out created, out needId, out resourceId, out desiredAmount);
                delta = after - before;

                if (applied && created && hasDebugBuffer)
                {
                    EM_Component_Event intentEvent = EM_Utility_LogEvent.BuildIntentEvent(effect.ParameterId, needId, resourceId, desiredAmount,
                        after, subject, target, societyRoot);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, intentEvent);
                }
            }
            else if (effect.EffectType == EmergenceEffectType.EmitSignal)
            {
                FixedString64Bytes emittedSignalId;
                FixedString64Bytes emittedContextId;

                applied = ApplyEmitSignal(target, signalTarget, effect.ParameterId, effect.SecondaryId, contextId, magnitude, timeSeconds,
                    societyRoot, ref lookups.SignalLookup, out emittedSignalId, out emittedContextId);
                delta = magnitude;

                if (applied && hasDebugBuffer)
                {
                    EM_Component_Event signalEvent = EM_Utility_LogEvent.BuildSignalEvent(emittedSignalId, magnitude, emittedContextId,
                        target, signalTarget, societyRoot);
                    EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, signalEvent);
                }
            }

            if (!applied || !hasDebugBuffer)
                return applied;

            EM_Component_Event effectEvent = EM_Utility_LogEvent.BuildEffectEvent(effect.EffectType, effect.ParameterId, contextId,
                delta, before, after, subject, target, societyRoot);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, effectEvent);
            return applied;
        }

        private static Entity ResolveRelationshipOther(Entity target, Entity subject, Entity signalTarget)
        {
            if (target == subject && signalTarget != Entity.Null)
                return signalTarget;

            if (target == signalTarget && subject != Entity.Null)
                return subject;

            if (signalTarget != Entity.Null && signalTarget != target)
                return signalTarget;

            if (subject != Entity.Null && subject != target)
                return subject;

            return Entity.Null;
        }
        #endregion
    }
}
