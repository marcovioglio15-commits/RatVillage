using Unity.Entities;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct EM_System_NarrativeSignalCollector : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_NeedSignalSettings> needSignalSettingsLookup;
        private BufferLookup<EM_BufferElement_NeedSignalOverride> needSignalOverrideLookup;
        private ComponentLookup<EM_Component_HealthSignalSettings> healthSignalSettingsLookup;
        private ulong lastDebugSequence;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            needSignalSettingsLookup = state.GetComponentLookup<EM_Component_NeedSignalSettings>(true);
            needSignalOverrideLookup = state.GetBufferLookup<EM_BufferElement_NeedSignalOverride>(true);
            healthSignalSettingsLookup = state.GetComponentLookup<EM_Component_HealthSignalSettings>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            bool hasNarrativeBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_BufferElement_NarrativeSignal> narrativeBuffer);

            if (!hasDebugBuffer || !hasNarrativeBuffer)
                return;

            RefRW<EM_Component_NarrativeLog> narrativeLogRef = SystemAPI.GetSingletonRW<EM_Component_NarrativeLog>();
            EM_Component_NarrativeLog narrativeLog = narrativeLogRef.ValueRO;
            int maxSignals = narrativeLog.MaxSignalEntries;

            needSignalSettingsLookup.Update(ref state);
            needSignalOverrideLookup.Update(ref state);
            healthSignalSettingsLookup.Update(ref state);

            ulong maxSequence = 0;

            for (int i = 0; i < debugBuffer.Length; i++)
            {
                EM_Component_Event debugEvent = debugBuffer[i];
                ulong sequence = debugEvent.Sequence;

                if (sequence > maxSequence)
                    maxSequence = sequence;

                if (lastDebugSequence != 0 && sequence <= lastDebugSequence)
                    continue;

                EM_BufferElement_NarrativeSignal signal;

                if (!TryBuildNarrativeSignal(debugEvent, out signal))
                    continue;

                EM_Utility_NarrativeLogEvent.AppendSignal(narrativeBuffer, maxSignals, ref narrativeLog, signal);
            }

            if (maxSequence > 0)
                lastDebugSequence = maxSequence;

            narrativeLogRef.ValueRW = narrativeLog;
        }
        #endregion
    }
}
