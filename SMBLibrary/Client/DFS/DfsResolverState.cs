namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Tracks state through the 14-step DFS resolution algorithm per MS-DFSC section 3.1.4.2.
    /// Generic parameter T represents the connection/session context type.
    /// </summary>
    /// <typeparam name="T">The type of context object (e.g., session, connection).</typeparam>
    /// <remarks>
    /// This class is infrastructure for future state machine improvements to the DFS resolver.
    /// It can be used to track resolution progress across multiple steps and enable
    /// more sophisticated retry/failover logic.
    /// </remarks>
    public class DfsResolverState<T>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsResolverState{T}"/>.
        /// </summary>
        /// <param name="originalPath">The original UNC path to resolve.</param>
        /// <param name="context">The connection/session context.</param>
        public DfsResolverState(string originalPath, T context)
        {
            OriginalPath = originalPath;
            CurrentPath = originalPath;
            Context = context;
            LastStatus = NTStatus.STATUS_SUCCESS;
        }

        /// <summary>
        /// Gets the original UNC path that was requested.
        /// </summary>
        public string OriginalPath { get; private set; }

        /// <summary>
        /// Gets or sets the current path being resolved (may be rewritten during resolution).
        /// </summary>
        public string CurrentPath { get; set; }

        /// <summary>
        /// Gets the connection/session context.
        /// </summary>
        public T Context { get; private set; }

        /// <summary>
        /// Gets or sets the type of DFS referral request to issue, if any.
        /// Null when no request type has been determined.
        /// </summary>
        public DfsRequestType? RequestType { get; set; }

        /// <summary>
        /// Gets or sets whether resolution is complete (algorithm reached terminal state).
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets whether the path was determined to be a DFS path.
        /// </summary>
        public bool IsDfsPath { get; set; }

        /// <summary>
        /// Gets or sets the cached referral entry found during lookup, if any.
        /// </summary>
        public ReferralCacheEntry CachedEntry { get; set; }

        /// <summary>
        /// Gets or sets the last NTStatus from a referral request or I/O operation.
        /// </summary>
        public NTStatus LastStatus { get; set; }
    }
}
