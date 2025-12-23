using Unity.Collections;
using Unity.Entities;

namespace Emergence
{
    /// <summary>
    /// World access and buffer maintenance for the Emergence debug HUD.
    /// </summary>
    public sealed partial class EmergenceDebugUiManager
    {
        #region Helpers
        /// <summary>
        /// Ensures ECS queries are created and ready.
        /// </summary>
        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            debugQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EmergenceDebugLog>(), ComponentType.ReadOnly<EmergenceDebugEvent>());
            clockQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EmergenceSocietyClock>(), ComponentType.ReadOnly<EmergenceSocietyRoot>());
            hasQueries = true;

            return true;
        }

        /// <summary>
        /// Returns the time-of-day from the first available society clock.
        /// </summary>
        private float GetSocietyTime()
        {
            if (!hasQueries)
                return 0f;

            NativeArray<EmergenceSocietyClock> clocks = clockQuery.ToComponentDataArray<EmergenceSocietyClock>(Allocator.Temp);

            if (clocks.Length == 0)
            {
                clocks.Dispose();
                return 0f;
            }

            float timeOfDay = clocks[0].TimeOfDay;
            clocks.Dispose();
            return timeOfDay;
        }

        /// <summary>
        /// Trims stored log lines based on the configured limits.
        /// </summary>
        private bool TrimLogLines()
        {
            bool trimmed = false;
            int lineLimit = maxLogLines;

            if (lineLimit <= 0)
                lineLimit = DefaultMaxLogLines;

            while (logLines.Count > lineLimit)
            {
                logLines.RemoveAt(0);
                trimmed = true;
            }

            int characterLimit = maxLogCharacters;

            if (characterLimit <= 0)
                return trimmed;

            int totalCharacters = 0;

            for (int i = 0; i < logLines.Count; i++)
            {
                totalCharacters += logLines[i].Length + 1;
            }

            while (totalCharacters > characterLimit && logLines.Count > 0)
            {
                totalCharacters -= logLines[0].Length + 1;
                logLines.RemoveAt(0);
                trimmed = true;
            }

            return trimmed;
        }
        #endregion
    }
}
