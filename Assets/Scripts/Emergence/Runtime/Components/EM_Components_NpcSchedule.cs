using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
    public enum EM_ScheduleTradePolicy : byte
    {
        AllowAll = 0,
        AllowOnlyListed = 1,
        BlockAll = 2
    }

    // Components and blob definitions for per-NPC schedules.
    #region Components
    public struct EM_Component_NpcSchedule : IComponentData
    {
        #region Data
        public BlobAssetReference<EM_BlobDefinition_NpcSchedule> Schedule;
        #endregion
    }

    public struct EM_Component_NpcScheduleState : IComponentData
    {
        #region Data
        public int CurrentEntryIndex;
        public FixedString64Bytes CurrentActivityId;
        public byte IsOverride;
        #endregion
    }

    public struct EM_Component_NpcScheduleTarget : IComponentData
    {
        #region Data
        public int EntryIndex;
        public FixedString64Bytes ActivityId;
        public FixedString64Bytes LocationId;
        public byte IsOverride;
        public byte TradeCapable;
        #endregion
    }

    public struct EM_Component_NpcScheduleOverride : IComponentData
    {
        #region Data
        public FixedString64Bytes ActivityId;
        public float RemainingHours;
        public float DurationHours;
        public int EntryIndex;
        #endregion
    }

    public struct EM_Component_NpcScheduleOverrideGate : IComponentData
    {
        #region Data
        public double LastOverrideTimeSeconds;
        public float LastOverridePriority;
        public FixedString64Bytes LastOverrideActivityId;
        #endregion
    }

    public struct EM_Component_NpcScheduleDuration : IComponentData
    {
        #region Data
        public FixedString64Bytes ActivityId;
        public float RemainingHours;
        public float DurationHours;
        public int EntryIndex;
        #endregion
    }

    public struct EM_BufferElement_NpcScheduleSignalState : IBufferElementData
    {
        #region Data
        public int SignalIndex;
        public float TickAccumulatorHours;
        #endregion
    }
    #endregion

    // Blob data layout for schedule presets.
    #region Blobs
    public struct EM_Blob_NpcScheduleSignal
    {
        #region Data
        public FixedString64Bytes StartSignalId;
        public FixedString64Bytes TickSignalId;
        public float TickIntervalHours;
        public BlobArray<float> CurveSamples;
        #endregion
    }

    public struct EM_BlobDefinition_NpcSchedule
    {
        #region Data
        public BlobArray<EM_Blob_NpcScheduleEntry> Entries;
        #endregion
    }

    public struct EM_Blob_NpcScheduleEntry
    {
        #region Data
        public FixedString64Bytes ActivityId;
        public FixedString64Bytes LocationId;
        public float StartHour;
        public float EndHour;
        public byte UseDuration;
        public float MinDurationHours;
        public float MaxDurationHours;
        public byte TradeCapable;
        public byte TradePolicy;
        public BlobArray<FixedString64Bytes> AllowedTradeNeedIds;
        public BlobArray<EM_Blob_NpcScheduleSignal> Signals;
        #endregion
    }
    #endregion
}
