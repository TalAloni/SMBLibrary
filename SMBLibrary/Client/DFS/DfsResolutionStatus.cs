namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Status of a DFS client resolution attempt.
    /// </summary>
    public enum DfsResolutionStatus
    {
        /// <summary>
        /// DFS resolution does not apply (for example, DFS is disabled for this connection or path).
        /// </summary>
        NotApplicable = 0,

        /// <summary>
        /// DFS resolution succeeded and a target path is available.
        /// </summary>
        Success = 1,

        /// <summary>
        /// DFS resolution was attempted but failed (e.g., malformed referrals, network errors).
        /// </summary>
        Error = 2,
    }
}
