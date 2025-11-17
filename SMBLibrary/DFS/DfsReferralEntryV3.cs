namespace SMBLibrary
{
    public class DfsReferralEntryV3 : DfsReferralEntry
    {
        public ushort VersionNumber { get; set; }
        public ushort Size { get; set; }
        public uint TimeToLive { get; set; }

        // v3-specific fields (modeled for now similarly to v2):
        public ushort ServerType { get; set; }
        public ushort ReferralEntryFlags { get; set; }
        public string DfsPath { get; set; }
        public string DfsAlternatePath { get; set; }
        public string NetworkAddress { get; set; }
    }
}
