using System;

namespace SMBLibrary.SMB1
{
    [Flags]
    public enum DesiredAccess : uint
    {
        FILE_READ_DATA = 0x0001,
        FILE_WRITE_DATA = 0x0002,
        FILE_APPEND_DATA = 0x0004,
        FILE_READ_EA = 0x0008,
        FILE_WRITE_EA = 0x0010,
        FILE_EXECUTE = 0x0020,
        FILE_READ_ATTRIBUTES = 0x0080,
        FILE_WRITE_ATTRIBUTES = 0x0100,
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
    }
}
