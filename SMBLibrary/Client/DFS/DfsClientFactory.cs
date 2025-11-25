using System;
using SMBLibrary.Client;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Public entry point for composing a DFS-aware ISMBFileStore from an existing ISMBFileStore
    /// and DFS client options, without changing existing SMB2Client / ISMBClient APIs.
    /// </summary>
    public static class DfsClientFactory
    {
        /// <summary>
        /// Create a DFS-aware ISMBFileStore when DFS is enabled via options; otherwise return the
        /// original store unchanged.
        /// </summary>
        /// <param name="innerStore">The underlying file store to wrap.</param>
        /// <param name="dfsHandle">An optional DFS IOCTL handle; may be null depending on transport implementation.</param>
        /// <param name="options">DFS client options controlling whether DFS is enabled.</param>
        public static ISMBFileStore CreateDfsAwareFileStore(ISMBFileStore innerStore, object dfsHandle, DfsClientOptions options)
        {
            if (innerStore == null)
            {
                throw new ArgumentNullException("innerStore");
            }

            // No options or DFS disabled: behave as a simple pass-through.
            if (options == null || !options.Enabled)
            {
                return innerStore;
            }

            return DfsFileStoreFactory.CreateDfsAwareFileStore(innerStore, dfsHandle, options);
        }
    }
}
