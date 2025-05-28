using System;

namespace SMBLibrary.SMB2
{
    [Flags]
    public enum ChangeNotifyFlags : ushort
    {
        None = 0x0000,
        WatchTree = 0x0001, // SMB2_WATCH_TREE
    }
}
