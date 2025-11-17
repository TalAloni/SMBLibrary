namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// DFS client options for controlling DFS-related behavior on a per-client or per-connection basis.
    /// For vNext-DFS, DFS is disabled by default.
    /// </summary>
    public class DfsClientOptions
    {
        /// <summary>
        /// When false (default), DFS client behavior is disabled and no DFS referral requests are issued.
        /// When true, DFS-related features may be enabled for the associated client/connection.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
