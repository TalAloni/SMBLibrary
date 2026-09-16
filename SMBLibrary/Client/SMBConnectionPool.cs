/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
// Added for EhPFileBridge - not upstream yet.
using System;
using System.Collections.Generic;
using SMBLibrary.Client.NetworkInterface;
using SMBLibrary.NetworkInterface;

namespace SMBLibrary.Client
{
    /// <summary>
    /// Holds several independent, already logged-in <see cref="ISMBClient"/> connections to the
    /// same server/credentials and lets callers run operations across them concurrently - e.g. one
    /// connection browsing a share while another copies data on the same target at the same time.
    ///
    /// This is NOT the SMB 3.x "Multichannel" protocol feature (multiple TCP connections bound to a
    /// single session), which only exists for dialect 3.0+ and requires both sides to advertise
    /// SMB2_GLOBAL_CAP_MULTI_CHANNEL - see <see cref="SMB2Client.ServerSupportsMultiChannel"/>.
    /// This pool instead opens several ordinary, independent sessions and works the same way for
    /// SMB1, SMB2 and SMB3 alike, so parallel actions are possible regardless of dialect.
    ///
    /// The number of connections opened is not guessed: it is derived from what the server itself
    /// advertises as its concurrent-connection limit for the negotiated dialect -
    /// <see cref="SMB1Client.MaxNumberOfVirtualCircuits"/> for SMB1, or the number of usable network
    /// interfaces returned by FSCTL_QUERY_NETWORK_INTERFACE_INFO for SMB2/3 (see
    /// <see cref="NetworkInterfaceInfoHelper"/>). If the server does not advertise a limit (e.g. a
    /// SMB2.0/2.1 server that does not support that FSCTL), the pool falls back to a single
    /// connection - i.e. today's behavior - instead of picking an arbitrary connection count.
    /// </summary>
    public class SMBConnectionPool : IDisposable
    {
        /// <summary>
        /// Creates a new, not-yet-connected <see cref="ISMBClient"/> instance of the desired SMB
        /// version (e.g. <c>() => new SMB2Client()</c> or <c>() => new SMB1Client()</c>).
        /// </summary>
        public delegate ISMBClient ClientFactory();

        // Upper bound on how many connections the pool will ever open, regardless of what the
        // server reports - a defensive cap against a server misreporting an unreasonably large
        // limit, not a value callers are expected to tune.
        private const int AbsoluteMaxConnections = 32;

        private readonly ClientFactory m_clientFactory;
        private readonly string m_serverName;
        private readonly SMBTransportType m_transport;
        private readonly string m_domainName;
        private readonly string m_userName;
        private readonly string m_password;

        private readonly object m_lock = new object();
        private readonly List<ISMBClient> m_clients = new List<ISMBClient>();
        private int m_nextClientIndex;

        public SMBConnectionPool(ClientFactory clientFactory, string serverName, SMBTransportType transport, string domainName, string userName, string password)
        {
            m_clientFactory = clientFactory;
            m_serverName = serverName;
            m_transport = transport;
            m_domainName = domainName;
            m_userName = userName;
            m_password = password;
        }

        /// <summary>
        /// Number of independent connections currently held by the pool (1 once <see cref="Connect"/>
        /// has completed successfully, more if the server advertised a higher connection limit).
        /// </summary>
        public int ConnectionCount
        {
            get
            {
                lock (m_lock)
                {
                    return m_clients.Count;
                }
            }
        }

        /// <summary>
        /// Establishes the primary connection, determines how many parallel connections the server
        /// allows (see class remarks), and opens the remaining ones. Returns the status of the
        /// primary connection's login; the pool is usable (with at least one connection) whenever
        /// this returns <see cref="NTStatus.STATUS_SUCCESS"/>, even if some of the additional,
        /// non-essential connections failed to open.
        /// </summary>
        public NTStatus Connect()
        {
            ISMBClient primaryClient = m_clientFactory();
            NTStatus status = ConnectAndLogin(primaryClient);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }

            lock (m_lock)
            {
                m_clients.Add(primaryClient);
            }

            int maxConnections = DetermineMaxConnections(primaryClient);
            for (int i = 1; i < maxConnections; i++)
            {
                ISMBClient additionalClient = m_clientFactory();
                if (ConnectAndLogin(additionalClient) == NTStatus.STATUS_SUCCESS)
                {
                    lock (m_lock)
                    {
                        m_clients.Add(additionalClient);
                    }
                }
                else
                {
                    // The server advertised a limit but an additional connection still failed
                    // (e.g. transient failure, or the advertised limit was optimistic) - keep what
                    // we already have instead of failing the whole pool.
                    additionalClient.Disconnect();
                    break;
                }
            }

            return NTStatus.STATUS_SUCCESS;
        }

        private NTStatus ConnectAndLogin(ISMBClient client)
        {
            if (!client.Connect(m_serverName, m_transport))
            {
                return NTStatus.STATUS_BAD_NETWORK_NAME;
            }
            return client.Login(m_domainName, m_userName, m_password);
        }

        /// <summary>
        /// Reads the server-advertised concurrent-connection limit (see class remarks) off the
        /// already connected/logged-in primary client. Falls back to 1 (no additional connections)
        /// whenever the dialect/server does not expose such a limit.
        /// </summary>
        private int DetermineMaxConnections(ISMBClient primaryClient)
        {
            if (primaryClient is SMB1Client smb1Client)
            {
                int serverLimit = smb1Client.MaxNumberOfVirtualCircuits;
                return Clamp(serverLimit);
            }

            if (primaryClient is SMB2Client smb2Client && smb2Client.ServerSupportsMultiChannel)
            {
                ISMBFileStore namedPipeShare = smb2Client.TreeConnect("IPC$", out NTStatus status);
                if (namedPipeShare != null)
                {
                    NTStatus queryStatus = NetworkInterfaceInfoHelper.QueryNetworkInterfaceInfo(namedPipeShare, out QueryNetworkInterfaceInfoResponse response);
                    namedPipeShare.Disconnect();
                    if (queryStatus == NTStatus.STATUS_SUCCESS && response != null)
                    {
                        return Clamp(response.Entries.Count);
                    }
                }
            }

            return 1;
        }

        private static int Clamp(int serverAdvertisedLimit)
        {
            if (serverAdvertisedLimit <= 0)
            {
                return 1;
            }
            return Math.Min(serverAdvertisedLimit, AbsoluteMaxConnections);
        }

        /// <summary>
        /// Returns the next connection to use for an operation, round-robin across all open
        /// connections. Browsing and data-transfer operations are treated identically - there is no
        /// dedicated "browse connection" - so simultaneous browse + copy naturally end up on
        /// different connections whenever more than one is available.
        /// </summary>
        public ISMBClient AcquireClient()
        {
            lock (m_lock)
            {
                if (m_clients.Count == 0)
                {
                    throw new InvalidOperationException("Connect() must succeed before AcquireClient() can be used");
                }
                ISMBClient client = m_clients[m_nextClientIndex % m_clients.Count];
                m_nextClientIndex = (m_nextClientIndex + 1) % m_clients.Count;
                return client;
            }
        }

        public void Disconnect()
        {
            lock (m_lock)
            {
                foreach (ISMBClient client in m_clients)
                {
                    try
                    {
                        client.Logoff();
                    }
                    catch
                    {
                        // best-effort logoff, the connection is going away regardless
                    }
                    client.Disconnect();
                }
                m_clients.Clear();
                m_nextClientIndex = 0;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
