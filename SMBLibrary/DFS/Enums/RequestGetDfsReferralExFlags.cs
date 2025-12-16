using System;

namespace SMBLibrary.DFS
{
    [Flags]
    public enum RequestGetDfsReferralExFlags : ushort
    {
        SiteName = 0x0001,
    }
}
