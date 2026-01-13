using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace EmergentMechanics
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EM_System_NeedUpdate))]
    [UpdateBefore(typeof(EM_System_MetricCollect))]
    public partial struct EM_System_NpcHealthSignals : ISystem
    {
        #region Fields
        private ComponentLookup<EM_Component_HealthSignalSettings> signalSettingsLookup;
        #endregion

        #region Unity Lifecycle
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EM_Component_NpcHealth>();
            state.RequireForUpdate<EM_Component_NpcHealthTickState>();
            signalSettingsLookup = state.GetComponentLookup<EM_Component_HealthSignalSettings>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeParallelHashMap<Entity, HealthTickData> tickDataMap = BuildTickConfigMap(ref state);

            if (tickDataMap.Count() == 0)
            {
                tickDataMap.Dispose();
                return;
            }

            bool hasDebugBuffer = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<EM_Component_Event> debugBuffer);
            int maxEntries = 0;
            EM_Component_Log debugLog = default;
            RefRW<EM_Component_Log> debugLogRef = default;

            if (hasDebugBuffer)
            {
                debugLogRef = SystemAPI.GetSingletonRW<EM_Component_Log>();
                debugLog = debugLogRef.ValueRO;
                maxEntries = debugLog.MaxEntries;
            }

            signalSettingsLookup.Update(ref state);

            foreach ((RefRO<EM_Component_NpcHealth> health, RefRW<EM_Component_NpcHealthTickState> tickState,
                DynamicBuffer<EM_BufferElement_NeedDamageSetting> damageSettings, DynamicBuffer<EM_BufferElement_Need> needs,
                DynamicBuffer<EM_BufferElement_NeedSetting> needSettings, DynamicBuffer<EM_BufferElement_SignalEvent> signals,
                EM_Component_SocietyMember member, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NpcHealth>, RefRW<EM_Component_NpcHealthTickState>,
                    DynamicBuffer<EM_BufferElement_NeedDamageSetting>, DynamicBuffer<EM_BufferElement_Need>,
                    DynamicBuffer<EM_BufferElement_NeedSetting>, DynamicBuffer<EM_BufferElement_SignalEvent>,
                    EM_Component_SocietyMember>()
                    .WithAll<EM_Component_SignalEmitter>()
                    .WithNone<Disabled>()
                    .WithEntityAccess())
            {
                HealthTickData tickData;
                bool found = tickDataMap.TryGetValue(member.SocietyRoot, out tickData);

                if (!found)
                    continue;

                if (tickData.TimeSeconds < tickState.ValueRO.NextTick)
                    continue;

                EM_Component_NpcHealthTickState updatedTick = tickState.ValueRO;
                updatedTick.NextTick = tickData.TimeSeconds + tickData.IntervalSeconds;
                tickState.ValueRW = updatedTick;

                if (member.SocietyRoot == Entity.Null || !signalSettingsLookup.HasComponent(member.SocietyRoot))
                    continue;

                EM_Component_HealthSignalSettings settings = signalSettingsLookup[member.SocietyRoot];

                float maxHealth = math.max(health.ValueRO.Max, 0f);
                float currentHealth = math.max(health.ValueRO.Current, 0f);

                if (maxHealth > 0f && settings.HealthValueSignalId.Length > 0)
                {
                    float normalizedHealth = math.saturate(currentHealth / maxHealth);
                    EmitHealthSignal(settings.HealthValueSignalId, normalizedHealth, default, signals, entity, member.SocietyRoot,
                        tickData.TimeSeconds, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
                }

                if (damageSettings.Length == 0 || settings.HealthDamageSignalId.Length == 0 || maxHealth <= 0f)
                    continue;

                float deltaHours = (float)(tickData.IntervalSeconds / 3600f);

                if (deltaHours <= 0f)
                    continue;

                for (int i = 0; i < damageSettings.Length; i++)
                {
                    EM_BufferElement_NeedDamageSetting damageSetting = damageSettings[i];

                    if (damageSetting.NeedId.Length == 0 || damageSetting.DamagePerHour <= 0f)
                        continue;

                    float urgency;
                    bool hasUrgency = TryResolveNeedUrgency(needs, needSettings, damageSetting.NeedId, out urgency);

                    if (!hasUrgency)
                        continue;

                    if (urgency <= damageSetting.UrgencyThreshold)
                        continue;

                    float damage = damageSetting.DamagePerHour * deltaHours;
                    float normalizedDamage = math.saturate(damage / maxHealth);

                    if (normalizedDamage <= 0f)
                        continue;

                    EmitHealthSignal(settings.HealthDamageSignalId, normalizedDamage, damageSetting.NeedId, signals, entity,
                        member.SocietyRoot, tickData.TimeSeconds, hasDebugBuffer, debugBuffer, maxEntries, ref debugLog);
                }
            }

            if (hasDebugBuffer)
                debugLogRef.ValueRW = debugLog;

            tickDataMap.Dispose();
        }
        #endregion

        #region Helpers
        private struct HealthTickData
        {
            public float IntervalSeconds;
            public double TimeSeconds;

            public HealthTickData(float intervalSeconds, double timeSeconds)
            {
                IntervalSeconds = intervalSeconds;
                TimeSeconds = timeSeconds;
            }
        }

        private NativeParallelHashMap<Entity, HealthTickData> BuildTickConfigMap(ref SystemState state)
        {
            NativeParallelHashMap<Entity, HealthTickData> tickDataMap = new NativeParallelHashMap<Entity, HealthTickData>(8, Allocator.Temp);

            foreach ((RefRO<EM_Component_NeedTickSettings> settings, RefRO<EM_Component_SocietyClock> clock, Entity entity)
                in SystemAPI.Query<RefRO<EM_Component_NeedTickSettings>, RefRO<EM_Component_SocietyClock>>()
                    .WithAll<EM_Component_SocietyRoot>()
                    .WithEntityAccess())
            {
                float intervalSeconds = GetIntervalSeconds(settings.ValueRO.TickIntervalHours);
                double timeSeconds = clock.ValueRO.SimulatedTimeSeconds;
                tickDataMap.TryAdd(entity, new HealthTickData(intervalSeconds, timeSeconds));
            }

            return tickDataMap;
        }

        private static float GetIntervalSeconds(float intervalHours)
        {
            if (intervalHours <= 0f)
                return 1f;

            return intervalHours * 3600f;
        }

        private static void EmitHealthSignal(FixedString64Bytes signalId, float value, FixedString64Bytes contextId,
            DynamicBuffer<EM_BufferElement_SignalEvent> signals, Entity subject, Entity societyRoot, double timeSeconds,
            bool hasDebugBuffer, DynamicBuffer<EM_Component_Event> debugBuffer, int maxEntries, ref EM_Component_Log debugLog)
        {
            if (signalId.Length == 0)
                return;

            EM_BufferElement_SignalEvent signalEvent = new EM_BufferElement_SignalEvent
            {
                SignalId = signalId,
                Value = value,
                Subject = subject,
                Target = Entity.Null,
                SocietyRoot = societyRoot,
                ContextId = contextId,
                Time = timeSeconds
            };

            signals.Add(signalEvent);

            if (!hasDebugBuffer)
                return;

            EM_Component_Event debugEvent = EM_Utility_LogEvent.BuildSignalEvent(signalId, value, contextId, subject, Entity.Null, societyRoot,
                timeSeconds);
            EM_Utility_LogEvent.AppendEvent(debugBuffer, maxEntries, ref debugLog, debugEvent);
        }

        private static bool TryResolveNeedUrgency(DynamicBuffer<EM_BufferElement_Need> needs,
            DynamicBuffer<EM_BufferElement_NeedSetting> settings, FixedString64Bytes needId, out float urgency)
        {
            urgency = 0f;

            if (needId.Length == 0)
                return false;

            float minValue = 0f;
            float maxValue = 1f;
            bool hasSetting = false;

            for (int i = 0; i < settings.Length; i++)
            {
                if (!settings[i].NeedId.Equals(needId))
                    continue;

                minValue = math.min(settings[i].MinValue, settings[i].MaxValue);
                maxValue = math.max(settings[i].MinValue, settings[i].MaxValue);
                hasSetting = true;
                break;
            }

            if (!hasSetting)
                return false;

            float value;
            bool hasNeed = TryGetNeedValue(needs, needId, out value);

            if (!hasNeed)
                return false;

            float range = maxValue - minValue;

            if (range <= 0f)
                return false;

            urgency = math.saturate((value - minValue) / range);
            return true;
        }

        private static bool TryGetNeedValue(DynamicBuffer<EM_BufferElement_Need> needs, FixedString64Bytes needId, out float value)
        {
            value = 0f;

            if (needId.Length == 0)
                return false;

            for (int i = 0; i < needs.Length; i++)
            {
                if (!needs[i].NeedId.Equals(needId))
                    continue;

                value = needs[i].Value;
                return true;
            }

            return false;
        }
        #endregion
    }
}
