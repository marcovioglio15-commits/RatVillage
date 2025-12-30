using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public sealed partial class EM_LogUiManager
    {
        #region Helpers
        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            debugQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EM_Component_Log>(), ComponentType.ReadOnly<EM_Component_Event>());
            clockQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EM_Component_SocietyClock>(), ComponentType.ReadOnly<EM_Component_SocietyRoot>());
            hasQueries = true;

            return true;
        }

        private float GetSocietyTime()
        {
            if (!hasQueries)
                return 0f;

            NativeArray<EM_Component_SocietyClock> clocks = clockQuery.ToComponentDataArray<EM_Component_SocietyClock>(Allocator.Temp);

            if (clocks.Length == 0)
            {
                clocks.Dispose();
                return 0f;
            }

            float timeOfDay = clocks[0].TimeOfDay;
            clocks.Dispose();
            return timeOfDay;
        }

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
