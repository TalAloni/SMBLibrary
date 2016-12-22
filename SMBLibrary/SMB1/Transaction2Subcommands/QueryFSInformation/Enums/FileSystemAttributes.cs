using System;

namespace SMBLibrary.SMB1
{
    [Flags]
    public enum FileSystemAttributes : uint
    {
        FILE_CASE_SENSITIVE_SEARCH = 0x0001,
        FILE_CASE_PRESERVED_NAMES = 0x0002,
        FILE_UNICODE_ON_DISK = 0x0004,
        FILE_PERSISTENT_ACLS = 0x0008,
        FILE_FILE_COMPRESSION = 0x0010,
        FILE_VOLUME_QUOTAS = 0x0020, // SMB 1.0 addition
        FILE_SUPPORTS_SPARSE_FILES = 0x0040, // SMB 1.0 addition
        FILE_SUPPORTS_REPARSE_POINTS = 0x0080, // SMB 1.0 addition
        FILE_SUPPORTS_REMOTE_STORAGE = 0x0100, // SMB 1.0 addition
        FILE_VOLUME_IS_COMPRESSED = 0x8000,
        FILE_SUPPORTS_OBJECT_IDS = 0x00010000, // SMB 1.0 addition
        FILE_SUPPORTS_ENCRYPTION = 0x00020000, // SMB 1.0 addition
        FILE_NAMED_STREAMS = 0x00040000, // SMB 1.0 addition
        FILE_READ_ONLY_VOLUME = 0x00080000, // SMB 1.0 addition
        FILE_SEQUENTIAL_WRITE_ONCE = 0x00100000, // SMB 1.0 addition
        FILE_SUPPORTS_TRANSACTIONS = 0x00200000, // SMB 1.0 addition
        FILE_SUPPORTS_HARD_LINKS = 0x00400000, // SMB 1.0 addition
        FILE_SUPPORTS_EXTENDED_ATTRIBUTES = 0x00800000, // SMB 1.0 addition
        FILE_SUPPORTS_OPEN_BY_FILE_ID = 0x01000000, // SMB 1.0 addition
        FILE_SUPPORTS_USN_JOURNAL = 0x02000000, // SMB 1.0 addition
    }
}
