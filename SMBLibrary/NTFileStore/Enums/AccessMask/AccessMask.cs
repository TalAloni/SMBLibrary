using System;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DTYP] 2.4.3 - ACCESS_MASK
    /// </summary>
    [Flags]
    public enum AccessMask : uint
    {
        FILE_READ_DATA = 0x00000001,
        FILE_WRITE_DATA = 0x00000002,
        FILE_APPEND_DATA = 0x00000004,
        FILE_READ_ATTRIBUTES = 0x00000080,

        // The bits in positions 16 through 31 are object specific.
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
        ACCESS_SYSTEM_SECURITY = 0x01000000,
        MAXIMUM_ALLOWED = 0x02000000,
        GENERIC_ALL = 0x10000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_READ = 0x80000000,
    }
}
