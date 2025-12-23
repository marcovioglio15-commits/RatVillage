using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Evaluates rules against queued signals and applies effects.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EmergenceSignalCollectSystem))]
    public partial struct EmergenceRuleEvaluateSystem : ISystem
    {
        #region State
        private NativeParallelHashMap<FixedString64Bytes, int> ruleGroupLookup;
        private bool ruleGroupLookupReady;
        private ComponentLookup<EmergenceSocietyMember> memberLookup;
        private ComponentLookup<EmergenceSocietyRoot> rootLookup;
        private ComponentLookup<EmergenceSocietyProfileReference> profileLookup;
        private ComponentLookup<EmergenceReputation> reputationLookup;
        private ComponentLookup<EmergenceCohesion> cohesionLookup;
        private BufferLookup<EmergenceNeed> needLookup;
        private BufferLookup<EmergenceResource> resourceLookup;
        #endregion

        #region Unity
        /// <summary>
        /// Initializes required queries for the system.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EmergenceLibraryReference>();
            state.RequireForUpdate<EmergenceGlobalSettings>();
            memberLookup = state.GetComponentLookup<EmergenceSocietyMember>(true);
            rootLookup = state.GetComponentLookup<EmergenceSocietyRoot>(true);
            profileLookup = state.GetComponentLookup<EmergenceSocietyProfileReference>(true);
            reputationLookup = state.GetComponentLookup<EmergenceReputation>(false);
            cohesionLookup = state.GetComponentLookup<EmergenceCohesion>(false);
            needLookup = state.GetBufferLookup<EmergenceNeed>(false);
            resourceLookup = state.GetBufferLookup<EmergenceResource>(false);
        }

        /// <summary>
        /// Disposes allocated native containers.
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            if (!ruleGroupLookup.IsCreated)
                return;

            ruleGroupLookup.Dispose();
        }

        /// <summary>
        /// Evaluates queued signals and applies rule effects.
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            EmergenceLibraryReference libraryReference = SystemAPI.GetSingleton<EmergenceLibraryReference>();

            if (!libraryReference.Value.IsCreated)
                return;

            EnsureRuleGroupLookup(libraryReference.Value);

            Entity libraryEntity = SystemAPI.GetSingletonEntity<EmergenceLibraryReference>();
            DynamicBuffer<EmergenceSignalEvent> queue = state.EntityManager.GetBuffer<EmergenceSignalEvent>(libraryEntity);

            if (queue.Length == 0)
                return;

            EmergenceGlobalSettings settings = SystemAPI.GetSingleton<EmergenceGlobalSettings>();
            EmergenceTierTickState tickState = SystemAPI.GetSingleton<EmergenceTierTickState>();
            double time = SystemAPI.Time.ElapsedTime;

            bool fullReady = time >= tickState.NextFullTick;
            bool simplifiedReady = time >= tickState.NextSimplifiedTick;
            bool aggregatedReady = time >= tickState.NextAggregatedTick;

            if (!fullReady && !simplifiedReady && !aggregatedReady)
                return;

            tickState.NextFullTick = fullReady ? time + GetInterval(settings.FullSimTickRate) : tickState.NextFullTick;
            tickState.NextSimplifiedTick = simplifiedReady ? time + GetInterval(settings.SimplifiedSimTickRate) : tickState.NextSimplifiedTick;
            tickState.NextAggregatedTick = aggregatedReady ? time + GetInterval(settings.AggregatedSimTickRate) : tickState.NextAggregatedTick;
            SystemAPI.SetSingleton(tickState);

            bool allReady = fullReady && simplifiedReady && aggregatedReady;
            NativeList<EmergenceSignalEvent> deferred = default;

            if (!allReady)
                deferred = new NativeList<EmergenceSignalEvent>(Allocator.Temp);

            memberLookup.Update(ref state);
            rootLookup.Update(ref state);
            profileLookup.Update(ref state);
            reputationLookup.Update(ref state);
            cohesionLookup.Update(ref state);
            needLookup.Update(ref state);
            resourceLookup.Update(ref state);

            ref EmergenceLibraryBlob libraryBlob = ref libraryReference.Value.Value;
            ref BlobArray<EmergenceRuleGroupBlob> ruleGroups = ref libraryBlob.RuleGroups;
            ref BlobArray<EmergenceRuleBlob> rules = ref libraryBlob.Rules;
            ref BlobArray<EmergenceSignalBlob> signals = ref libraryBlob.Signals;
            ref BlobArray<EmergenceEffectBlob> effects = ref libraryBlob.Effects;
            ref BlobArray<EmergenceRuleSetBlob> ruleSets = ref libraryBlob.RuleSets;

            for (int i = 0; i < queue.Length; i++)
            {
                EmergenceSignalEvent signalEvent = queue[i];

                if (!IsTierReady(signalEvent.LodTier, fullReady, simplifiedReady, aggregatedReady))
                {
                    if (deferred.IsCreated)
                        deferred.Add(signalEvent);

                    continue;
                }

                int groupIndex;
                bool foundGroup = ruleGroupLookup.TryGetValue(signalEvent.SignalId, out groupIndex);

                if (!foundGroup)
                    continue;

                EmergenceRuleGroupBlob group = ruleGroups[groupIndex];

                if (group.Length <= 0)
                    continue;

                BlobAssetReference<EmergenceSocietyProfileBlob> profileReference;
                bool hasProfile = TryGetProfileReference(signalEvent.Target, profileLookup, memberLookup, out profileReference);

                for (int r = group.StartIndex; r < group.StartIndex + group.Length; r++)
                {
                    EmergenceRuleBlob rule = rules[r];

                    if (rule.RuleSetIndex < 0 || rule.RuleSetIndex >= ruleSets.Length)
                        continue;

                    EmergenceRuleSetBlob ruleSet = ruleSets[rule.RuleSetIndex];

                    if (ruleSet.IsEnabled == 0)
                        continue;

                    if (rule.SignalIndex < 0 || rule.SignalIndex >= signals.Length)
                        continue;

                    EmergenceSignalBlob signal = signals[rule.SignalIndex];

                    if ((int)signalEvent.LodTier > (int)signal.MinimumLod)
                        continue;

                    if (signalEvent.Value < rule.MinimumSignalValue)
                        continue;

                    if (hasProfile && profileReference.IsCreated)
                    {
                        ref BlobArray<byte> mask = ref profileReference.Value.RuleSetMask;

                        if (rule.RuleSetIndex < mask.Length && mask[rule.RuleSetIndex] == 0)
                            continue;
                    }

                    if (rule.EffectIndex < 0 || rule.EffectIndex >= effects.Length)
                        continue;

                    EmergenceEffectBlob effect = effects[rule.EffectIndex];

                    Entity effectTarget;
                    bool targetResolved = TryResolveEffectTarget(signalEvent.Target, effect.Target, memberLookup, rootLookup, out effectTarget);

                    if (!targetResolved)
                        continue;

                    float magnitude = effect.Magnitude * rule.Weight * signal.DefaultWeight * signalEvent.Value;

                    if (effect.UseClamp != 0)
                        magnitude = math.clamp(magnitude, effect.MinValue, effect.MaxValue);

                    ApplyEffect(effect, effectTarget, magnitude, ref needLookup, ref resourceLookup, ref reputationLookup, ref cohesionLookup);
                }
            }

            queue.Clear();

            if (deferred.IsCreated)
            {
                queue.AddRange(deferred.AsArray());
                deferred.Dispose();
            }
        }
        #endregion
    }
}
