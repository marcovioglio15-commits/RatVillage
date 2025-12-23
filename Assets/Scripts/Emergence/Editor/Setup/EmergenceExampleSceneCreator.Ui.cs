using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Emergence
{
    /// <summary>
    /// Debug UI creation helpers for the example village setup.
    /// </summary>
    internal static partial class EmergenceExampleSceneCreator
    {
        #region Debug UI
        private static void EnsureDebugUi(Scene mainScene, EM_DebugMessageTemplates templates)
        {
            GameObject uiRoot = GetOrCreateRootObject(mainScene, DebugUiObjectName);
            Canvas canvas = GetOrAddComponent<Canvas>(uiRoot);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(uiRoot);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(uiRoot);

            TMP_Text timeLabel = GetOrCreateText(uiRoot.transform, DebugTimeLabelName);
            TMP_Text logLabel = GetOrCreateText(uiRoot.transform, DebugLogLabelName);
            ConfigureTimeLabel(timeLabel);
            ConfigureLogLabel(logLabel);

            EmergenceDebugUiManager manager = GetOrAddComponent<EmergenceDebugUiManager>(uiRoot);
            ConfigureDebugUiManager(manager, timeLabel, logLabel, templates);
        }

        private static TMP_Text GetOrCreateText(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                TMP_Text existing = child.GetComponent<TMP_Text>();

                if (existing != null)
                    return existing;

                return child.gameObject.AddComponent<TextMeshProUGUI>();
            }

            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.AddComponent<TextMeshProUGUI>();
        }

        private static void ConfigureTimeLabel(TMP_Text timeLabel)
        {
            RectTransform rect = timeLabel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(420f, 28f);

            timeLabel.alignment = TextAlignmentOptions.TopLeft;
            timeLabel.fontSize = 20f;
            timeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            timeLabel.raycastTarget = false;
            timeLabel.text = "Time of Day: 00:00";
        }

        private static void ConfigureLogLabel(TMP_Text logLabel)
        {
            RectTransform rect = logLabel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -52f);
            rect.sizeDelta = new Vector2(900f, 600f);

            logLabel.alignment = TextAlignmentOptions.TopLeft;
            logLabel.fontSize = 14f;
            logLabel.textWrappingMode = TextWrappingModes.Normal;
            logLabel.overflowMode = TextOverflowModes.Overflow;
            logLabel.raycastTarget = false;
            logLabel.text = string.Empty;
        }

        private static void ConfigureDebugUiManager(EmergenceDebugUiManager manager, TMP_Text timeLabel, TMP_Text logLabel,
            EM_DebugMessageTemplates templates)
        {
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("timeLabel").objectReferenceValue = timeLabel;
            serialized.FindProperty("logLabel").objectReferenceValue = logLabel;
            serialized.FindProperty("templates").objectReferenceValue = templates;
            serialized.FindProperty("refreshInterval").floatValue = 0.25f;
            serialized.FindProperty("maxLogLines").intValue = 40;
            serialized.FindProperty("maxLogCharacters").intValue = 4000;
            serialized.ApplyModifiedProperties();
        }
        #endregion
    }
}
