using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region Utilities
        // Sample the affinity multiplier curve for the provider NPC.
        private static float SampleAffinityMultiplier(Entity provider, float affinity01,
            ref ComponentLookup<EM_Component_NpcTradePreferences> tradePreferencesLookup)
        {
            if (!tradePreferencesLookup.HasComponent(provider))
                return 1f;

            EM_Component_NpcTradePreferences preferences = tradePreferencesLookup[provider];
            int count = preferences.AffinityMultiplierSamples.Length;

            if (count <= 0)
                return 1f;

            if (count == 1)
                return preferences.AffinityMultiplierSamples[0];

            float scaled = math.saturate(affinity01) * (count - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, count - 1);
            float t = scaled - index;
            float value = math.lerp(preferences.AffinityMultiplierSamples[index], preferences.AffinityMultiplierSamples[nextIndex], t);

            return math.clamp(value, preferences.MinMultiplier, preferences.MaxMultiplier);
        }

        // Emit a trade signal and mirror it into the debug buffer.
        private static void EmitTradeSignal(DynamicBuffer<EM_BufferElement_SignalEvent> signals, FixedString64Bytes signalId,
            FixedString64Bytes contextId, Entity subject, Entity target, Entity societyRoot, float value,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries)
        {
            if (signalId.Length == 0)
                return;

            EM_BufferElement_SignalEvent signalEvent = new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = value,
                Subject = subject,
                Target = target,
                SocietyRoot = societyRoot,
                ContextId = contextId,
                Time = 0d
            };

            signals.Add(signalEvent);

            if (!hasDebugBuffer)
                return;

            EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildSignalEvent(signalId, value, contextId, subject, target, societyRoot);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, debugEvent);
        }

        // Get the current amount for a resource id.
        private static float GetResourceAmount(DynamicBuffer<EM_BufferElement_Resource> resources, FixedString64Bytes resourceId)
        {
            if (resourceId.Length == 0)
                return 0f;

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                return resources[i].Amount;
            }

            return 0f;
        }

        // Apply a delta to the resource buffer.
        private static void ApplyResourceDelta(DynamicBuffer<EM_BufferElement_Resource> resources, FixedString64Bytes resourceId, float delta)
        {
            if (resourceId.Length == 0)
                return;

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EM_BufferElement_Resource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            resources.Add(new EM_BufferElement_Resource
            {
                ResourceId = resourceId,
                Amount = delta
            });
        }

        // Apply a delta to the need buffer with clamping.
        private static void ApplyNeedDelta(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId, float delta,
            float minValue, float maxValue)
        {
            if (needId.Length == 0)
                return;

            float minClamp = math.min(minValue, maxValue);
            float maxClamp = math.max(minValue, maxValue);

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EM_BufferElement_Need entry = needs[i];
                entry.Value = math.clamp(entry.Value + delta, minClamp, maxClamp);
                needs[i] = entry;
                return;
            }

            needs.Add(new EM_BufferElement_Need
            {
                NeedId = needId,
                Value = math.clamp(delta, minClamp, maxClamp)
            });
        }

        // Advance the deterministic random sequence and return a 0-1 value.
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
