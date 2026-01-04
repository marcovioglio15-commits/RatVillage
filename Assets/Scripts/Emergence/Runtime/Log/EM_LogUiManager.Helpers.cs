using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

        private void RefreshLogView()
        {
            EnsureLinePoolSize(logLines.Count);

            for (int i = 0; i < linePool.Count; i++)
            {
                bool isActive = i < logLines.Count;
                TMP_Text line = linePool[i];

                if (isActive)
                {
                    if (!line.gameObject.activeSelf)
                        line.gameObject.SetActive(true);

                    line.text = logLines[i];
                    continue;
                }

                if (line.gameObject.activeSelf)
                    line.gameObject.SetActive(false);
            }

            if (!autoScroll || logScrollRect == null)
                return;

            Canvas.ForceUpdateCanvases();
            if (logScrollRect.verticalScrollbar != null)
                logScrollRect.verticalNormalizedPosition = 0f;
        }

        private void EnsureLinePoolSize(int requiredCount)
        {
            if (logContent == null || logLinePrefab == null)
                return;

            while (linePool.Count < requiredCount)
            {
                TMP_Text line = Object.Instantiate(logLinePrefab, logContent);
                line.gameObject.SetActive(true);
                linePool.Add(line);
            }
        }
        #endregion
    }
}
