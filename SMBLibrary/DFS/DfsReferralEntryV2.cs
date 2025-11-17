namespace SMBLibrary
{
    public class DfsReferralEntryV2 : DfsReferralEntry
    {
        public ushort VersionNumber { get; set; }
        public ushort Size { get; set; }
        public uint TimeToLive { get; set; }

        // v2-specific fields (modeled but not yet parsed from the wire):
        public ushort ServerType { get; set; }
        public ushort ReferralEntryFlags { get; set; }
        public string DfsPath { get; set; }
        public string DfsAlternatePath { get; set; }
        public string NetworkAddress { get; set; }
    }
}
