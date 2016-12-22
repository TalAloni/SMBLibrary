using System;

namespace SMBLibrary.SMB1
{
    [Flags]
    public enum FileStatus : ushort
    {
        NO_EAS = 0x01,
        NO_SUBSTREAMS = 0x02,
        NO_REPARSETAG = 0x04,
    }
}
