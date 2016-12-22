using System;

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// No bits set = Prevents the file from being shared
    /// </summary>
    [Flags]
    public enum ShareAccess : uint
    {
        FILE_SHARE_READ = 0x0001,
        FILE_SHARE_WRITE = 0x0002,
        FILE_SHARE_DELETE = 0x0004,
    }
}
