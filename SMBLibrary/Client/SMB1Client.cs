/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Client
{
    public class SMB1Client
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;
        public const string NTLanManagerDialect = "NT LM 0.12";
        
        public const int MaxBufferSize = 65535; // Valid range: 512 - 65535
        public const int MaxMpxCount = 1;

        private SMBTransportType m_transport;
        private bool m_isConnected;
        private bool m_isLoggedIn;
        private Socket m_clientSocket;
        private IAsyncResult m_currentAsyncResult;
        private bool m_forceExtendedSecurity;

        private object m_incomingQueueLock = new object();
        private List<SMB1Message> m_incomingQueue = new List<SMB1Message>();
        private EventWaitHandle m_incomingQueueEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private ushort m_userID;
        private byte[] m_serverChallenge;
        private byte[] m_securityBlob;
        private byte[] m_sessionKey;

        public SMB1Client()
        {
        }

        public bool Connect(IPAddress serverAddress, SMBTransportType transport)
        {
            return Connect(serverAddress, transport, true);
        }

        public bool Connect(IPAddress serverAddress, SMBTransportType transport, bool forceExtendedSecurity)
        {
            m_transport = transport;
            if (!m_isConnected)
            {
                m_forceExtendedSecurity = forceExtendedSecurity;
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
                bool supportsCIFS = NegotiateNTLanManagerDialect(m_forceExtendedSecurity);
                if (!supportsCIFS)
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

        private bool NegotiateNTLanManagerDialect(bool forceExtendedSecurity)
        {
            if (m_transport == SMBTransportType.NetBiosOverTCP)
            {
                SessionRequestPacket sessionRequest = new SessionRequestPacket();
                sessionRequest.CalledName = NetBiosUtils.GetMSNetBiosName("*SMBSERVER", NetBiosSuffix.FileServiceService); ;
                sessionRequest.CallingName = NetBiosUtils.GetMSNetBiosName(Environment.MachineName, NetBiosSuffix.WorkstationService);
                TrySendPacket(m_clientSocket, sessionRequest);
            }
            NegotiateRequest request = new NegotiateRequest();
            request.Dialects.Add(NTLanManagerDialect);

            TrySendMessage(request);
            SMB1Message reply = WaitForMessage(CommandName.SMB_COM_NEGOTIATE);
            if (reply == null)
            {
                return false;
            }

            if (reply.Commands[0] is NegotiateResponse && !forceExtendedSecurity)
            {
                NegotiateResponse response = (NegotiateResponse)reply.Commands[0];
                m_serverChallenge = response.Challenge;
                return true;
            }
            else if (reply.Commands[0] is NegotiateResponseExtended)
            {
                NegotiateResponseExtended response = (NegotiateResponseExtended)reply.Commands[0];
                m_securityBlob = response.SecurityBlob;
                return true;
            }
            else
            {
                return false;
            }
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

            if (m_serverChallenge != null)
            {
                SessionSetupAndXRequest request = new SessionSetupAndXRequest();
                request.MaxBufferSize = MaxBufferSize;
                request.MaxMpxCount = MaxMpxCount;
                request.Capabilities = Capabilities.Unicode | Capabilities.NTStatusCode;
                request.AccountName = userName;
                request.PrimaryDomain = domainName;
                byte[] clientChallenge = new byte[8];
                new Random().NextBytes(clientChallenge);
                if (authenticationMethod == AuthenticationMethod.NTLMv1)
                {
                    request.OEMPassword = NTLMCryptography.ComputeLMv1Response(m_serverChallenge, password);
                    request.UnicodePassword = NTLMCryptography.ComputeNTLMv1Response(m_serverChallenge, password);
                }
                else if (authenticationMethod == AuthenticationMethod.NTLMv1ExtendedSessionSecurity)
                {
                    // [MS-CIFS] CIFS does not support Extended Session Security because there is no mechanism in CIFS to negotiate Extended Session Security
                    throw new ArgumentException("SMB Extended Security must be negotiated in order for NTLMv1 Extended Session Security to be used");
                }
                else // NTLMv2
                {
                    // Note: NTLMv2 over non-extended security session setup is not supported under Windows Vista and later which will return STATUS_INVALID_PARAMETER.
                    // https://msdn.microsoft.com/en-us/library/ee441701.aspx
                    // https://msdn.microsoft.com/en-us/library/cc236700.aspx
                    request.OEMPassword = NTLMCryptography.ComputeLMv2Response(m_serverChallenge, clientChallenge, password, userName, domainName);
                    NTLMv2ClientChallenge clientChallengeStructure = new NTLMv2ClientChallenge(DateTime.UtcNow, clientChallenge, AVPairUtils.GetAVPairSequence(domainName, Environment.MachineName));
                    byte[] temp = clientChallengeStructure.GetBytesPadded();
                    byte[] proofStr = NTLMCryptography.ComputeNTLMv2Proof(m_serverChallenge, temp, password, userName, domainName);
                    request.UnicodePassword = ByteUtils.Concatenate(proofStr, temp);
                }
                
                TrySendMessage(request);

                SMB1Message reply = WaitForMessage(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                if (reply != null)
                {
                    m_isLoggedIn = (reply.Header.Status == NTStatus.STATUS_SUCCESS);
                    return reply.Header.Status;
                }
                return NTStatus.STATUS_INVALID_SMB;
            }
            else // m_securityBlob != null
            {
                SessionSetupAndXRequestExtended request = new SessionSetupAndXRequestExtended();
                request.MaxBufferSize = MaxBufferSize;
                request.MaxMpxCount = MaxMpxCount;
                request.Capabilities = Capabilities.Unicode | Capabilities.NTStatusCode | Capabilities.LargeRead | Capabilities.LargeWrite;
                request.SecurityBlob = NTLMAuthenticationHelper.GetNegotiateMessage(m_securityBlob, domainName, authenticationMethod);
                TrySendMessage(request);
                
                SMB1Message reply = WaitForMessage(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                if (reply != null)
                {
                    if (reply.Header.Status == NTStatus.STATUS_MORE_PROCESSING_REQUIRED && reply.Commands[0] is SessionSetupAndXResponseExtended)
                    {
                        SessionSetupAndXResponseExtended response = (SessionSetupAndXResponseExtended)reply.Commands[0];
                        m_userID = reply.Header.UID;
                        request = new SessionSetupAndXRequestExtended();
                        request.MaxBufferSize = MaxBufferSize;
                        request.MaxMpxCount = MaxMpxCount;
                        request.Capabilities = Capabilities.Unicode | Capabilities.NTStatusCode | Capabilities.LargeRead | Capabilities.LargeWrite | Capabilities.ExtendedSecurity;

                        request.SecurityBlob = NTLMAuthenticationHelper.GetAuthenticateMessage(response.SecurityBlob, domainName, userName, password, authenticationMethod, out m_sessionKey);
                        TrySendMessage(request);

                        reply = WaitForMessage(CommandName.SMB_COM_SESSION_SETUP_ANDX);
                        if (reply != null)
                        {
                            m_isLoggedIn = (reply.Header.Status == NTStatus.STATUS_SUCCESS);
                            return reply.Header.Status;
                        }
                    }
                    else
                    {
                        return reply.Header.Status;
                    }
                }
                return NTStatus.STATUS_INVALID_SMB;
            }
        }

        public NTStatus Logoff()
        {
            LogoffAndXRequest request = new LogoffAndXRequest();
            TrySendMessage(request);

            SMB1Message reply = WaitForMessage(CommandName.SMB_COM_LOGOFF_ANDX);
            if (reply != null)
            {
                m_isLoggedIn = (reply.Header.Status != NTStatus.STATUS_SUCCESS);
                return reply.Header.Status;
            }
            return NTStatus.STATUS_INVALID_SMB;
        }

        public List<string> ListShares(out NTStatus status)
        {
            if (!m_isConnected || !m_isLoggedIn)
            {
                throw new InvalidOperationException("A login session must be successfully established before retrieving share list");
            }

            SMB1FileStore namedPipeShare = TreeConnect("IPC$", ServiceName.NamedPipe, out status);
            if (namedPipeShare == null)
            {
                return null;
            }

            List<string> shares = ServerServiceHelper.ListShares(namedPipeShare, ShareType.DiskDrive, out status);
            namedPipeShare.Disconnect();
            return shares;
        }

        public SMB1FileStore TreeConnect(string shareName, ServiceName serviceName, out NTStatus status)
        {
            if (!m_isConnected || !m_isLoggedIn)
            {
                throw new InvalidOperationException("A login session must be successfully established before connecting to a share");
            }

            TreeConnectAndXRequest request = new TreeConnectAndXRequest();
            request.Path = shareName;
            request.Service = serviceName;
            TrySendMessage(request);
            SMB1Message reply = WaitForMessage(CommandName.SMB_COM_TREE_CONNECT_ANDX);
            if (reply != null)
            {
                status = reply.Header.Status;
                if (reply.Header.Status == NTStatus.STATUS_SUCCESS && reply.Commands[0] is TreeConnectAndXResponse)
                {
                    TreeConnectAndXResponse response = (TreeConnectAndXResponse)reply.Commands[0];
                    return new SMB1FileStore(this, reply.Header.TID);
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
                SMB1Message message;
                try
                {
                    message = SMB1Message.GetSMB1Message(packet.Trailer);
                }
                catch (Exception ex)
                {
                    Log("Invalid SMB1 message: " + ex.Message);
                    m_clientSocket.Close();
                    m_isConnected = false;
                    return;
                }

                lock (m_incomingQueueLock)
                {
                    m_incomingQueue.Add(message);
                    m_incomingQueueEventHandle.Set();
                }
            }
        }

        internal SMB1Message WaitForMessage(CommandName commandName)
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
                        SMB1Message message = m_incomingQueue[index];

                        if (message.Commands[0].CommandName == commandName)
                        {
                            m_incomingQueue.RemoveAt(index);
                            return message;
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

        internal void TrySendMessage(SMB1Command request)
        {
            TrySendMessage(request, 0);
        }

        internal void TrySendMessage(SMB1Command request, ushort treeID)
        {
            SMB1Message message = new SMB1Message();
            message.Header.UnicodeFlag = true;
            message.Header.ExtendedSecurityFlag = m_forceExtendedSecurity;
            message.Header.Flags2 |= HeaderFlags2.LongNamesAllowed | HeaderFlags2.LongNameUsed | HeaderFlags2.NTStatusCode;
            message.Header.UID = m_userID;
            message.Header.TID = treeID;
            message.Commands.Add(request);
            TrySendMessage(m_clientSocket, message);
        }

        public static void TrySendMessage(Socket socket, SMB1Message message)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = message.GetBytes();
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
