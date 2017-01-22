/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SMBLibrary.NetBios;
using SMBLibrary.Services;
using SMBLibrary.SMB1;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server
{
    public partial class SMBServer
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;
        public const string NTLanManagerDialect = "NT LM 0.12";
        public const bool EnableExtendedSecurity = true;

        private ShareCollection m_shares; // e.g. Shared folders
        private INTLMAuthenticationProvider m_users;
        private NamedPipeShare m_services; // Named pipes
        private IPAddress m_serverAddress;
        private SMBTransportType m_transport;
        private bool m_enableSMB1;
        private bool m_enableSMB2;

        private Socket m_listenerSocket;
        private bool m_listening;
        private Guid m_serverGuid;

        public event EventHandler<LogEntry> OnLogEntry;

        public SMBServer(ShareCollection shares, INTLMAuthenticationProvider users, IPAddress serverAddress, SMBTransportType transport) : this(shares, users, serverAddress, transport, true, true)
        {
        }

        public SMBServer(ShareCollection shares, INTLMAuthenticationProvider users, IPAddress serverAddress, SMBTransportType transport, bool enableSMB1, bool enableSMB2)
        {
            m_shares = shares;
            m_users = users;
            m_serverAddress = serverAddress;
            m_serverGuid = Guid.NewGuid();
            m_transport = transport;
            m_enableSMB1 = enableSMB1;
            m_enableSMB2 = enableSMB2;

            m_services = new NamedPipeShare(shares.ListShares());
        }

        public void Start()
        {
            if (!m_listening)
            {
                m_listening = true;

                m_listenerSocket = new Socket(m_serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                int port = (m_transport == SMBTransportType.DirectTCPTransport ? DirectTCPPort : NetBiosOverTCPPort);
                m_listenerSocket.Bind(new IPEndPoint(m_serverAddress, port));
                m_listenerSocket.Listen((int)SocketOptionName.MaxConnections);
                m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
            }
        }

        public void Stop()
        {
            m_listening = false;
            SocketUtils.ReleaseSocket(m_listenerSocket);
        }

        // This method Accepts new connections
        private void ConnectRequestCallback(IAsyncResult ar)
        {
            Socket listenerSocket = (Socket)ar.AsyncState;

            Socket clientSocket;
            try
            {
                clientSocket = listenerSocket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                const int WSAECONNRESET = 10054;
                // Client may have closed the connection before we start to process the connection request.
                // When we get this error, we have to continue to accept other requests.
                // See http://stackoverflow.com/questions/7704417/socket-endaccept-error-10054
                if (ex.ErrorCode == WSAECONNRESET)
                {
                    m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
                }
                Log(Severity.Debug, "Connection request error {0}", ex.ErrorCode);
                return;
            }

            ConnectionState state = new ConnectionState(Log);
            // Disable the Nagle Algorithm for this tcp socket:
            clientSocket.NoDelay = true;
            state.ClientSocket = clientSocket;
            state.ClientEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            state.LogToServer(Severity.Verbose, "New connection request");
            try
            {
                // Direct TCP transport packet is actually an NBT Session Message Packet,
                // So in either case (NetBios over TCP or Direct TCP Transport) we will receive an NBT packet.
                clientSocket.BeginReceive(state.ReceiveBuffer.Buffer, state.ReceiveBuffer.WriteOffset, state.ReceiveBuffer.AvailableLength, 0, ReceiveCallback, state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
            m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectionState state = (ConnectionState)result.AsyncState;
            Socket clientSocket = state.ClientSocket;

            if (!m_listening)
            {
                clientSocket.Close();
                return;
            }

            int numberOfBytesReceived;
            try
            {
                numberOfBytesReceived = clientSocket.EndReceive(result);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            if (numberOfBytesReceived == 0)
            {
                // The other side has closed the connection
                state.LogToServer(Severity.Debug, "The other side closed the connection");
                clientSocket.Close();
                return;
            }

            NBTConnectionReceiveBuffer receiveBuffer = state.ReceiveBuffer;
            receiveBuffer.SetNumberOfBytesReceived(numberOfBytesReceived);
            ProcessConnectionBuffer(ref state);

            if (clientSocket.Connected)
            {
                try
                {
                    clientSocket.BeginReceive(state.ReceiveBuffer.Buffer, state.ReceiveBuffer.WriteOffset, state.ReceiveBuffer.AvailableLength, 0, ReceiveCallback, state);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (SocketException)
                {
                }
            }
        }

        public void ProcessConnectionBuffer(ref ConnectionState state)
        {
            Socket clientSocket = state.ClientSocket;

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
                    state.ClientSocket.Close();
                }

                if (packet != null)
                {
                    ProcessPacket(packet, ref state);
                }
            }
        }

        public void ProcessPacket(SessionPacket packet, ref ConnectionState state)
        {
            if (packet is SessionRequestPacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                PositiveSessionResponsePacket response = new PositiveSessionResponsePacket();
                TrySendPacket(state, response);
            }
            else if (packet is SessionKeepAlivePacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                // [RFC 1001] NetBIOS session keep alives do not require a response from the NetBIOS peer
            }
            else if (packet is SessionMessagePacket)
            {
                // Note: To be compatible with SMB2 specifications, we must accept SMB_COM_NEGOTIATE.
                // We will disconnect the connection if m_enableSMB1 == false and the client does not support SMB2.
                bool acceptSMB1 = (state.ServerDialect == SMBDialect.NotSet || state.ServerDialect == SMBDialect.NTLM012);
                bool acceptSMB2 = (m_enableSMB2 && (state.ServerDialect == SMBDialect.NotSet || state.ServerDialect == SMBDialect.SMB202 || state.ServerDialect == SMBDialect.SMB210));

                if (SMB1Header.IsValidSMB1Header(packet.Trailer))
                {
                    if (!acceptSMB1)
                    {
                        state.LogToServer(Severity.Verbose, "Rejected SMB1 message");
                        state.ClientSocket.Close();
                        return;
                    }

                    SMB1Message message = null;
                    try
                    {
                        message = SMB1Message.GetSMB1Message(packet.Trailer);
                    }
                    catch (Exception ex)
                    {
                        state.LogToServer(Severity.Warning, "Invalid SMB1 message: " + ex.Message);
                        state.ClientSocket.Close();
                        return;
                    }
                    state.LogToServer(Severity.Verbose, "SMB1 message received: {0} requests, First request: {1}, Packet length: {2}", message.Commands.Count, message.Commands[0].CommandName.ToString(), packet.Length);
                    if (state.ServerDialect == SMBDialect.NotSet && m_enableSMB2)
                    {
                        // Check if the client supports SMB 2
                        List<string> smb2Dialects = SMB2.NegotiateHelper.FindSMB2Dialects(message);
                        if (smb2Dialects.Count > 0)
                        {
                            SMB2Command response = SMB2.NegotiateHelper.GetNegotiateResponse(smb2Dialects, state, m_serverGuid);
                            if (state.ServerDialect != SMBDialect.NotSet)
                            {
                                state = new SMB2ConnectionState(state, AllocatePersistentFileID);
                            }
                            TrySendResponse(state, response);
                            return;
                        }
                    }

                    if (m_enableSMB1)
                    {
                        ProcessSMB1Message(message, ref state);
                    }
                    else
                    {
                        // [MS-SMB2] 3.3.5.3.2 If the string is not present in the dialect list and the server does not implement SMB,
                        // the server MUST disconnect the connection [..] without sending a response.
                        state.LogToServer(Severity.Verbose, "Rejected SMB1 message");
                        state.ClientSocket.Close();
                    }
                }
                else if (SMB2Header.IsValidSMB2Header(packet.Trailer))
                {
                    if (!acceptSMB2)
                    {
                        state.LogToServer(Severity.Verbose, "Rejected SMB2 message");
                        state.ClientSocket.Close();
                        return;
                    }

                    List<SMB2Command> requestChain;
                    try
                    {
                        requestChain = SMB2Command.ReadRequestChain(packet.Trailer, 0);
                    }
                    catch (Exception ex)
                    {
                        state.LogToServer(Severity.Warning, "Invalid SMB2 request chain: " + ex.Message);
                        state.ClientSocket.Close();
                        return;
                    }
                    state.LogToServer(Severity.Verbose, "SMB2 request chain received: {0} requests, First request: {1}, Packet length: {2}", requestChain.Count, requestChain[0].CommandName.ToString(), packet.Length);
                    List<SMB2Command> responseChain = new List<SMB2Command>();
                    foreach (SMB2Command request in requestChain)
                    {
                        SMB2Command response = ProcessSMB2Command(request, ref state);
                        if (response != null)
                        {
                            UpdateSMB2Header(response, request);
                            responseChain.Add(response);
                        }
                    }
                    if (responseChain.Count > 0)
                    {
                        TrySendResponseChain(state, responseChain);
                    }
                }
                else
                {
                    state.LogToServer(Severity.Warning, "Invalid SMB message");
                    state.ClientSocket.Close();
                }
            }
            else
            {
                state.LogToServer(Severity.Warning, "Invalid NetBIOS packet");
                state.ClientSocket.Close();
                return;
            }
        }

        public static void TrySendPacket(ConnectionState state, SessionPacket response)
        {
            Socket clientSocket = state.ClientSocket;
            try
            {
                clientSocket.Send(response.GetBytes());
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Log(Severity severity, string message)
        {
            // To be thread-safe we must capture the delegate reference first
            EventHandler<LogEntry> handler = OnLogEntry;
            if (handler != null)
            {
                handler(this, new LogEntry(DateTime.Now, severity, "SMB Server", message));
            }
        }

        public void Log(Severity severity, string message, params object[] args)
        {
            Log(severity, String.Format(message, args));
        }
    }
}
