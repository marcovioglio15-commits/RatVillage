using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Helper methods for rule evaluation and effect application.
    /// </summary>
    public partial struct EmergenceRuleEvaluateSystem
    {
        #region Helpers
        private void EnsureRuleGroupLookup(BlobAssetReference<EmergenceLibraryBlob> libraryBlob)
        {
            if (ruleGroupLookupReady)
                return;

            ref BlobArray<EmergenceRuleGroupBlob> ruleGroups = ref libraryBlob.Value.RuleGroups;
            int groupCount = ruleGroups.Length;

            if (groupCount <= 0)
            {
                ruleGroupLookup = new NativeParallelHashMap<FixedString64Bytes, int>(1, Allocator.Persistent);
                ruleGroupLookupReady = true;
                return;
            }

            ruleGroupLookup = new NativeParallelHashMap<FixedString64Bytes, int>(groupCount, Allocator.Persistent);

            for (int i = 0; i < groupCount; i++)
            {
                EmergenceRuleGroupBlob group = ruleGroups[i];
                ruleGroupLookup.TryAdd(group.SignalId, i);
            }

            ruleGroupLookupReady = true;
        }

        private static float GetInterval(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        private static bool IsTierReady(EmergenceLodTier tier, bool fullReady, bool simplifiedReady, bool aggregatedReady)
        {
            if (tier == EmergenceLodTier.Full)
                return fullReady;

            if (tier == EmergenceLodTier.Simplified)
                return simplifiedReady;

            return aggregatedReady;
        }

        private static bool TryGetProfileReference(Entity target, ComponentLookup<EmergenceSocietyProfileReference> profileLookup,
            ComponentLookup<EmergenceSocietyMember> memberLookup, out BlobAssetReference<EmergenceSocietyProfileBlob> profileReference)
        {
            profileReference = default;

            if (target == Entity.Null)
                return false;

            if (profileLookup.HasComponent(target))
            {
                profileReference = profileLookup[target].Value;
                return true;
            }

            if (memberLookup.HasComponent(target))
            {
                Entity societyRoot = memberLookup[target].SocietyRoot;

                if (societyRoot == Entity.Null)
                    return false;

                if (!profileLookup.HasComponent(societyRoot))
                    return false;

                profileReference = profileLookup[societyRoot].Value;
                return true;
            }

            return false;
        }

        private static bool TryResolveEffectTarget(Entity eventTarget, EmergenceEffectTarget targetMode, ComponentLookup<EmergenceSocietyMember> memberLookup,
            ComponentLookup<EmergenceSocietyRoot> rootLookup, out Entity resolvedTarget)
        {
            resolvedTarget = Entity.Null;

            if (eventTarget == Entity.Null)
                return false;

            if (targetMode == EmergenceEffectTarget.EventTarget)
            {
                resolvedTarget = eventTarget;
                return true;
            }

            if (rootLookup.HasComponent(eventTarget))
            {
                resolvedTarget = eventTarget;
                return true;
            }

            if (!memberLookup.HasComponent(eventTarget))
                return false;

            Entity societyRoot = memberLookup[eventTarget].SocietyRoot;

            if (societyRoot == Entity.Null)
                return false;

            resolvedTarget = societyRoot;
            return true;
        }

        private static void ApplyEffect(EmergenceEffectBlob effect, Entity target, float magnitude,
            ref BufferLookup<EmergenceNeed> needLookup, ref BufferLookup<EmergenceResource> resourceLookup,
            ref ComponentLookup<EmergenceReputation> reputationLookup, ref ComponentLookup<EmergenceCohesion> cohesionLookup)
        {
            if (effect.EffectType == EmergenceEffectType.ModifyNeed)
            {
                ApplyNeedDelta(target, effect.ParameterId, magnitude, ref needLookup);
                return;
            }

            if (effect.EffectType == EmergenceEffectType.ModifyResource)
            {
                ApplyResourceDelta(target, effect.ParameterId, magnitude, ref resourceLookup);
                return;
            }

            if (effect.EffectType == EmergenceEffectType.ModifyReputation)
            {
                ApplyReputationDelta(target, magnitude, ref reputationLookup);
                return;
            }

            if (effect.EffectType == EmergenceEffectType.ModifyCohesion)
                ApplyCohesionDelta(target, magnitude, ref cohesionLookup);
        }

        private static void ApplyNeedDelta(Entity target, FixedString64Bytes needId, float delta, ref BufferLookup<EmergenceNeed> needLookup)
        {
            if (!needLookup.HasBuffer(target))
                return;

            if (needId.Length == 0)
                return;

            DynamicBuffer<EmergenceNeed> needs = needLookup[target];

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EmergenceNeed entry = needs[i];
                entry.Value += delta;
                needs[i] = entry;
                return;
            }

            EmergenceNeed newEntry = new EmergenceNeed
            {
                NeedId = needId,
                Value = delta
            };

            needs.Add(newEntry);
        }

        private static void ApplyResourceDelta(Entity target, FixedString64Bytes resourceId, float delta, ref BufferLookup<EmergenceResource> resourceLookup)
        {
            if (!resourceLookup.HasBuffer(target))
                return;

            if (resourceId.Length == 0)
                return;

            DynamicBuffer<EmergenceResource> resources = resourceLookup[target];

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EmergenceResource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            EmergenceResource newEntry = new EmergenceResource
            {
                ResourceId = resourceId,
                Amount = delta
            };

            resources.Add(newEntry);
        }

        private static void ApplyReputationDelta(Entity target, float delta, ref ComponentLookup<EmergenceReputation> reputationLookup)
        {
            if (!reputationLookup.HasComponent(target))
                return;

            EmergenceReputation reputation = reputationLookup[target];
            reputation.Value += delta;
            reputationLookup[target] = reputation;
        }

        private static void ApplyCohesionDelta(Entity target, float delta, ref ComponentLookup<EmergenceCohesion> cohesionLookup)
        {
            if (!cohesionLookup.HasComponent(target))
                return;

            EmergenceCohesion cohesion = cohesionLookup[target];
            cohesion.Value += delta;
            cohesionLookup[target] = cohesion;
        }
        #endregion
    }
}
