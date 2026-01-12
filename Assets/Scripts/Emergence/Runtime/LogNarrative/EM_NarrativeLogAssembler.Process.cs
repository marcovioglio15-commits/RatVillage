using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NarrativeLogAssembler
    {
        #region Processing
        private void ProcessSignals()
        {
            if (logQuery.IsEmptyIgnoreFilter)
                return;

            NativeArray<Entity> entities = logQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
            {
                entities.Dispose();
                return;
            }

            Entity logEntity = entities[0];
            entities.Dispose();

            DynamicBuffer<EM_BufferElement_NarrativeSignal> signalBuffer =
                entityManager.GetBuffer<EM_BufferElement_NarrativeSignal>(logEntity);
            DynamicBuffer<EM_BufferElement_NarrativeLogEntry> entryBuffer =
                entityManager.GetBuffer<EM_BufferElement_NarrativeLogEntry>(logEntity);
            EM_Component_NarrativeLog log = entityManager.GetComponentData<EM_Component_NarrativeLog>(logEntity);

            ApplySettingsToLog(ref log);

            EM_NarrativeLogTemplates templateAsset = templates;

            if (templateAsset == null || templateAsset.Templates == null)
            {
                entityManager.SetComponentData(logEntity, log);
                return;
            }

            EM_NarrativeTemplate[] templateList = templateAsset.Templates;
            EM_NarrativeThresholds thresholds = EM_NarrativeThresholds.FromSettings(settings);
            ulong maxSequence = 0;

            for (int i = 0; i < signalBuffer.Length; i++)
            {
                EM_BufferElement_NarrativeSignal signal = signalBuffer[i];
                ulong sequence = signal.Sequence;

                if (sequence > maxSequence)
                    maxSequence = sequence;

                if (lastSignalSequence != 0 && sequence <= lastSignalSequence)
                    continue;

                if (!TryAppendEntryFromSignal(signal, templateList, thresholds, ref log, entryBuffer))
                    continue;
            }

            if (maxSequence > 0)
                lastSignalSequence = maxSequence;

            entityManager.SetComponentData(logEntity, log);
        }

        private void ApplySettingsToLog(ref EM_Component_NarrativeLog log)
        {
            if (settings == null)
                return;

            int maxSignals = settings.MaxSignalEntries;
            int maxEntries = settings.MaxLogEntries;

            if (maxSignals > 0 && log.MaxSignalEntries != maxSignals)
                log.MaxSignalEntries = maxSignals;

            if (maxEntries > 0 && log.MaxLogEntries != maxEntries)
                log.MaxLogEntries = maxEntries;
        }
        #endregion
    }
}
