using System;
using Unity.Collections;

namespace EmergentMechanics
{
    public sealed partial class EM_LogUiManager
    {
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
            if (logSettings == null)
                return 0;

            int hash = 17;
            hash = (hash * 31) + logSettings.GetInstanceID();
            hash = (hash * 31) + (int)logSettings.FilterMode;
            hash = (hash * 31) + BoolToInt(logSettings.IncludeSignals);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeNeedSignals);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeTradeSignals);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeOtherSignals);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeIntents);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeEffects);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeInteractionAttempts);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeInteractionSuccess);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeInteractionFailures);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeScheduleStart);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeScheduleEnd);
            hash = (hash * 31) + BoolToInt(logSettings.IncludeScheduleTick);
            hash = (hash * 31) + SafeStringHash(logSettings.NeedSignalPrefix);
            hash = (hash * 31) + SafeStringHash(logSettings.TradeSignalPrefix);
            return hash;
        }

        private static int BoolToInt(bool value)
        {
            if (value)
                return 1;

            return 0;
        }

        private static int SafeStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            return value.GetHashCode();
        }

        private bool ShouldIncludeEvent(EM_Component_Event debugEvent)
        {
            if (logSettings == null)
                return true;

            EM_DebugLogFilterMode mode = logSettings.FilterMode;

            if (mode == EM_DebugLogFilterMode.All)
                return true;

            if (mode == EM_DebugLogFilterMode.SignalsOnly)
            {
                if (debugEvent.Type != EM_DebugEventType.SignalEmitted)
                    return false;

                return IsSignalAllowed(debugEvent.SignalId, logSettings);
            }

            if (mode == EM_DebugLogFilterMode.EventsOnly)
                return debugEvent.Type != EM_DebugEventType.SignalEmitted;

            if (mode == EM_DebugLogFilterMode.ScheduleStartEndOnly)
                return debugEvent.Type == EM_DebugEventType.ScheduleWindow || debugEvent.Type == EM_DebugEventType.ScheduleEnd;

            if (mode == EM_DebugLogFilterMode.TradesOnly)
                return IsTradeEvent(debugEvent, logSettings);

            return ShouldIncludeEventCustom(debugEvent, logSettings);
        }

        private static bool ShouldIncludeEventCustom(EM_Component_Event debugEvent, EM_DebugLogSettings settings)
        {
            switch (debugEvent.Type)
            {
                case EM_DebugEventType.SignalEmitted:
                    if (!settings.IncludeSignals)
                        return false;

                    return IsSignalAllowed(debugEvent.SignalId, settings);

                case EM_DebugEventType.IntentCreated:
                    return settings.IncludeIntents;

                case EM_DebugEventType.EffectApplied:
                    return settings.IncludeEffects;

                case EM_DebugEventType.InteractionAttempt:
                    return settings.IncludeInteractionAttempts;

                case EM_DebugEventType.InteractionSuccess:
                    return settings.IncludeInteractionSuccess;

                case EM_DebugEventType.InteractionFail:
                    return settings.IncludeInteractionFailures;

                case EM_DebugEventType.ScheduleWindow:
                    return settings.IncludeScheduleStart;

                case EM_DebugEventType.ScheduleEnd:
                    return settings.IncludeScheduleEnd;

                case EM_DebugEventType.ScheduleTick:
                    return settings.IncludeScheduleTick;

                default:
                    return true;
            }
        }

        private static bool IsTradeEvent(EM_Component_Event debugEvent, EM_DebugLogSettings settings)
        {
            if (debugEvent.Type == EM_DebugEventType.InteractionAttempt)
                return true;

            if (debugEvent.Type == EM_DebugEventType.InteractionSuccess)
                return true;

            if (debugEvent.Type == EM_DebugEventType.InteractionFail)
                return true;

            if (debugEvent.Type != EM_DebugEventType.SignalEmitted)
                return false;

            return IsTradeSignal(debugEvent.SignalId, settings);
        }

        private static bool IsSignalAllowed(FixedString64Bytes signalId, EM_DebugLogSettings settings)
        {
            if (settings.IncludeNeedSignals && settings.IncludeTradeSignals && settings.IncludeOtherSignals)
                return true;

            bool matchesNeed = StartsWithPrefix(signalId, settings.NeedSignalPrefix);
            bool matchesTrade = StartsWithPrefix(signalId, settings.TradeSignalPrefix);

            if (matchesNeed)
                return settings.IncludeNeedSignals;

            if (matchesTrade)
                return settings.IncludeTradeSignals;

            return settings.IncludeOtherSignals;
        }

        private static bool IsTradeSignal(FixedString64Bytes signalId, EM_DebugLogSettings settings)
        {
            if (settings == null)
                return false;

            return StartsWithPrefix(signalId, settings.TradeSignalPrefix);
        }

        private static bool StartsWithPrefix(FixedString64Bytes value, string prefix)
        {
            if (value.Length == 0)
                return false;

            if (string.IsNullOrEmpty(prefix))
                return false;

            string valueString = value.ToString();
            return valueString.StartsWith(prefix, StringComparison.Ordinal);
        }
        #endregion
    }
}
