namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Result of a DFS client resolution attempt.
    /// </summary>
    public class DfsResolutionResult
    {
        /// <summary>
        /// Overall status of the DFS resolution attempt.
        /// </summary>
        public DfsResolutionStatus Status { get; set; }

        /// <summary>
        /// The resolved UNC path when DFS resolution succeeds, or the original path
        /// when DFS is not applicable.
        /// </summary>
        public string ResolvedPath { get; set; }
    }
}
