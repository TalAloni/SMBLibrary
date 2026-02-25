
namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/e5a70738-7ee4-46d9-a5f7-6644daa49a51">
    /// [MS-FSCC] 2.5.2 - FileFsControlInformation</see>
    /// </summary>
    public enum FileSystemControlFlags : uint
    {
        /// <summary>
        /// FILE_VC_QUOTA_TRACK.
        /// Quotas are tracked on the volume, but they are not enforced.
        /// Tracked quotas enable reporting on the file system space used by system users.
        /// If both this flag and FILE_VC_QUOTA_ENFORCE are specified, FILE_VC_QUOTA_ENFORCE is ignored.
        /// 
        /// Note: This flag takes precedence over FILE_VC_QUOTA_ENFORCE. In other words,
        /// if both FILE_VC_QUOTA_TRACK and FILE_VC_QUOTA_ENFORCE are set, the FILE_VC_QUOTA_ENFORCE flag
        /// is ignored. This flag will be ignored if a client attempts to set it.
        /// </summary>
        QuotaTrack = 0x00000001,

        /// <summary>
        /// FILE_VC_QUOTA_ENFORCE.
        /// Quotas are tracked and enforced on the volume.
        /// </summary>
        QuotaEnforce = 0x00000002,

        /// <summary>
        /// FILE_VC_CONTENT_INDEX_DISABLED.
        /// Content indexing is disabled.
        /// </summary>
        ContentIndexingDisabled = 0x00000008,

        /// <summary>
        /// FILE_VC_LOG_QUOTA_THRESHOLD.
        /// An event log entry will be created when the user exceeds his or her assigned quota warning threshold.
        /// </summary>
        LogQuotaThreshold = 0x00000010,

        /// <summary>
        /// FILE_VC_LOG_QUOTA_LIMIT.
        /// An event log entry will be created when the user exceeds the assigned disk quota limit.
        /// </summary>
        LogQuotaLimit = 0x00000020,

        /// <summary>
        /// FILE_VC_LOG_VOLUME_THRESHOLD.
        /// An event log entry will be created when the volume's free space threshold is exceeded.
        /// </summary>
        LogVolumeThreshold = 0x00000040,

        /// <summary>
        /// FILE_VC_LOG_VOLUME_LIMIT.
        /// An event log entry will be created when the volume's free space limit is exceeded.
        /// </summary>
        LogVolumeLimit = 0x00000080,

        /// <summary>
        /// FILE_VC_QUOTAS_INCOMPLETE.
        /// The quota information for the volume is incomplete because it is corrupt,
        /// or the system is in the process of rebuilding the quota information.
        /// 
        /// Note: This does not necessarily imply that FILE_VC_QUOTAS_REBUILDING is set.
        /// This flag will be ignored if a client attempts to set it.
        /// </summary>
        QuotasIncomplete = 0x00000100,

        /// <summary>
        /// FILE_VC_QUOTAS_REBUILDING.
        /// The file system is rebuilding the quota information for the volume.
        /// 
        /// Note: This does not necessarily imply that FILE_VC_QUOTAS_INCOMPLETE is set.
        /// This flag will be ignored if a client attempts to set it.
        /// </summary>
        QuotasRebuilding = 0x00000200,
    }
}
