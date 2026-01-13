using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Health
        private static bool ApplyHealthDelta(Entity target, float magnitude, float normalized,
            ref ComponentLookup<EM_Component_NpcHealth> healthLookup, out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (!healthLookup.HasComponent(target))
                return false;

            EM_Component_NpcHealth health = healthLookup[target];
            float maxHealth = math.max(health.Max, 0f);
            before = health.Current;
            float delta = normalized * magnitude * maxHealth;
            after = math.clamp(before + delta, 0f, maxHealth);
            health.Current = after;
            health.Max = maxHealth;
            healthLookup[target] = health;
            return true;
        }
        #endregion
    }
}
