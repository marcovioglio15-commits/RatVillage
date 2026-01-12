using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_NarrativeSignalCollector
    {
        #region Signal Resolution
        private bool IsNeedUrgencySignal(EM_Component_Event debugEvent)
        {
            if (debugEvent.SignalId.Length == 0)
                return false;

            Entity societyRoot = debugEvent.Society;

            if (societyRoot == Entity.Null)
                return false;

            if (!needSignalSettingsLookup.HasComponent(societyRoot))
                return false;

            EM_Component_NeedSignalSettings settings = needSignalSettingsLookup[societyRoot];

            if (debugEvent.SignalId.Equals(settings.NeedUrgencySignalId))
                return true;

            if (debugEvent.ContextId.Length == 0)
                return false;

            if (!needSignalOverrideLookup.HasBuffer(societyRoot))
                return false;

            DynamicBuffer<EM_BufferElement_NeedSignalOverride> overrides = needSignalOverrideLookup[societyRoot];

            for (int i = 0; i < overrides.Length; i++)
            {
                if (!overrides[i].NeedId.Equals(debugEvent.ContextId))
                    continue;

                if (overrides[i].UrgencySignalId.Length == 0)
                    return false;

                return overrides[i].UrgencySignalId.Equals(debugEvent.SignalId);
            }

            return false;
        }

        private bool IsHealthValueSignal(EM_Component_Event debugEvent)
        {
            if (debugEvent.SignalId.Length == 0)
                return false;

            Entity societyRoot = debugEvent.Society;

            if (societyRoot == Entity.Null)
                return false;

            if (!healthSignalSettingsLookup.HasComponent(societyRoot))
                return false;

            EM_Component_HealthSignalSettings settings = healthSignalSettingsLookup[societyRoot];
            return debugEvent.SignalId.Equals(settings.HealthValueSignalId);
        }

        private bool IsHealthDamageSignal(EM_Component_Event debugEvent)
        {
            if (debugEvent.SignalId.Length == 0)
                return false;

            Entity societyRoot = debugEvent.Society;

            if (societyRoot == Entity.Null)
                return false;

            if (!healthSignalSettingsLookup.HasComponent(societyRoot))
                return false;

            EM_Component_HealthSignalSettings settings = healthSignalSettingsLookup[societyRoot];
            return debugEvent.SignalId.Equals(settings.HealthDamageSignalId);
        }

        private static bool IsOverrideActivity(FixedString64Bytes activityId)
        {
            if (activityId.Length == 0)
                return false;

            string value = activityId.ToString();
            return value.StartsWith("Override.", System.StringComparison.Ordinal);
        }
        #endregion
    }
}
