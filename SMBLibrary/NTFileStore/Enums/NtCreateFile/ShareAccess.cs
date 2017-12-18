using System;

namespace SMBLibrary
{
    /// <summary>
    /// No bits set = Prevents the file from being shared
    /// </summary>
    [Flags]
    public enum ShareAccess : uint
    {
        Read = 0x0001,   // FILE_SHARE_READ
        Write = 0x0002,  // FILE_SHARE_WRITE
        Delete = 0x0004, // FILE_SHARE_DELETE
    }
}
