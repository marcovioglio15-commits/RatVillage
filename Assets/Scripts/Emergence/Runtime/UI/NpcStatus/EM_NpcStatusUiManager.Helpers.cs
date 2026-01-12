using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_NpcStatusUiManager
    {
        #region Refresh
        private void RefreshEntries()
        {
            if (statusTextPrefab == null)
                return;

            EnsureCamera();

            NativeArray<Entity> entities = npcQuery.ToEntityArray(Allocator.Temp);
            activeEntities.Clear();

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                activeEntities.Add(entity);

                NpcStatusEntry entry;

                if (!entries.TryGetValue(entity, out entry))
                    entry = CreateEntry(entity);

                UpdateEntryText(entry);
            }

            entities.Dispose();

            removalBuffer.Clear();

            foreach (KeyValuePair<Entity, NpcStatusEntry> pair in entries)
            {
                if (activeEntities.Contains(pair.Key))
                    continue;

                removalBuffer.Add(pair.Key);
            }

            for (int i = 0; i < removalBuffer.Count; i++)
            {
                RemoveEntry(removalBuffer[i]);
            }
        }
        #endregion

        #region Transforms
        private void UpdateEntryTransforms()
        {
            EnsureCamera();
            removalBuffer.Clear();

            foreach (KeyValuePair<Entity, NpcStatusEntry> pair in entries)
            {
                Entity entity = pair.Key;
                NpcStatusEntry entry = pair.Value;

                if (!entityManager.Exists(entity))
                {
                    removalBuffer.Add(entity);
                    continue;
                }

                if (!entityManager.HasComponent<LocalTransform>(entity))
                {
                    removalBuffer.Add(entity);
                    continue;
                }

                LocalTransform transformData = entityManager.GetComponentData<LocalTransform>(entity);
                float3 position = transformData.Position;
                Vector3 worldPosition = new Vector3(position.x, position.y, position.z) + worldOffset;
                entry.Transform.position = worldPosition;

                if (faceCamera && cachedCamera != null)
                    entry.Transform.rotation = cachedCamera.transform.rotation;
            }

            for (int i = 0; i < removalBuffer.Count; i++)
            {
                RemoveEntry(removalBuffer[i]);
            }
        }
        #endregion

        #region Formatting
        private void UpdateEntryText(NpcStatusEntry entry)
        {
            if (entry == null || entry.Text == null)
                return;

            StringBuilder builder = new StringBuilder(128);
            AppendNeedsLine(entry.Entity, builder);
            builder.AppendLine();
            AppendResourcesLine(entry.Entity, builder);
            builder.AppendLine();
            AppendHealthLine(entry.Entity, builder);

            string text = builder.ToString();

            if (entry.Text.text == text)
                return;

            entry.Text.text = text;
        }

        private void AppendNeedsLine(Entity entity, StringBuilder builder)
        {
            builder.Append("Needs: ");

            if (!entityManager.HasBuffer<EM_BufferElement_Need>(entity))
            {
                builder.Append("None");
                return;
            }

            DynamicBuffer<EM_BufferElement_Need> needs = entityManager.GetBuffer<EM_BufferElement_Need>(entity);

            if (needs.Length == 0)
            {
                builder.Append("None");
                return;
            }

            for (int i = 0; i < needs.Length; i++)
            {
                EM_BufferElement_Need entry = needs[i];

                if (i > 0)
                    builder.Append(", ");

                builder.Append(EM_NarrativeLogFormatter.FormatId(entry.NeedId));
                builder.Append(' ');
                builder.Append(FormatValue(entry.Value));
            }
        }

        private void AppendResourcesLine(Entity entity, StringBuilder builder)
        {
            builder.Append("Resources: ");

            if (!entityManager.HasBuffer<EM_BufferElement_Resource>(entity))
            {
                builder.Append("None");
                return;
            }

            DynamicBuffer<EM_BufferElement_Resource> resources = entityManager.GetBuffer<EM_BufferElement_Resource>(entity);

            if (resources.Length == 0)
            {
                builder.Append("None");
                return;
            }

            for (int i = 0; i < resources.Length; i++)
            {
                EM_BufferElement_Resource entry = resources[i];

                if (i > 0)
                    builder.Append(", ");

                builder.Append(EM_NarrativeLogFormatter.FormatId(entry.ResourceId));
                builder.Append(' ');
                builder.Append(FormatValue(entry.Amount));
            }
        }

        private void AppendHealthLine(Entity entity, StringBuilder builder)
        {
            builder.Append("Health: ");

            if (!entityManager.HasComponent<EM_Component_NpcHealth>(entity))
            {
                builder.Append("None");
                return;
            }

            EM_Component_NpcHealth health = entityManager.GetComponentData<EM_Component_NpcHealth>(entity);
            builder.Append(FormatValue(health.Current));

            if (health.Max <= 0f)
                return;

            builder.Append('/');
            builder.Append(FormatValue(health.Max));
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion

        #region Entries
        private NpcStatusEntry CreateEntry(Entity entity)
        {
            TMP_Text instance = Instantiate(statusTextPrefab, ResolveRoot());
            instance.gameObject.SetActive(true);

            NpcStatusEntry entry = new NpcStatusEntry
            {
                Entity = entity,
                Text = instance,
                Transform = instance.transform
            };

            entries[entity] = entry;
            return entry;
        }

        private void RemoveEntry(Entity entity)
        {
            NpcStatusEntry entry;

            if (!entries.TryGetValue(entity, out entry))
                return;

            if (entry != null && entry.Text != null)
                Destroy(entry.Text.gameObject);

            entries.Remove(entity);
        }

        private void ClearEntries()
        {
            foreach (KeyValuePair<Entity, NpcStatusEntry> pair in entries)
            {
                NpcStatusEntry entry = pair.Value;

                if (entry != null && entry.Text != null)
                    Destroy(entry.Text.gameObject);
            }

            entries.Clear();
            activeEntities.Clear();
            removalBuffer.Clear();
        }
        #endregion

        #region World
        private bool EnsureWorld()
        {
            if (cachedWorld != null && cachedWorld.IsCreated)
                return true;

            cachedWorld = World.DefaultGameObjectInjectionWorld;

            if (cachedWorld == null || !cachedWorld.IsCreated)
                return false;

            entityManager = cachedWorld.EntityManager;
            npcQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EM_Component_NpcType>(),
                ComponentType.ReadOnly<LocalTransform>(), ComponentType.Exclude<Prefab>());
            return true;
        }

        private void EnsureCamera()
        {
            if (!faceCamera)
                return;

            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
                return;

            cachedCamera = Camera.main;
        }

        private Transform ResolveRoot()
        {
            if (statusRoot != null)
                return statusRoot;

            return transform;
        }
        #endregion
    }
}
