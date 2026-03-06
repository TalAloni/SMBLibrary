using System;
using System.Collections.Generic;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Delegate for creating ISMBClient instances for a given server name.
    /// </summary>
    /// <param name="serverName">The server name to create a client for.</param>
    /// <returns>An ISMBClient instance.</returns>
    public delegate ISMBClient SmbClientFactory(string serverName);

    /// <summary>
    /// Manages SMB sessions across multiple servers for DFS interlink scenarios.
    /// When a DFS referral points to a different server, this manager creates
    /// or reuses existing connections to that target server.
    /// </summary>
    public class DfsSessionManager : IDisposable
    {
        private readonly SmbClientFactory _clientFactory;
        private readonly Dictionary<string, ISMBClient> _clientsByServer;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="DfsSessionManager"/> using the default SMB2Client factory.
        /// </summary>
        public DfsSessionManager()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsSessionManager"/> with a custom client factory.
        /// </summary>
        /// <param name="clientFactory">Factory function that creates an ISMBClient for a given server name.
        /// If null, a default factory creating SMB2Client instances is used.</param>
        public DfsSessionManager(SmbClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? DefaultClientFactory;
            _clientsByServer = new Dictionary<string, ISMBClient>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or creates an ISMBFileStore for the specified server and share.
        /// Reuses existing connections to the same server.
        /// </summary>
        /// <param name="serverName">The target server name.</param>
        /// <param name="shareName">The share name to connect to.</param>
        /// <param name="credentials">Credentials for authentication.</param>
        /// <param name="status">The result status of the operation.</param>
        /// <returns>An ISMBFileStore for the target share, or null if connection/login failed.</returns>
        public ISMBFileStore GetOrCreateSession(string serverName, string shareName, DfsCredentials credentials, out NTStatus status)
        {
            if (serverName == null)
            {
                throw new ArgumentNullException("serverName");
            }

            if (shareName == null)
            {
                throw new ArgumentNullException("shareName");
            }

            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("DfsSessionManager");
                }

                ISMBClient client;
                if (!_clientsByServer.TryGetValue(serverName, out client))
                {
                    // Create new client
                    client = _clientFactory(serverName);
                    if (!client.Connect(serverName, SMBTransportType.DirectTCPTransport))
                    {
                        status = NTStatus.STATUS_BAD_NETWORK_NAME;
                        return null;
                    }

                    NTStatus loginStatus = client.Login(credentials.DomainName, credentials.UserName, credentials.Password);
                    if (loginStatus != NTStatus.STATUS_SUCCESS)
                    {
                        client.Disconnect();
                        status = loginStatus;
                        return null;
                    }

                    _clientsByServer[serverName] = client;
                }

                // TreeConnect to the share
                NTStatus treeConnectStatus;
                ISMBFileStore store = client.TreeConnect(shareName, out treeConnectStatus);
                status = treeConnectStatus;
                return store;
            }
        }

        /// <summary>
        /// Disposes all managed client connections.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_lock)
                {
                    foreach (ISMBClient client in _clientsByServer.Values)
                    {
                        try
                        {
                            client.Disconnect();
                        }
                        catch
                        {
                            // Ignore disconnect errors during dispose
                        }
                    }

                    _clientsByServer.Clear();
                    _disposed = true;
                }
            }
        }

        private static ISMBClient DefaultClientFactory(string serverName)
        {
            return new SMB2Client();
        }
    }
}
