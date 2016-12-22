using System;

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_EXT_FILE_ATTR
    /// </summary>
    [Flags]
    public enum ExtendedFileAttributes : uint
    {
        Readonly = 0x001, // ATTR_READONLY
        Hidden = 0x0002, // ATTR_HIDDEN
        System = 0x0004, // ATTR_SYSTEM
        Directory = 0x0010, // ATTR_DIRECTORY
        Archive = 0x0020, // ATTR_ARCHIVE

        /// <summary>
        /// The file has no other attributes set. This attribute is valid only if used alone.
        /// </summary>
        Normal = 0x0080, // ATTR_NORMAL
        Temporary = 0x0100, // ATTR_TEMPORARY
        Sparse = 0x0200, // ATTR_SPARSE, SMB 1.0 Addition
        ReparsePoint = 0x0400, // ATTR_REPARSE_POINT, SMB 1.0 Addition
        Compressed = 0x0800, // ATTR_COMPRESSED
        Offline = 0x1000, // ATTR_OFFLINE, SMB 1.0 Addition
        NotIndexed = 0x2000, // ATTR_NOT_CONTENT_INDEXED, SMB 1.0 Addition
        Encrypted = 0x4000, // ATTR_ENCRYPTED, SMB 1.0 Addition
        PosixSemantics = 0x01000000, // POSIX_SEMANTICS
        BackupSemantics = 0x02000000, // BACKUP_SEMANTICS
        DeleteOnClose = 0x04000000, // DELETE_ON_CLOSE
        SequentialScan = 0x08000000, // SEQUENTIAL_SCAN
        RandomAccess = 0x10000000, // RANDOM_ACCESS
        NoBuffering = 0x10000000, // NO_BUFFERING
        WriteThrough = 0x80000000, // WRITE_THROUGH
    }
}
