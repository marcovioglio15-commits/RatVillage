using Unity.Entities;

namespace EmergentMechanics
{
    public partial struct EM_System_Trade
    {
        #region IntentLookup
        private static bool TryFindIntentIndex(DynamicBuffer<EM_BufferElement_Intent> intents, EM_Component_TradeRequestState requestState,
            out int intentIndex, out EM_BufferElement_Intent intent)
        {
            intentIndex = -1;
            intent = default;

            for (int i = 0; i < intents.Length; i++)
            {
                EM_BufferElement_Intent current = intents[i];

                if (requestState.IntentId.Length > 0 && !current.IntentId.Equals(requestState.IntentId))
                    continue;

                if (requestState.NeedId.Length > 0 && !current.NeedId.Equals(requestState.NeedId))
                    continue;

                if (requestState.ResourceId.Length > 0 && !current.ResourceId.Equals(requestState.ResourceId))
                    continue;

                intentIndex = i;
                intent = current;
                return true;
            }

            return false;
        }
        #endregion
    }
}
