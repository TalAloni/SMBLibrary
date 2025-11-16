using System;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Default DFS client resolver implementation for vNext-DFS.
    /// For now, it only handles the DFS-disabled case and returns NotApplicable.
    /// </summary>
    public class DfsClientResolver : IDfsClientResolver
    {
        public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            // DFS disabled: do not attempt resolution; return original path as-is.
            if (!options.Enabled)
            {
                DfsResolutionResult result = new DfsResolutionResult();
                result.Status = DfsResolutionStatus.NotApplicable;
                result.ResolvedPath = originalPath;
                return result;
            }

            // DFS-enabled behavior will be implemented in later phases.
            DfsResolutionResult notImplemented = new DfsResolutionResult();
            notImplemented.Status = DfsResolutionStatus.Error;
            notImplemented.ResolvedPath = originalPath;
            return notImplemented;
        }
    }
}
