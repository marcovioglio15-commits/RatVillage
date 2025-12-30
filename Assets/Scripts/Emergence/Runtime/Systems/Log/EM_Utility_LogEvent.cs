using Unity.Entities;

namespace EmergentMechanics
{
    internal static class EM_Utility_LogEvent
    {
        #region Public Properties
        public static void AppendEvent(DynamicBuffer<EM_Component_Event> buffer, int maxEntries, EM_Component_Event entry)
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
