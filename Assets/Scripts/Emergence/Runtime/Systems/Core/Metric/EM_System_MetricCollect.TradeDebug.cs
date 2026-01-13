using System;
using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_MetricCollect : ISystem
    {
        #region TradeDebug
        private const string TradeSignalPrefix = "Signal.Trade.";
        private const int TradeSignalPrefixLength = 13;
        private const int TradeSignalDuplicateScan = 16;

        private static void TryAppendTradeSignalDebugEvent(EM_BufferElement_SignalEvent signalEvent,
            DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (signalEvent.SignalId.Length == 0)
                return;

            if (!IsTradeSignal(signalEvent.SignalId))
                return;

            if (HasRecentTradeSignalLog(signalEvent, debugBuffer))
                return;

            EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildSignalEvent(signalEvent.SignalId, signalEvent.Value, signalEvent.ContextId,
                signalEvent.Subject, signalEvent.Target, signalEvent.SocietyRoot, signalEvent.Time);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
        }

        private static bool IsTradeSignal(FixedString64Bytes signalId)
        {
            if (signalId.Length < TradeSignalPrefixLength)
                return false;

            string value = signalId.ToString();
            return value.StartsWith(TradeSignalPrefix, StringComparison.Ordinal);
        }

        private static bool HasRecentTradeSignalLog(EM_BufferElement_SignalEvent signalEvent, DynamicBuffer<EM_Component_Event> debugBuffer)
        {
            int count = debugBuffer.Length;

            if (count <= 0)
                return false;

            int start = count - TradeSignalDuplicateScan;

            if (start < 0)
                start = 0;

            for (int i = count - 1; i >= start; i--)
            {
                EM_Component_Event entry = debugBuffer[i];

                if (entry.Type != EM_DebugEventType.SignalEmitted)
                    continue;

                if (!entry.SignalId.Equals(signalEvent.SignalId))
                    continue;

                if (entry.Subject != signalEvent.Subject)
                    continue;

                if (entry.Target != signalEvent.Target)
                    continue;

                if (!entry.ContextId.Equals(signalEvent.ContextId))
                    continue;

                if (entry.Value != signalEvent.Value)
                    continue;

                return true;
            }

            return false;
        }
        #endregion
    }
}
