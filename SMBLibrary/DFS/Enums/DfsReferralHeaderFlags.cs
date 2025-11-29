using System;

namespace SMBLibrary.DFS
{
    [Flags]
    public enum DfsReferralHeaderFlags : uint
    {
        ReferralServers = 0x00000001,
        StorageServers = 0x00000002,
        TargetFailback = 0x00000004,
    }
}
