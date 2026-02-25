using System;

namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/ebc7e6e5-4650-4e54-b17c-cf60f6fbeeaa">
    /// [MS-FSCC] 2.5.1 - FileFsAttributeInformation</see>
    /// </summary>
    [Flags]
    public enum FileSystemAttributes : uint
    {
        /// <summary>
        /// FILE_CASE_SENSITIVE_SEARCH.
        /// The file system supports case-sensitive file names when looking up (searching for) file names in a directory.
        /// </summary>
        CaseSensitiveSearch = 0x0001,

        /// <summary>
        /// FILE_CASE_PRESERVED_NAMES.
        /// The file system preserves the case of file names when it places a name on disk.
        /// </summary>
        CasePreservedNames = 0x0002, 

        /// <summary>
        /// FILE_CASE_PRESERVED_NAMES.
        /// This enum value is a typo retained for backwards compatibility, use <see cref="CasePreservedNames"/> instead.
        /// </summary>
        CasePreservedNamed = 0x0002,

        /// <summary>
        /// FILE_UNICODE_ON_DISK.
        /// The file system supports Unicode in file and directory names. This flag applies only to file
        /// and directory names; the file system neither restricts nor interprets the bytes of data within a file.
        /// </summary>
        UnicodeOnDisk = 0x0004,

        /// <summary>
        /// FILE_PERSISTENT_ACLS.
        /// The file system preserves and enforces access control lists (ACLs).
        /// </summary>
        PersistentACLs = 0x0008,

        /// <summary>
        /// FILE_FILE_COMPRESSION.
        /// The file volume supports file-based compression. This flag is incompatible with the FILE_VOLUME_IS_COMPRESSED flag.
        /// </summary>
        FileCompression = 0x0010,

        /// <summary>
        /// FILE_VOLUME_QUOTAS.
        /// The file system supports per-user quotas.
        /// </summary>
        VolumeQuotas = 0x0020,

        /// <summary>
        /// FILE_SUPPORTS_SPARSE_FILES.
        /// The file system supports sparse files.
        /// </summary>
        SupportsSparseFiles = 0x0040,

        /// <summary>
        /// FILE_SUPPORTS_REPARSE_POINTS.
        /// The file system supports reparse points.
        /// </summary>
        SupportsReparsePoints = 0x0080,

        /// <summary>
        /// FILE_SUPPORTS_REMOTE_STORAGE.
        /// The file system supports remote storage.
        /// </summary>
        SupportsRemoteStorage = 0x0100,

        /// <summary>
        /// FILE_RETURNS_CLEANUP_RESULT_INFO.
        /// On a successful cleanup operation, the file system returns information that describes additional
        /// actions taken during cleanup, such as deleting the file. File system filters can examine this
        /// information in their post-cleanup callback.
        /// </summary>
        ReturnsCleanupResultInfo = 0x0200,

        /// <summary>
        /// FILE_SUPPORTS_POSIX_UNLINK_RENAME.
        /// The file system supports POSIX-style delete and rename operations.
        /// </summary>
        SupportsPOSIXUnlinkRename = 0x0400,

        /// <summary>
        /// FILE_VOLUME_IS_COMPRESSED.
        /// The specified volume is a compressed volume. This flag is incompatible with the FILE_FILE_COMPRESSION flag. 
        /// </summary>
        VolumeIsCompressed = 0x8000,

        /// <summary>
        /// FILE_SUPPORTS_OBJECT_IDS.
        /// The file system supports object identifiers.
        /// </summary>
        SupportsObjectIDs = 0x00010000,

        /// <summary>
        /// FILE_SUPPORTS_ENCRYPTION.
        /// The file system supports the Encrypted File System (EFS).
        /// </summary>
        SupportsEncryption = 0x00020000,

        /// <summary>
        /// FILE_NAMED_STREAMS.
        /// The file system supports named streams.
        /// </summary>
        NamedStreams = 0x00040000,

        /// <summary>
        /// FILE_READ_ONLY_VOLUME.
        /// If set, the volume has been mounted in read-only mode.
        /// </summary>
        ReadOnlyVolume = 0x00080000,

        /// <summary>
        /// FILE_SEQUENTIAL_WRITE_ONCE.
        /// The underlying volume is write once.
        /// </summary>
        SequentialWriteOnce = 0x00100000,

        /// <summary>
        /// FILE_SUPPORTS_TRANSACTIONS.
        /// The volume supports transactions.
        /// </summary>
        SupportsTransactions = 0x00200000,

        /// <summary>
        /// FILE_SUPPORTS_HARD_LINKS.
        /// The file system supports hard linking files.
        /// </summary>
        SupportsHardLinks = 0x00400000,

        /// <summary>
        /// FILE_SUPPORTS_EXTENDED_ATTRIBUTES.
        /// The file system persistently stores Extended Attribute information per file.
        /// </summary>
        SupportsExtendedAttributes = 0x00800000,

        /// <summary>
        /// FILE_SUPPORTS_OPEN_BY_FILE_ID.
        /// The file system supports opening a file by FileID or ObjectID.
        /// </summary>
        SupportsOpenByFileID = 0x01000000,

        /// <summary>
        /// FILE_SUPPORTS_USN_JOURNAL.
        /// The file system implements a USN change journal.
        /// </summary>
        SupportsUSNJournal = 0x02000000,

        /// <summary>
        /// FILE_SUPPORT_INTEGRITY_STREAMS.
        /// The file system supports integrity streams.
        /// </summary>
        SupportsIntegrityStreams = 0x04000000,

        /// <summary>
        /// FILE_SUPPORTS_BLOCK_REFCOUNTING.
        /// The file system supports sharing logical clusters between files on the same volume.
        /// The file system reallocates on writes to shared clusters.
        /// Indicates that FSCTL_DUPLICATE_EXTENTS_TO_FILE is a supported operation.
        /// </summary>
        SupportsBlockRefCounting = 0x08000000,

        /// <summary>
        /// FILE_SUPPORTS_SPARSE_VDL.
        /// The file system tracks whether each cluster of a file contains valid data
        /// (either from explicit file writes or automatic zeros) or invalid data (has not yet been written to or zeroed).
        /// File systems that use Sparse VDL do not store a valid data length (section 2.4.50)
        /// and do not require that valid data be contiguous within a file.
        /// </summary>
        SupportsSparseVDL = 0x10000000,
    }
}
