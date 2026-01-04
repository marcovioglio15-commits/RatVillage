using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal struct EM_RuleEvaluationLookups
    {
        #region Data
        public BufferLookup<EM_BufferElement_RuleCooldown> CooldownLookup;
        public ComponentLookup<EM_Component_RandomSeed> RandomLookup;
        public ComponentLookup<EM_Component_SocietyMember> MemberLookup;
        public ComponentLookup<EM_Component_SocietyRoot> RootLookup;
        public BufferLookup<EM_BufferElement_Need> NeedLookup;
        public BufferLookup<EM_BufferElement_NeedSetting> NeedSettingLookup;
        public BufferLookup<EM_BufferElement_Resource> ResourceLookup;
        public BufferLookup<EM_BufferElement_Relationship> RelationshipLookup;
        public BufferLookup<EM_BufferElement_RelationshipType> RelationshipTypeLookup;
        public ComponentLookup<EM_Component_NpcType> NpcTypeLookup;
        public ComponentLookup<EM_Component_Reputation> ReputationLookup;
        public ComponentLookup<EM_Component_Cohesion> CohesionLookup;
        public ComponentLookup<EM_Component_NpcSchedule> ScheduleLookup;
        public ComponentLookup<EM_Component_NpcScheduleOverride> ScheduleOverrideLookup;
        public BufferLookup<EM_BufferElement_Intent> IntentLookup;
        public BufferLookup<EM_BufferElement_SignalEvent> SignalLookup;
        #endregion
    }

    internal static partial class EM_RuleEvaluation
    {
        #region Rules
        // Evaluates rules for a metric sample and applies all effects from triggered rules.
        public static bool TryEvaluateRules(ref EM_Blob_Library libraryBlob, ref NativeParallelHashMap<int, int> ruleGroupLookup,
            int metricIndex, float normalized, double time, Entity subject, Entity societyRoot, Entity signalTarget, FixedString64Bytes contextId,
            bool hasProfile, BlobAssetReference<EM_Blob_SocietyProfile> profileBlob, ref EM_RuleEvaluationLookups lookups,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            if (!hasProfile)
                return false;

            int groupIndex;
            bool foundGroup = ruleGroupLookup.TryGetValue(metricIndex, out groupIndex);

            if (!foundGroup)
                return false;

            ref BlobArray<EM_Blob_RuleGroup> ruleGroups = ref libraryBlob.RuleGroups;
            ref BlobArray<EM_Blob_Rule> rules = ref libraryBlob.Rules;
            ref BlobArray<EM_Blob_RuleEffect> ruleEffects = ref libraryBlob.RuleEffects;
            ref BlobArray<EM_Blob_Effect> effects = ref libraryBlob.Effects;
            ref BlobArray<EM_Blob_ProbabilityCurve> curves = ref libraryBlob.Curves;
            ref BlobArray<byte> ruleSetMask = ref profileBlob.Value.RuleSetMask;

            EM_Blob_RuleGroup group = ruleGroups[groupIndex];

            if (group.Length <= 0)
                return false;

            if (!lookups.RandomLookup.HasComponent(subject))
                return false;

            EM_Component_RandomSeed seed = lookups.RandomLookup[subject];
            bool triggered = false;

            for (int r = group.StartIndex; r < group.StartIndex + group.Length; r++)
            {
                EM_Blob_Rule rule = rules[r];

                if (rule.RuleSetIndex >= 0 && rule.RuleSetIndex < ruleSetMask.Length && ruleSetMask[rule.RuleSetIndex] == 0)
                    continue;

                if (rule.ContextId.Length > 0 && !rule.ContextId.Equals(contextId))
                    continue;

                if (rule.CurveIndex < 0 || rule.CurveIndex >= curves.Length)
                    continue;

                ref EM_Blob_ProbabilityCurve curve = ref curves[rule.CurveIndex];
                float probability = SampleCurve(ref curve, normalized);

                if (probability <= 0f)
                    continue;

                float randomValue = NextRandom01(ref seed);

                if (randomValue > probability)
                    continue;

                Entity cooldownTarget = subject;

                if (!TryConsumeCooldown(cooldownTarget, r, rule.CooldownSeconds, time, ref lookups.CooldownLookup))
                    continue;

                int effectStart = rule.EffectStartIndex;
                int effectEnd = rule.EffectStartIndex + rule.EffectLength;

                for (int e = effectStart; e < effectEnd; e++)
                {
                    if (e < 0 || e >= ruleEffects.Length)
                        continue;

                    EM_Blob_RuleEffect ruleEffect = ruleEffects[e];

                    if (ruleEffect.EffectIndex < 0 || ruleEffect.EffectIndex >= effects.Length)
                        continue;

                    EM_Blob_Effect effect = effects[ruleEffect.EffectIndex];
                    Entity resolvedTarget;

                    if (!TryResolveEffectTarget(subject, societyRoot, signalTarget, effect.Target, lookups.MemberLookup, lookups.RootLookup, out resolvedTarget))
                        continue;

                    float magnitude = effect.Magnitude * rule.Weight * ruleEffect.Weight;

                    if (effect.UseClamp != 0)
                        magnitude = math.clamp(magnitude, effect.MinValue, effect.MaxValue);

                    bool applied = ApplyEffect(effect, magnitude, resolvedTarget, subject, signalTarget, societyRoot, contextId,
                        ref lookups, hasDebugBuffer, debugBuffer, maxEntries);

                    if (applied)
                        triggered = true;
                }
            }

            lookups.RandomLookup[subject] = seed;
            return triggered;
        }
        #endregion

        #region Cooldown
        private static bool TryConsumeCooldown(Entity target, int ruleIndex, float cooldownSeconds, double time,
            ref BufferLookup<EM_BufferElement_RuleCooldown> cooldownLookup)
        {
            if (cooldownSeconds <= 0f)
                return true;

            if (target == Entity.Null)
                return true;

            if (!cooldownLookup.HasBuffer(target))
                return true;

            DynamicBuffer<EM_BufferElement_RuleCooldown> cooldowns = cooldownLookup[target];

            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i].RuleIndex != ruleIndex)
                    continue;

                EM_BufferElement_RuleCooldown entry = cooldowns[i];

                if (time < entry.NextAllowedTime)
                    return false;

                entry.NextAllowedTime = time + cooldownSeconds;
                cooldowns[i] = entry;
                return true;
            }

            cooldowns.Add(new EM_BufferElement_RuleCooldown
            {
                RuleIndex = ruleIndex,
                NextAllowedTime = time + cooldownSeconds
            });

            return true;
        }
        #endregion

        #region TargetResolution
        private static bool TryResolveEffectTarget(Entity subject, Entity societyRoot, Entity signalTarget, EmergenceEffectTarget targetMode,
            ComponentLookup<EM_Component_SocietyMember> memberLookup, ComponentLookup<EM_Component_SocietyRoot> rootLookup,
            out Entity resolvedTarget)
        {
            resolvedTarget = Entity.Null;

            if (targetMode == EmergenceEffectTarget.EventTarget)
            {
                resolvedTarget = subject;
                return subject != Entity.Null;
            }

            if (targetMode == EmergenceEffectTarget.SignalTarget)
            {
                resolvedTarget = signalTarget;
                return signalTarget != Entity.Null;
            }

            if (societyRoot != Entity.Null)
            {
                resolvedTarget = societyRoot;
                return true;
            }

            if (subject != Entity.Null && rootLookup.HasComponent(subject))
            {
                resolvedTarget = subject;
                return true;
            }

            if (subject == Entity.Null || !memberLookup.HasComponent(subject))
                return false;

            Entity root = memberLookup[subject].SocietyRoot;

            if (root == Entity.Null)
                return false;

            resolvedTarget = root;
            return true;
        }
        #endregion

        #region Probability
        private static float SampleCurve(ref EM_Blob_ProbabilityCurve curve, float normalized)
        {
            int count = curve.Samples.Length;

            if (count == 0)
                return 0f;

            if (count == 1)
                return math.clamp(curve.Samples[0], 0f, 1f);

            float t = math.clamp(normalized, 0f, 1f);
            float scaled = t * (count - 1);
            int index = (int)math.floor(scaled);
            int next = math.min(index + 1, count - 1);
            float lerp = scaled - index;
            float value = math.lerp(curve.Samples[index], curve.Samples[next], lerp);

            return math.clamp(value, 0f, 1f);
        }
        #endregion

        #region Random
        private static float NextRandom01(ref EM_Component_RandomSeed seed)
        {
            uint current = seed.Value;

            if (current == 0)
                current = 1u;

            Random random = Random.CreateFromIndex(current);
            float value = random.NextFloat();
            seed.Value = random.NextUInt();

            return value;
        }
        #endregion
    }
}
