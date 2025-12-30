using Unity.Collections;
using Unity.Entities;

namespace EmergentMechanics
{
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
        public float TickAccumulatorHours;
        public byte IsOverride;
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
    #endregion

    // Blob data layout for schedule presets.
    #region Blobs
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
        public float StartHour;
        public float EndHour;
        public float TickIntervalHours;
        public FixedString64Bytes StartSignalId;
        public FixedString64Bytes TickSignalId;
        public BlobArray<float> CurveSamples;
        #endregion
    }
    #endregion
}
