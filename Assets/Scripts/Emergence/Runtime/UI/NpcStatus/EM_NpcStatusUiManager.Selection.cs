using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcStatusUiManager
    {
        #region Selection
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

        private void HandleSelectionInput()
        {
            if (statusTextPrefab == null)
                return;

            if (!IsLeftClickTriggered())
                return;

            if (IsPointerOverUiLayer())
                return;

            EnsureCamera();

            if (cachedCamera == null)
                return;

            Entity entity;

            if (!TryPickNpcEntity(out entity))
                return;

            bool shouldShow = !visibleEntities.Contains(entity);
            ToggleEntryVisibility(entity, shouldShow);
        }
        #endregion

        #region Input
        private static bool IsLeftClickTriggered()
        {
            if (Mouse.current == null)
                return Input.GetMouseButtonDown(0);

            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        private bool IsPointerOverUiLayer()
        {
            if (EventSystem.current == null)
                return false;

            if (uiBlockLayers.value == 0)
                return false;

            Vector2 position = Vector2.zero;

            if (Mouse.current != null)
                position = Mouse.current.position.ReadValue();
            else
            {
                Vector3 legacyPosition = Input.mousePosition;
                position = new Vector2(legacyPosition.x, legacyPosition.y);
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = position
            };
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                GameObject hitObject = uiRaycastResults[i].gameObject;

                if (hitObject == null)
                    continue;

                int layerMask = 1 << hitObject.layer;

                if ((uiBlockLayers.value & layerMask) != 0)
                    return true;
            }

            return false;
        }
        #endregion

        #region Visibility
        private void ToggleEntryVisibility(Entity entity, bool isVisible)
        {
            if (!isVisible)
            {
                visibleEntities.Remove(entity);
                RemoveEntry(entity);
                return;
            }

            if (!entityManager.Exists(entity))
                return;

            if (!visibleEntities.Add(entity))
                return;

            NpcStatusEntry entry;

            if (!entries.TryGetValue(entity, out entry))
                entry = CreateEntry(entity);

            UpdateEntryText(entry);
        }
        #endregion
    }
}
