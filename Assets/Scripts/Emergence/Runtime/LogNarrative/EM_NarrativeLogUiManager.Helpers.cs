using System.Globalization;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NarrativeLogUiManager
    {
        #region Update
        private void UpdateLog()
        {
            if (logContent == null || logLinePrefab == null)
                return;

            bool filterChanged = UpdateFilterSignature();
            NativeArray<Entity> entities = logQuery.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
            {
                entities.Dispose();
                return;
            }

            Entity logEntity = entities[0];
            entities.Dispose();

            DynamicBuffer<EM_BufferElement_NarrativeLogEntry> buffer =
                entityManager.GetBuffer<EM_BufferElement_NarrativeLogEntry>(logEntity);

            if (filterChanged)
            {
                lastSequence = 0;
                logLines.Clear();
            }

            bool appended = false;
            ulong maxSequence = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                EM_BufferElement_NarrativeLogEntry entry = buffer[i];
                ulong sequence = entry.Sequence;

                if (sequence > maxSequence)
                    maxSequence = sequence;

                if (lastSequence != 0 && sequence <= lastSequence)
                    continue;

                if (!ShouldIncludeEntry(entry))
                    continue;

                string line = FormatEntry(entry);

                if (string.IsNullOrEmpty(line))
                    continue;

                logLines.Add(line);
                appended = true;
            }

            if (lastSequence > 0 && maxSequence < lastSequence)
            {
                lastSequence = 0;
                logLines.Clear();

                for (int i = 0; i < buffer.Length; i++)
                {
                    EM_BufferElement_NarrativeLogEntry entry = buffer[i];
                    ulong sequence = entry.Sequence;

                    if (sequence > maxSequence)
                        maxSequence = sequence;

                    if (!ShouldIncludeEntry(entry))
                        continue;

                    string line = FormatEntry(entry);

                    if (string.IsNullOrEmpty(line))
                        continue;

                    logLines.Add(line);
                    appended = true;
                }
            }

            if (maxSequence > 0)
                lastSequence = maxSequence;

            bool trimmed = TrimLogLines();

            if (!appended && !trimmed && !filterChanged)
                return;

            RefreshLogView();
        }
        #endregion

        #region Filtering
        private bool UpdateFilterSignature()
        {
            int signature = GetFilterSignature();

            if (signature == lastFilterSignature)
                return false;

            lastFilterSignature = signature;
            return true;
        }

        private int GetFilterSignature()
        {
            if (settings == null)
                return 0;

            int hash = 17;
            hash = (hash * 31) + settings.GetInstanceID();
            hash = (hash * 31) + (int)settings.Verbosity;
            hash = (hash * 31) + (settings.IncludeDesignerEntries ? 1 : 0);
            return hash;
        }

        private bool ShouldIncludeEntry(EM_BufferElement_NarrativeLogEntry entry)
        {
            if (settings == null)
                return true;

            if (entry.Verbosity > settings.Verbosity)
                return false;

            if (entry.Visibility == EM_NarrativeVisibility.Designer && !settings.IncludeDesignerEntries)
                return false;

            return true;
        }
        #endregion

        #region Formatting
        private string FormatEntry(EM_BufferElement_NarrativeLogEntry entry)
        {
            string body = entry.Text.ToString();

            if (entry.Title.Length == 0)
                return ApplyLineColor(entry, body);

            string title = entry.Title.ToString();

            if (string.IsNullOrEmpty(body))
                return ApplyLineColor(entry, title);

            return ApplyLineColor(entry, title + " - " + body);
        }

        private string ApplyLineColor(EM_BufferElement_NarrativeLogEntry entry, string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            Entity colorEntity = ResolveLineColorEntity(entry);

            if (colorEntity == Entity.Null)
                return text;

            if (!entityManager.Exists(colorEntity))
                return text;

            if (!entityManager.HasComponent<EM_Component_LogColor>(colorEntity))
                return text;

            EM_Component_LogColor logColor = entityManager.GetComponentData<EM_Component_LogColor>(colorEntity);
            Color color = new Color(logColor.Value.x, logColor.Value.y, logColor.Value.z, logColor.Value.w);
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            return string.Format(CultureInfo.InvariantCulture, "<color=#{0}>{1}</color>", hex, text);
        }

        private static Entity ResolveLineColorEntity(EM_BufferElement_NarrativeLogEntry entry)
        {
            if (entry.Subject != Entity.Null)
                return entry.Subject;

            if (entry.Target != Entity.Null)
                return entry.Target;

            return Entity.Null;
        }
        #endregion

        #region Helpers
        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            logQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EM_Component_NarrativeLog>(),
                ComponentType.ReadOnly<EM_BufferElement_NarrativeLogEntry>());
            return true;
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
