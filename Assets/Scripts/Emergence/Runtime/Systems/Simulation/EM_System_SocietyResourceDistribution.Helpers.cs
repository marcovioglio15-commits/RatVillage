using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    public partial struct EM_System_SocietyResourceDistribution : ISystem
    {
        #region Helpers
        private void BuildMemberLists(ref SystemState state, ref NativeList<Entity> members, ref NativeList<Entity> memberSocieties)
        {
            foreach ((EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<EM_Component_SocietyMember>().WithAll<EM_BufferElement_NeedRule, EM_BufferElement_Need, EM_BufferElement_Resource>().WithEntityAccess())
            {
                members.Add(entity);
                memberSocieties.Add(member.SocietyRoot);
            }
        }

        private static float GetIntervalSeconds(float tickRate)
        {
            if (tickRate <= 0f)
                return 1f;

            return 1f / tickRate;
        }

        private static void ApplyDistribution(DynamicBuffer<EM_BufferElement_Resource> societyResources, DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_NeedRule> rules, DynamicBuffer<EM_BufferElement_Resource> memberResources,
            EM_Component_SocietyResourceDistributionSettings settings, int maxTransfers, bool hasDebugBuffer,
            DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, float timeOfDay, Entity society, Entity member)
        {
            for (int attempt = 0; attempt < maxTransfers; attempt++)
            {
                int ruleIndex;
                float availableAmount;
                bool found = SelectBestRule(needs, rules, societyResources, out ruleIndex, out availableAmount);

                if (!found)
                    return;

                EM_BufferElement_NeedRule rule = rules[ruleIndex];
                float transferAmount = GetTransferAmount(rule, settings, availableAmount);

                if (transferAmount <= 0f)
                    return;

                ApplyResourceDelta(societyResources, rule.ResourceId, -transferAmount);
                ApplyResourceDelta(memberResources, rule.ResourceId, transferAmount);

                float satisfaction = GetSatisfactionAmount(rule, settings, transferAmount);
                ApplyNeedDelta(needs, rule.NeedId, -satisfaction, rule.MinValue, rule.MaxValue);

                if (hasDebugBuffer)
                {
                    AppendDistributionDebugEvent(debugBuffer, maxEntries, timeOfDay, society, member, rule.NeedId, rule.ResourceId,
                        transferAmount);
                }
            }
        }

        private static bool SelectBestRule(DynamicBuffer<EM_BufferElement_Need> needs, DynamicBuffer<EM_BufferElement_NeedRule> rules,
            DynamicBuffer<EM_BufferElement_Resource> societyResources, out int ruleIndex, out float availableAmount)
        {
            ruleIndex = -1;
            availableAmount = 0f;
            float bestScore = 0f;

            for (int i = 0; i < rules.Length; i++)
            {
                EM_BufferElement_NeedRule rule = rules[i];

                if (rule.NeedId.Length == 0)
                    continue;

                if (rule.ResourceId.Length == 0)
                    continue;

                int currentNeedIndex = FindNeedIndex(needs, rule.NeedId);

                if (currentNeedIndex < 0)
                    continue;

                float maxValue = math.max(rule.MinValue, rule.MaxValue);
                float startThreshold = math.min(rule.StartThreshold, maxValue);
                float range = maxValue - startThreshold;

                if (range <= 0f)
                    continue;

                float needValue = needs[currentNeedIndex].Value;

                if (needValue < startThreshold)
                    continue;

                float currentAvailable = GetResourceAmount(societyResources, rule.ResourceId);

                if (currentAvailable <= 0f)
                    continue;

                float normalized = math.saturate((needValue - startThreshold) / range);

                if (normalized <= bestScore)
                    continue;

                bestScore = normalized;
                ruleIndex = i;
                availableAmount = currentAvailable;
            }

            if (ruleIndex < 0)
                return false;

            if (availableAmount <= 0f)
                return false;

            return true;
        }

        private static float GetTransferAmount(EM_BufferElement_NeedRule rule, EM_Component_SocietyResourceDistributionSettings settings, float availableAmount)
        {
            float transferAmount = rule.ResourceTransferAmount;

            if (transferAmount <= 0f)
                transferAmount = settings.DefaultTransferAmount;

            if (transferAmount <= 0f)
                transferAmount = availableAmount;

            return math.min(availableAmount, transferAmount);
        }

        private static float GetSatisfactionAmount(EM_BufferElement_NeedRule rule, EM_Component_SocietyResourceDistributionSettings settings, float transferAmount)
        {
            float satisfaction = rule.NeedSatisfactionAmount;

            if (satisfaction <= 0f)
                satisfaction = settings.DefaultNeedSatisfaction;

            if (satisfaction <= 0f)
                satisfaction = transferAmount;

            return satisfaction;
        }

        private static int FindNeedIndex(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId)
        {
            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                return i;
            }

            return -1;
        }

        private static float GetResourceAmount(DynamicBuffer<EM_BufferElement_Resource> resources, FixedString64Bytes resourceId)
        {
            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                return resources[i].Amount;
            }

            return 0f;
        }

        private static void ApplyResourceDelta(DynamicBuffer<EM_BufferElement_Resource> resources, FixedString64Bytes resourceId, float delta)
        {
            if (resourceId.Length == 0)
                return;

            for (int i = 0; i < resources.Length; i++)
            {
                if (!resources[i].ResourceId.Equals(resourceId))
                    continue;

                EM_BufferElement_Resource entry = resources[i];
                entry.Amount += delta;
                resources[i] = entry;
                return;
            }

            EM_BufferElement_Resource newEntry = new EM_BufferElement_Resource
            {
                ResourceId = resourceId,
                Amount = delta
            };

            resources.Add(newEntry);
        }

        private static void ApplyNeedDelta(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId, float delta, float minValue, float maxValue)
        {
            if (needId.Length == 0)
                return;

            float minClamp = math.min(minValue, maxValue);
            float maxClamp = math.max(minValue, maxValue);

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                EM_BufferElement_Need entry = needs[i];
                entry.Value = math.clamp(entry.Value + delta, minClamp, maxClamp);
                needs[i] = entry;
                return;
            }

            EM_BufferElement_Need newEntry = new EM_BufferElement_Need
            {
                NeedId = needId,
                Value = math.clamp(delta, minClamp, maxClamp)
            };

            needs.Add(newEntry);
        }
        #endregion
    }
}
