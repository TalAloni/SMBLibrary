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
using SMBLibrary.NetBios;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Client
{
    public class SMB1Client
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;
        public const string NTLanManagerDialect = "NT LM 0.12";

        private SMBTransportType m_transport;
        private bool m_isConnected;
        private Socket m_clientSocket;
        private IAsyncResult m_currentAsyncResult;

        private object m_incomingQueueLock = new object();
        private List<SMB1Message> m_incomingQueue = new List<SMB1Message>();
        private EventWaitHandle m_incomingQueueEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public SMB1Client()
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
                m_currentAsyncResult = m_clientSocket.BeginReceive(buffer.Buffer, buffer.WriteOffset, buffer.AvailableLength, SocketFlags.None, new AsyncCallback(OnServerSocketReceive), state);
                bool supportsCIFS = NegotiateNTLanManagerDialect();
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

        private bool NegotiateNTLanManagerDialect()
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
            TrySendMessage(m_clientSocket, request);
            SMB1Message reply = WaitForMessage(CommandName.SMB_COM_NEGOTIATE);
            if (reply == null)
            {
                return false;
            }

            if (reply.Commands[0] is NegotiateResponse)
            {
                NegotiateResponse response = (NegotiateResponse)reply.Commands[0];
                return true;
            }
            else if (reply.Commands[0] is NegotiateResponseExtended)
            {
                NegotiateResponseExtended response = (NegotiateResponseExtended)reply.Commands[0];
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnServerSocketReceive(IAsyncResult ar)
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
                    m_currentAsyncResult = m_clientSocket.BeginReceive(buffer.Buffer, buffer.WriteOffset, buffer.AvailableLength, SocketFlags.None, new AsyncCallback(OnServerSocketReceive), state);
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

        public SMB1Message WaitForMessage(CommandName commandName)
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

        public void Log(string message)
        {
            System.Diagnostics.Debug.Print(message);
        }

        public static void TrySendMessage(Socket serverSocket, SMB1Command request)
        {
            SMB1Message message = new SMB1Message();
            message.Commands.Add(request);
            TrySendMessage(serverSocket, message);
        }

        public static void TrySendMessage(Socket serverSocket, SMB1Message message)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = message.GetBytes();
            TrySendPacket(serverSocket, packet);
        }

        public static void TrySendPacket(Socket serverSocket, SessionPacket response)
        {
            try
            {
                serverSocket.Send(response.GetBytes());
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
