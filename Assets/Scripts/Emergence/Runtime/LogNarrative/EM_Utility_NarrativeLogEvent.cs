using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    internal static class EM_Utility_NarrativeLogEvent
    {
        #region Append
        public static void AppendSignal(DynamicBuffer<EM_BufferElement_NarrativeSignal> buffer, int maxEntries,
            ref EM_Component_NarrativeLog log, EM_BufferElement_NarrativeSignal signal)
        {
            if (maxEntries <= 0)
                return;

            int limit = maxEntries;

            if (limit < 1)
                return;

            if (buffer.Length >= limit)
            {
                int removeCount = buffer.Length - limit + 1;

                if (removeCount > 0)
                    buffer.RemoveRange(0, removeCount);
            }

            if (log.NextSignalSequence == 0)
                log.NextSignalSequence = 1;

            signal.Sequence = log.NextSignalSequence;
            log.NextSignalSequence++;
            buffer.Add(signal);
        }

        public static void AppendEntry(DynamicBuffer<EM_BufferElement_NarrativeLogEntry> buffer, int maxEntries,
            ref EM_Component_NarrativeLog log, EM_BufferElement_NarrativeLogEntry entry)
        {
            if (maxEntries <= 0)
                return;

            int limit = maxEntries;

            if (limit < 1)
                return;

            if (buffer.Length >= limit)
            {
                int removeCount = buffer.Length - limit + 1;

                if (removeCount > 0)
                    buffer.RemoveRange(0, removeCount);
            }

            if (log.NextLogSequence == 0)
                log.NextLogSequence = 1;

            entry.Sequence = log.NextLogSequence;
            log.NextLogSequence++;
            buffer.Add(entry);
        }
        #endregion

        #region Time
        public static double ConvertTimeSecondsToHours(double timeSeconds)
        {
            if (timeSeconds < 0d)
                return -1d;

            return timeSeconds / 3600d;
        }
        #endregion
    }
}
