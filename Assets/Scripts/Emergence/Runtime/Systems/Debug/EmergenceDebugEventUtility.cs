using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// Utility helpers for managing Emergence debug event buffers.
    /// </summary>
    internal static class EmergenceDebugEventUtility
    {
        #region Public
        /// <summary>
        /// Appends a debug event while enforcing the configured buffer limit.
        /// </summary>
        public static void AppendEvent(DynamicBuffer<EmergenceDebugEvent> buffer, int maxEntries, EmergenceDebugEvent entry)
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

            buffer.Add(entry);
        }
        #endregion
    }
}
