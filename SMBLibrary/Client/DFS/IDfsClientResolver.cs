namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Abstraction for resolving DFS paths on the client side.
    /// </summary>
    public interface IDfsClientResolver
    {
        /// <summary>
        /// Resolve a UNC path according to DFS settings.
        /// For vNext-DFS, if DFS is disabled for the provided options, implementations
        /// should return a result with Status=NotApplicable and the original path.
        /// </summary>
        /// <param name="options">DFS client options for this resolution attempt.</param>
        /// <param name="originalPath">The UNC path requested by the caller.</param>
        /// <returns>A DFS resolution result describing the outcome.</returns>
        DfsResolutionResult Resolve(DfsClientOptions options, string originalPath);
    }
}
