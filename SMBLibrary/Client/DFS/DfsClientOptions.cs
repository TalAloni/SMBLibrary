namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// DFS client options for controlling DFS-related behavior on a per-client or per-connection basis.
    /// For vNext-DFS, DFS is disabled by default.
    /// </summary>
    public class DfsClientOptions
    {
        private int _referralCacheTtlSeconds = 300;
        private int _domainCacheTtlSeconds = 300;
        private int _maxRetries = 3;

        /// <summary>
        /// When false (default), DFS client behavior is disabled and no DFS referral requests are issued.
        /// When true, DFS-related features may be enabled for the associated client/connection.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// When true, enables domain cache for domain-based DFS resolution (requires domain environment).
        /// Default: false.
        /// </summary>
        public bool EnableDomainCache { get; set; }

        /// <summary>
        /// When true, enables the full 14-step DFS resolution algorithm per MS-DFSC.
        /// When false, uses a simpler single-request resolution.
        /// Default: false.
        /// </summary>
        public bool EnableFullResolution { get; set; }

        /// <summary>
        /// When true, enables cross-server session management for DFS interlink scenarios.
        /// Default: false.
        /// </summary>
        public bool EnableCrossServerSessions { get; set; }

        /// <summary>
        /// Gets or sets the TTL in seconds for referral cache entries.
        /// Default: 300 seconds (5 minutes).
        /// </summary>
        public int ReferralCacheTtlSeconds
        {
            get { return _referralCacheTtlSeconds; }
            set { _referralCacheTtlSeconds = value; }
        }

        /// <summary>
        /// Gets or sets the TTL in seconds for domain cache entries.
        /// Default: 300 seconds (5 minutes).
        /// </summary>
        public int DomainCacheTtlSeconds
        {
            get { return _domainCacheTtlSeconds; }
            set { _domainCacheTtlSeconds = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of retries for DFS target failover.
        /// Default: 3.
        /// </summary>
        public int MaxRetries
        {
            get { return _maxRetries; }
            set { _maxRetries = value; }
        }

        /// <summary>
        /// Gets or sets the site name for site-aware referral requests.
        /// When null (default), no site name is sent in referral requests.
        /// </summary>
        public string SiteName { get; set; }
    }
}
