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

        private Socket m_listenerSocket;
        private bool m_listening;
        private Guid m_serverGuid;

        public SMBServer(ShareCollection shares, INTLMAuthenticationProvider users, IPAddress serverAddress, SMBTransportType transport)
        {
            m_shares = shares;
            m_users = users;
            m_serverAddress = serverAddress;
            m_serverGuid = Guid.NewGuid();
            m_transport = transport;

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
            System.Diagnostics.Debug.Print("[{0}] New connection request", DateTime.Now.ToString("HH:mm:ss:ffff"));
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
                System.Diagnostics.Debug.Print("[{0}] Connection request error {1}", DateTime.Now.ToString("HH:mm:ss:ffff"), ex.ErrorCode);
                return;
            }

            SMB1ConnectionState state = new SMB1ConnectionState();
            // Disable the Nagle Algorithm for this tcp socket:
            clientSocket.NoDelay = true;
            state.ClientSocket = clientSocket;
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
            SMB1ConnectionState state = (SMB1ConnectionState)result.AsyncState;
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
                System.Diagnostics.Debug.Print("[{0}] The other side closed the connection", DateTime.Now.ToString("HH:mm:ss:ffff"));
                clientSocket.Close();
                return;
            }

            NBTConnectionReceiveBuffer receiveBuffer = state.ReceiveBuffer;
            receiveBuffer.SetNumberOfBytesReceived(numberOfBytesReceived);
            ProcessConnectionBuffer(state);

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

        public void ProcessConnectionBuffer(SMB1ConnectionState state)
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
                    ProcessPacket(packet, state);
                }
            }
        }

        public void ProcessPacket(SessionPacket packet, SMB1ConnectionState state)
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
                SMB1Message message = null;
#if DEBUG
                message = SMB1Message.GetSMB1Message(packet.Trailer);
                System.Diagnostics.Debug.Print("[{0}] Message Received: {1} Commands, First Command: {2}, Packet length: {3}", DateTime.Now.ToString("HH:mm:ss:ffff"), message.Commands.Count, message.Commands[0].CommandName.ToString(), packet.Length);
#else
                try
                {
                    message = SMB1Message.GetSMB1Message(packet.Trailer);
                }
                catch (Exception)
                {
                    state.ClientSocket.Close();
                    return;
                }
#endif
                ProcessSMB1Message(message, state);
            }
            else
            {
                System.Diagnostics.Debug.Print("[{0}] Invalid NetBIOS packet", DateTime.Now.ToString("HH:mm:ss:ffff"));
                state.ClientSocket.Close();
                return;
            }
        }

        public static void TrySendPacket(SMB1ConnectionState state, SessionPacket response)
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
    }
}
