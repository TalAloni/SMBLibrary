using System;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Exception thrown when DFS path resolution fails.
    /// </summary>
    /// <remarks>
    /// This exception is intended for use in scenarios where callers prefer exception-based
    /// error handling over status-code returns. The <see cref="DfsPathResolver"/> currently
    /// uses status-based returns via <see cref="DfsResolutionResult"/>, but this exception
    /// can be thrown by higher-level wrappers or future API extensions.
    /// </remarks>
    public class DfsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DfsException"/> with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DfsException(string message) : base(message)
        {
            Status = NTStatus.STATUS_SUCCESS;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsException"/> with a message and inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DfsException(string message, Exception innerException) : base(message, innerException)
        {
            Status = NTStatus.STATUS_SUCCESS;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsException"/> with a message and status.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="status">The NTStatus code from the failed operation.</param>
        public DfsException(string message, NTStatus status) : base(message)
        {
            Status = status;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsException"/> with a message, status, and path.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="status">The NTStatus code from the failed operation.</param>
        /// <param name="path">The DFS path that failed to resolve.</param>
        public DfsException(string message, NTStatus status, string path) : base(message)
        {
            Status = status;
            Path = path;
        }

        /// <summary>
        /// Gets the NTStatus code from the failed DFS operation.
        /// </summary>
        public NTStatus Status { get; private set; }

        /// <summary>
        /// Gets the DFS path that failed to resolve, if available.
        /// </summary>
        public string Path { get; private set; }
    }
}
