using System;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Event args for when DFS path resolution starts.
    /// </summary>
    public class DfsResolutionStartedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsResolutionStartedEventArgs"/>.
        /// </summary>
        /// <param name="path">The UNC path being resolved.</param>
        public DfsResolutionStartedEventArgs(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the UNC path being resolved.
        /// </summary>
        public string Path { get; private set; }
    }

    /// <summary>
    /// Event args for when a DFS referral request is issued.
    /// </summary>
    public class DfsReferralRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsReferralRequestedEventArgs"/>.
        /// </summary>
        /// <param name="path">The path for which referral is requested.</param>
        /// <param name="requestType">The type of referral request.</param>
        /// <param name="targetServer">The server receiving the request, if known.</param>
        public DfsReferralRequestedEventArgs(string path, DfsRequestType requestType, string targetServer)
        {
            Path = path;
            RequestType = requestType;
            TargetServer = targetServer;
        }

        /// <summary>
        /// Gets the path for which referral is requested.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the type of referral request.
        /// </summary>
        public DfsRequestType RequestType { get; private set; }

        /// <summary>
        /// Gets the server receiving the request, if known.
        /// </summary>
        public string TargetServer { get; private set; }
    }

    /// <summary>
    /// Event args for when a DFS referral response is received.
    /// </summary>
    public class DfsReferralReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsReferralReceivedEventArgs"/>.
        /// </summary>
        /// <param name="path">The path for which referral was requested.</param>
        /// <param name="status">The status from the referral response.</param>
        /// <param name="referralCount">The number of referral entries received.</param>
        /// <param name="ttlSeconds">The TTL in seconds from the first referral entry.</param>
        public DfsReferralReceivedEventArgs(string path, NTStatus status, int referralCount, int ttlSeconds)
        {
            Path = path;
            Status = status;
            ReferralCount = referralCount;
            TtlSeconds = ttlSeconds;
        }

        /// <summary>
        /// Gets the path for which referral was requested.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the status from the referral response.
        /// </summary>
        public NTStatus Status { get; private set; }

        /// <summary>
        /// Gets the number of referral entries received.
        /// </summary>
        public int ReferralCount { get; private set; }

        /// <summary>
        /// Gets the TTL in seconds from the first referral entry.
        /// </summary>
        public int TtlSeconds { get; private set; }
    }

    /// <summary>
    /// Event args for when DFS path resolution completes.
    /// </summary>
    public class DfsResolutionCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsResolutionCompletedEventArgs"/>.
        /// </summary>
        /// <param name="originalPath">The original UNC path requested.</param>
        /// <param name="resolvedPath">The resolved UNC path.</param>
        /// <param name="status">The final status of resolution.</param>
        /// <param name="wasDfsPath">Whether the path was a DFS path.</param>
        public DfsResolutionCompletedEventArgs(string originalPath, string resolvedPath, NTStatus status, bool wasDfsPath)
        {
            OriginalPath = originalPath;
            ResolvedPath = resolvedPath;
            Status = status;
            WasDfsPath = wasDfsPath;
        }

        /// <summary>
        /// Gets the original UNC path requested.
        /// </summary>
        public string OriginalPath { get; private set; }

        /// <summary>
        /// Gets the resolved UNC path.
        /// </summary>
        public string ResolvedPath { get; private set; }

        /// <summary>
        /// Gets the final status of resolution.
        /// </summary>
        public NTStatus Status { get; private set; }

        /// <summary>
        /// Gets whether the path was a DFS path.
        /// </summary>
        public bool WasDfsPath { get; private set; }
    }
}
