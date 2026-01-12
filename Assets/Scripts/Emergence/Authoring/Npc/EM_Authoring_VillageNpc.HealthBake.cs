using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EmergentMechanics
{
    public sealed partial class EM_Authoring_VillageNpc
    {
        #region Health Bake
        private static void AddHealthComponents(EM_Authoring_VillageNpc authoring, Entity entity, Baker<EM_Authoring_VillageNpc> baker)
        {
            float maxHealth = math.max(0f, authoring.maxHealth);
            float currentHealth = math.clamp(authoring.initialHealth, 0f, maxHealth);

            baker.AddComponent(entity, new EM_Component_NpcHealth
            {
                Current = currentHealth,
                Max = maxHealth
            });
            baker.AddComponent(entity, new EM_Component_NpcHealthTickState
            {
                NextTick = 0d
            });

            DynamicBuffer<EM_BufferElement_NeedDamageSetting> buffer = baker.AddBuffer<EM_BufferElement_NeedDamageSetting>(entity);
            AddNeedDamageSettings(authoring.needs, ref buffer);
        }

        private static void AddNeedDamageSettings(NeedProfileEntry[] source, ref DynamicBuffer<EM_BufferElement_NeedDamageSetting> buffer)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (!EM_IdUtility.HasId(source[i].NeedIdDefinition, source[i].NeedId))
                    continue;

                float threshold = math.clamp(source[i].DamageThreshold, 0f, 1f);
                float damagePerHour = math.max(0f, source[i].DamagePerHour);

                if (damagePerHour <= 0f)
                    continue;

                buffer.Add(new EM_BufferElement_NeedDamageSetting
                {
                    NeedId = EM_IdUtility.ToFixed(source[i].NeedIdDefinition, source[i].NeedId),
                    UrgencyThreshold = threshold,
                    DamagePerHour = damagePerHour
                });
            }
        }
        #endregion
    }
}
