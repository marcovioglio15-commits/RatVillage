using Unity.Entities;

namespace EmergentMechanics
{
    internal static partial class EM_RuleEvaluation
    {
        #region Social
        // Apply a reputation delta on the target.
        private static bool ApplyReputationDelta(Entity target, float delta,
            ref ComponentLookup<EM_Component_Reputation> reputationLookup, out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (!reputationLookup.HasComponent(target))
                return false;

            EM_Component_Reputation reputation = reputationLookup[target];
            before = reputation.Value;
            after = reputation.Value + delta;
            reputation.Value = after;
            reputationLookup[target] = reputation;
            return true;
        }

        // Apply a cohesion delta on the target.
        private static bool ApplyCohesionDelta(Entity target, float delta,
            ref ComponentLookup<EM_Component_Cohesion> cohesionLookup, out float before, out float after)
        {
            before = 0f;
            after = 0f;

            if (!cohesionLookup.HasComponent(target))
                return false;

            EM_Component_Cohesion cohesion = cohesionLookup[target];
            before = cohesion.Value;
            after = cohesion.Value + delta;
            cohesion.Value = after;
            cohesionLookup[target] = cohesion;
            return true;
        }
        #endregion
    }
}
