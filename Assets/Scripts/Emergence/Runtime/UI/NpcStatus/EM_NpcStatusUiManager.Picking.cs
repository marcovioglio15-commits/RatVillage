using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcStatusUiManager
    {
        #region Picking
        private bool TryPickNpcEntity(out Entity entity)
        {
            entity = Entity.Null;

            if (cachedCamera == null)
                return false;

            if (!hasNpcQuery)
                return false;

            if (TryPickNpcFromGrid(out entity))
                return true;

            if (TryPickNpcFromScreenProximity(out entity))
                return true;

            if (TryPickNpcFromRay(out entity))
                return true;

            return false;
        }
        #endregion

        #region Grid Picking
        private bool TryPickNpcFromGrid(out Entity entity)
        {
            entity = Entity.Null;

            EM_Component_LocationGrid grid;
            DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy;

            if (!TryResolveGrid(out grid, out occupancy))
                return false;

            Vector2 screenPosition;

            if (!TryGetPointerScreenPosition(out screenPosition))
                return false;

            Ray ray = cachedCamera.ScreenPointToRay(screenPosition);
            Vector3 gridOrigin = new Vector3(grid.Origin.x, grid.Origin.y, grid.Origin.z);
            Plane plane = new Plane(Vector3.up, gridOrigin);
            float enter;

            if (!plane.Raycast(ray, out enter))
                return false;

            Vector3 hitPoint = ray.GetPoint(enter);
            int nodeIndex;

            if (!EM_Utility_LocationGrid.TryGetNodeIndex((float3)hitPoint, grid, out nodeIndex))
                return false;

            if (nodeIndex < 0 || nodeIndex >= occupancy.Length)
                return false;

            Entity occupant = occupancy[nodeIndex].Occupant;

            if (occupant == Entity.Null)
                return false;

            if (!entityManager.Exists(occupant))
                return false;

            if (!entityManager.HasComponent<EM_Component_NpcLocationState>(occupant))
                return false;

            entity = occupant;
            return true;
        }
        #endregion

        #region Screen Picking
        private bool TryPickNpcFromScreenProximity(out Entity entity)
        {
            entity = Entity.Null;

            if (!hasNpcQuery)
                return false;

            float maxDistance = selectionScreenRadiusPixels;

            if (maxDistance <= 0f)
                return false;

            Vector2 screenPosition;

            if (!TryGetPointerScreenPosition(out screenPosition))
                return false;

            NativeArray<Entity> candidates = npcQuery.ToEntityArray(Allocator.Temp);

            try
            {
                if (candidates.Length == 0)
                    return false;

                float bestDistanceSq = maxDistance * maxDistance;
                Entity bestEntity = Entity.Null;

                for (int i = 0; i < candidates.Length; i++)
                {
                    Entity candidate = candidates[i];
                    float3 worldPosition;

                    if (!TryGetWorldPosition(candidate, out worldPosition))
                        continue;

                    Vector3 candidateWorld = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z);
                    Vector3 candidateScreen = cachedCamera.WorldToScreenPoint(candidateWorld);

                    if (candidateScreen.z <= 0f)
                        continue;

                    float deltaX = candidateScreen.x - screenPosition.x;
                    float deltaY = candidateScreen.y - screenPosition.y;
                    float distanceSq = deltaX * deltaX + deltaY * deltaY;

                    if (distanceSq > bestDistanceSq)
                        continue;

                    bestDistanceSq = distanceSq;
                    bestEntity = candidate;
                }

                if (bestEntity == Entity.Null)
                    return false;

                entity = bestEntity;
                return true;
            }
            finally
            {
                candidates.Dispose();
            }
        }
        #endregion

        #region Ray Picking
        private bool TryPickNpcFromRay(out Entity entity)
        {
            entity = Entity.Null;

            if (!hasNpcQuery)
                return false;

            float maxDistance = selectionRadius;

            if (maxDistance <= 0f)
                return false;

            Vector2 screenPosition;

            if (!TryGetPointerScreenPosition(out screenPosition))
                return false;

            Ray ray = cachedCamera.ScreenPointToRay(screenPosition);
            float maxDistanceSq = maxDistance * maxDistance;
            NativeArray<Entity> candidates = npcQuery.ToEntityArray(Allocator.Temp);

            try
            {
                if (candidates.Length == 0)
                    return false;

                float bestDistanceSq = maxDistanceSq;
                Entity bestEntity = Entity.Null;

                for (int i = 0; i < candidates.Length; i++)
                {
                    Entity candidate = candidates[i];
                    float3 worldPosition;

                    if (!TryGetWorldPosition(candidate, out worldPosition))
                        continue;

                    Vector3 candidateWorld = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z);
                    Vector3 toCandidate = candidateWorld - ray.origin;
                    float projection = Vector3.Dot(toCandidate, ray.direction);

                    if (projection < 0f)
                        continue;

                    Vector3 closest = ray.origin + ray.direction * projection;
                    float distanceSq = (candidateWorld - closest).sqrMagnitude;

                    if (distanceSq > bestDistanceSq)
                        continue;

                    bestDistanceSq = distanceSq;
                    bestEntity = candidate;
                }

                if (bestEntity == Entity.Null)
                    return false;

                entity = bestEntity;
                return true;
            }
            finally
            {
                candidates.Dispose();
            }
        }
        #endregion

        #region Grid Resolve
        private bool TryResolveGrid(out EM_Component_LocationGrid grid, out DynamicBuffer<EM_BufferElement_LocationOccupancy> occupancy)
        {
            grid = default;
            occupancy = default;

            if (!hasGridQuery)
                return false;

            if (gridQuery.IsEmptyIgnoreFilter)
                return false;

            NativeArray<Entity> grids = gridQuery.ToEntityArray(Allocator.Temp);

            try
            {
                if (grids.Length == 0)
                    return false;

                Entity gridEntity = grids[0];

                if (!entityManager.Exists(gridEntity))
                    return false;

                if (!entityManager.HasComponent<EM_Component_LocationGrid>(gridEntity))
                    return false;

                if (!entityManager.HasBuffer<EM_BufferElement_LocationOccupancy>(gridEntity))
                    return false;

                grid = entityManager.GetComponentData<EM_Component_LocationGrid>(gridEntity);
                occupancy = entityManager.GetBuffer<EM_BufferElement_LocationOccupancy>(gridEntity);
                return true;
            }
            finally
            {
                grids.Dispose();
            }
        }
        #endregion

        #region Pointer
        private static bool TryGetPointerScreenPosition(out Vector2 position)
        {
            position = Vector2.zero;

            if (Mouse.current != null)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            if (!Input.mousePresent)
                return false;

            Vector3 legacyPosition = Input.mousePosition;
            position = new Vector2(legacyPosition.x, legacyPosition.y);
            return true;
        }
        #endregion
    }
}
