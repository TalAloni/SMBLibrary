using System;

namespace SMBLibrary
{
    public class DfsReferralEntryV1 : DfsReferralEntry
    {
        public ushort VersionNumber { get; set; }
        public ushort Size { get; set; }
        public uint TimeToLive { get; set; }
        public string DfsPath { get; set; }
        public string NetworkAddress { get; set; }
    }
}
