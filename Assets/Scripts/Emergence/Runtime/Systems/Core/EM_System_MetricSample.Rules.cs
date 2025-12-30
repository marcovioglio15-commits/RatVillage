using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_MetricSample
    {
        #region Rule Evaluation
        // Evaluate rules mapped to a metric and apply effects.
        #region Evaluation
        private bool TryEvaluateRules(ref EM_Blob_Library libraryBlob, int metricIndex, float normalized, double time,
            Entity subject, Entity societyRoot, bool hasProfile, BlobAssetReference<EM_Blob_SocietyProfile> profileBlob)
        {
            if (!hasProfile)
                return false;

            int groupIndex;
            bool foundGroup = ruleGroupLookup.TryGetValue(metricIndex, out groupIndex);

            if (!foundGroup)
                return false;

            ref BlobArray<EM_Blob_RuleGroup> ruleGroups = ref libraryBlob.RuleGroups;
            ref BlobArray<EM_Blob_Rule> rules = ref libraryBlob.Rules;
            ref BlobArray<EM_Blob_Effect> effects = ref libraryBlob.Effects;
            ref BlobArray<EM_Blob_ProbabilityCurve> curves = ref libraryBlob.Curves;
            ref BlobArray<byte> ruleSetMask = ref profileBlob.Value.RuleSetMask;

            EM_Blob_RuleGroup group = ruleGroups[groupIndex];

            if (group.Length <= 0)
                return false;

            EM_Component_RandomSeed seed = randomLookup[subject];
            bool triggered = false;

            for (int r = group.StartIndex; r < group.StartIndex + group.Length; r++)
            {
                EM_Blob_Rule rule = rules[r];

                if (rule.RuleSetIndex >= 0 && rule.RuleSetIndex < ruleSetMask.Length && ruleSetMask[rule.RuleSetIndex] == 0)
                    continue;

                if (rule.EffectIndex < 0 || rule.EffectIndex >= effects.Length)
                    continue;

                if (rule.CurveIndex < 0 || rule.CurveIndex >= curves.Length)
                    continue;

                EM_Blob_Effect effect = effects[rule.EffectIndex];

                if (!TryResolveEffectTarget(subject, societyRoot, effect.Target, memberLookup, rootLookup, out Entity target))
                    continue;

                ref EM_Blob_ProbabilityCurve curve = ref curves[rule.CurveIndex];
                float probability = SampleCurve(ref curve, normalized);

                if (probability <= 0f)
                    continue;

                float randomValue = NextRandom01(ref seed);

                if (randomValue > probability)
                    continue;

                if (!TryConsumeCooldown(target, r, rule.CooldownSeconds, time))
                    continue;

                float magnitude = effect.Magnitude * rule.Weight;

                if (effect.UseClamp != 0)
                    magnitude = math.clamp(magnitude, effect.MinValue, effect.MaxValue);

                ApplyEffect(effect, target, magnitude, ref needLookup, ref resourceLookup, ref reputationLookup, ref cohesionLookup,
                    ref scheduleLookup, ref scheduleOverrideLookup);
                triggered = true;
            }

            randomLookup[subject] = seed;
            return triggered;
        }
        #endregion

        // Cooldown enforcement per rule and target.
        #region Cooldown
        private bool TryConsumeCooldown(Entity target, int ruleIndex, float cooldownSeconds, double time)
        {
            if (cooldownSeconds <= 0f)
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

        // Resolve the effect target using event and society context.
        #region TargetResolution
        private static bool TryResolveEffectTarget(Entity subject, Entity societyRoot, EmergenceEffectTarget targetMode,
            ComponentLookup<EM_Component_SocietyMember> memberLookup, ComponentLookup<EM_Component_SocietyRoot> rootLookup,
            out Entity resolvedTarget)
        {
            resolvedTarget = Entity.Null;

            if (targetMode == EmergenceEffectTarget.EventTarget)
            {
                resolvedTarget = subject;
                return subject != Entity.Null;
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
        #endregion
    }
}
