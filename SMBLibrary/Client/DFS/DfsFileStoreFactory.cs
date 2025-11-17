using System;
using SMBLibrary;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Internal helper for composing a DFS-aware INTFileStore from an existing INTFileStore,
    /// a DFS handle, and DFS client options.
    /// </summary>
    internal static class DfsFileStoreFactory
    {
        internal static INTFileStore CreateDfsAwareFileStore(INTFileStore inner, object dfsHandle, DfsClientOptions options)
        {
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            // If DFS is disabled, return the original store unchanged.
            if (!options.Enabled)
            {
                return inner;
            }

            IDfsReferralTransport transport = Smb2DfsReferralTransport.CreateUsingDeviceIOControl(inner, dfsHandle);
            IDfsClientResolver resolver = new DfsClientResolver(transport);
            return new DfsAwareClientAdapter(inner, resolver, options);
        }
    }
}
