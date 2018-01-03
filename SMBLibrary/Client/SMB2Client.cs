/* Copyright (C) 2017-2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.NetBios;
using SMBLibrary.Services;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Client
{
    public class SMB2Client : ISMBClient
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;

        public const uint ClientMaxTransactSize = 65536;
        public const uint ClientMaxReadSize = 65536;
        public const uint ClientMaxWriteSize = 65536;

        private SMBTransportType m_transport;
        private bool m_isConnected;
        private bool m_isLoggedIn;
        private Socket m_clientSocket;
        private IAsyncResult m_currentAsyncResult;

        private object m_incomingQueueLock = new object();
        private List<SMB2Command> m_incomingQueue = new List<SMB2Command>();
        private EventWaitHandle m_incomingQueueEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private uint m_messageID = 0;
        private SMB2Dialect m_dialect;
        private uint m_maxTransactSize;
        private uint m_maxReadSize;
        private uint m_maxWriteSize;
        private ulong m_sessionID;
        private byte[] m_securityBlob;
        private byte[] m_sessionKey;

        public SMB2Client()
        {
        }

        public bool Connect(IPAddress serverAddress, SMBTransportType transport)
        {
            m_transport = transport;
            if (!m_isConnected)
            {
                m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                int port;
                if (transport == SMBTransportType.DirectTCPTransport)
                {
                    port = DirectTCPPort;
                }
                else
                {
                    port = NetBiosOverTCPPort;
                }

                try
                {
                    m_clientSocket.Connect(serverAddress, port);
                }
                catch (SocketException)
                {
                    return false;
                }

                ConnectionState state = new ConnectionState();
                NBTConnectionReceiveBuffer buffer = state.ReceiveBuffer;
                m_currentAsyncResult = m_clientSocket.BeginReceive(buffer.Buffer, buffer.WriteOffset, buffer.AvailableLength, SocketFlags.None, new AsyncCallback(OnClientSocketReceive), state);
                bool supportsDialect = NegotiateDialect();
                if (!supportsDialect)
                {
                    m_clientSocket.Close();
                }
                else
                {
                    m_isConnected = true;
                }
            }
            return m_isConnected;
        }

        public void Disconnect()
        {
            if (m_isConnected)
            {
                m_clientSocket.Disconnect(false);
                m_isConnected = false;
            }
        }

        private bool NegotiateDialect()
        {
            NegotiateRequest request = new NegotiateRequest();
            request.SecurityMode = SecurityMode.SigningEnabled;
            request.ClientGuid = Guid.NewGuid();
            request.ClientStartTime = DateTime.Now;
            request.Dialects.Add(SMB2Dialect.SMB202);
            request.Dialects.Add(SMB2Dialect.SMB210);

            TrySendCommand(request);
            NegotiateResponse response = WaitForCommand(SMB2CommandName.Negotiate) as NegotiateResponse;
            if (response != null && response.Header.Status == NTStatus.STATUS_SUCCESS)
            {
                m_dialect = response.DialectRevision;
                m_maxTransactSize = Math.Min(response.MaxTransactSize, ClientMaxTransactSize);
                m_maxReadSize = Math.Min(response.MaxReadSize, ClientMaxReadSize);
                m_maxWriteSize = Math.Min(response.MaxWriteSize, ClientMaxWriteSize);
                m_securityBlob = response.SecurityBuffer;
                return true;
            }
            return false;
        }

        public NTStatus Login(string domainName, string userName, string password)
        {
            return Login(domainName, userName, password, AuthenticationMethod.NTLMv2);
        }

        public NTStatus Login(string domainName, string userName, string password, AuthenticationMethod authenticationMethod)
        {
            if (!m_isConnected)
            {
                throw new InvalidOperationException("A connection must be successfully established before attempting login");
            }

            byte[] negotiateMessage = NTLMAuthenticationHelper.GetNegotiateMessage(m_securityBlob, domainName, authenticationMethod);
            if (negotiateMessage == null)
            {
                return NTStatus.SEC_E_INVALID_TOKEN;
            }

            SessionSetupRequest request = new SessionSetupRequest();
            request.SecurityMode = SecurityMode.SigningEnabled;
            request.SecurityBuffer = negotiateMessage;
            TrySendCommand(request);
            SMB2Command response = WaitForCommand(SMB2CommandName.SessionSetup);
            if (response != null)
            {
                if (response.Header.Status == NTStatus.STATUS_MORE_PROCESSING_REQUIRED && response is SessionSetupResponse)
                {
                    byte[] authenticateMessage = NTLMAuthenticationHelper.GetAuthenticateMessage(((SessionSetupResponse)response).SecurityBuffer, domainName, userName, password, authenticationMethod, out m_sessionKey);
                    if (authenticateMessage == null)
                    {
                        return NTStatus.SEC_E_INVALID_TOKEN;
                    }

                    m_sessionID = response.Header.SessionID;
                    request = new SessionSetupRequest();
                    request.SecurityMode = SecurityMode.SigningEnabled;
                    request.SecurityBuffer = authenticateMessage;
                    TrySendCommand(request);
                    response = WaitForCommand(SMB2CommandName.SessionSetup);
                    if (response != null)
                    {
                        m_isLoggedIn = (response.Header.Status == NTStatus.STATUS_SUCCESS);
                        return response.Header.Status;
                    }
                }
                else
                {
                    return response.Header.Status;
                }
            }
            return NTStatus.STATUS_INVALID_SMB;
        }

        public NTStatus Logoff()
        {
            if (!m_isConnected)
            {
                throw new InvalidOperationException("A login session must be successfully established before attempting logoff");
            }

            LogoffRequest request = new LogoffRequest();
            TrySendCommand(request);

            SMB2Command response = WaitForCommand(SMB2CommandName.Logoff);
            if (response != null)
            {
                m_isLoggedIn = (response.Header.Status != NTStatus.STATUS_SUCCESS);
                return response.Header.Status;
            }
            return NTStatus.STATUS_INVALID_SMB;
        }

        public List<string> ListShares(out NTStatus status)
        {
            if (!m_isConnected || !m_isLoggedIn)
            {
                throw new InvalidOperationException("A login session must be successfully established before retrieving share list");
            }

            ISMBFileStore namedPipeShare = TreeConnect("IPC$", out status);
            if (namedPipeShare == null)
            {
                return null;
            }

            List<string> shares = ServerServiceHelper.ListShares(namedPipeShare, SMBLibrary.Services.ShareType.DiskDrive, out status);
            namedPipeShare.Disconnect();
            return shares;
        }

        public ISMBFileStore TreeConnect(string shareName, out NTStatus status)
        {
            if (!m_isConnected || !m_isLoggedIn)
            {
                throw new InvalidOperationException("A login session must be successfully established before connecting to a share");
            }

            IPAddress serverIPAddress = ((IPEndPoint)m_clientSocket.RemoteEndPoint).Address;
            string sharePath = String.Format(@"\\{0}\{1}", serverIPAddress.ToString(), shareName);
            TreeConnectRequest request = new TreeConnectRequest();
            request.Path = sharePath;
            TrySendCommand(request);
            SMB2Command response = WaitForCommand(SMB2CommandName.TreeConnect);
            if (response != null)
            {
                status = response.Header.Status;
                if (response.Header.Status == NTStatus.STATUS_SUCCESS && response is TreeConnectResponse)
                {
                    return new SMB2FileStore(this, response.Header.TreeID);
                }
            }
            else
            {
                status = NTStatus.STATUS_INVALID_SMB;
            }
            return null;
        }

        private void OnClientSocketReceive(IAsyncResult ar)
        {
            if (ar != m_currentAsyncResult)
            {
                // We ignore calls for old sockets which we no longer use
                // See: http://rajputyh.blogspot.co.il/2010/04/solve-exception-message-iasyncresult.html
                return;
            }

            ConnectionState state = (ConnectionState)ar.AsyncState;

            if (!m_clientSocket.Connected)
            {
                return;
            }

            int numberOfBytesReceived = 0;
            try
            {
                numberOfBytesReceived = m_clientSocket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                Log("[ReceiveCallback] EndReceive ObjectDisposedException");
                return;
            }
            catch (SocketException ex)
            {
                Log("[ReceiveCallback] EndReceive SocketException: " + ex.Message);
                return;
            }

            if (numberOfBytesReceived == 0)
            {
                m_isConnected = false;
            }
            else
            {
                NBTConnectionReceiveBuffer buffer = state.ReceiveBuffer;
                buffer.SetNumberOfBytesReceived(numberOfBytesReceived);
                ProcessConnectionBuffer(state);

                try
                {
                    m_currentAsyncResult = m_clientSocket.BeginReceive(buffer.Buffer, buffer.WriteOffset, buffer.AvailableLength, SocketFlags.None, new AsyncCallback(OnClientSocketReceive), state);
                }
                catch (ObjectDisposedException)
                {
                    m_isConnected = false;
                    Log("[ReceiveCallback] BeginReceive ObjectDisposedException");
                }
                catch (SocketException ex)
                {
                    m_isConnected = false;
                    Log("[ReceiveCallback] BeginReceive SocketException: " + ex.Message);
                }
            }
        }

        private void ProcessConnectionBuffer(ConnectionState state)
        {
            NBTConnectionReceiveBuffer receiveBuffer = state.ReceiveBuffer;
            while (receiveBuffer.HasCompletePacket())
            {
                SessionPacket packet = null;
                try
                {
                    packet = receiveBuffer.DequeuePacket();
                }
                catch (Exception)
                {
                    m_clientSocket.Close();
                    break;
                }

                if (packet != null)
                {
                    ProcessPacket(packet, state);
                }
            }
        }

        private void ProcessPacket(SessionPacket packet, ConnectionState state)
        {
            if (packet is SessionKeepAlivePacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                // [RFC 1001] NetBIOS session keep alives do not require a response from the NetBIOS peer
            }
            else if (packet is PositiveSessionResponsePacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
            }
            else if (packet is NegativeSessionResponsePacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                m_clientSocket.Close();
                m_isConnected = false;
            }
            else if (packet is SessionMessagePacket)
            {
                SMB2Command command;
                try
                {
                    command = SMB2Command.ReadResponse(packet.Trailer, 0);
                }
                catch (Exception ex)
                {
                    Log("Invalid SMB2 response: " + ex.Message);
                    m_clientSocket.Close();
                    m_isConnected = false;
                    return;
                }

                // [MS-SMB2] 3.2.5.1.2 - If the MessageId is 0xFFFFFFFFFFFFFFFF, this is not a reply to a previous request,
                // and the client MUST NOT attempt to locate the request, but instead process it as follows:
                // If the command field in the SMB2 header is SMB2 OPLOCK_BREAK, it MUST be processed as specified in 3.2.5.19.
                // Otherwise, the response MUST be discarded as invalid.
                if (command.Header.MessageID != 0xFFFFFFFFFFFFFFFF || command.Header.Command == SMB2CommandName.OplockBreak)
                {
                    lock (m_incomingQueueLock)
                    {
                        m_incomingQueue.Add(command);
                        m_incomingQueueEventHandle.Set();
                    }
                }
            }
        }

        internal SMB2Command WaitForCommand(SMB2CommandName commandName)
        {
            const int TimeOut = 5000;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < TimeOut)
            {
                lock (m_incomingQueueLock)
                {
                    for (int index = 0; index < m_incomingQueue.Count; index++)
                    {
                        SMB2Command command = m_incomingQueue[index];

                        if (command.CommandName == commandName)
                        {
                            m_incomingQueue.RemoveAt(index);
                            return command;
                        }
                    }
                }
                m_incomingQueueEventHandle.WaitOne(100);
            }
            return null;
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.Print(message);
        }

        internal void TrySendCommand(SMB2Command request)
        {
            request.Header.Credits = 1;
            request.Header.MessageID = m_messageID;
            request.Header.SessionID = m_sessionID;
            TrySendCommand(m_clientSocket, request);
            m_messageID++;
        }

        public uint MaxTransactSize
        {
            get
            {
                return m_maxTransactSize;
            }
        }

        public uint MaxReadSize
        {
            get
            {
                return m_maxReadSize;
            }
        }

        public uint MaxWriteSize
        {
            get
            {
                return m_maxWriteSize;
            }
        }

        public static void TrySendCommand(Socket socket, SMB2Command request)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = request.GetBytes();
            TrySendPacket(socket, packet);
        }

        public static void TrySendPacket(Socket socket, SessionPacket packet)
        {
            try
            {
                socket.Send(packet.GetBytes());
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
