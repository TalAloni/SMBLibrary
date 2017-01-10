/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SMBLibrary.NetBios;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Client
{
    public class SMBClient
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;
        public const string NTLanManagerDialect = "NT LM 0.12";

        public SMBClient(IPAddress serverAddress, SMBTransportType transport)
        {
            NegotiateRequest request = new NegotiateRequest();
            request.Dialects.Add(NTLanManagerDialect);

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (transport == SMBTransportType.DirectTCPTransport)
            {
                serverSocket.Connect(serverAddress, DirectTCPPort);
            }
            else
            {
                serverSocket.Connect(serverAddress, NetBiosOverTCPPort);
            }
            TrySendMessage(serverSocket, request);
        }

        public static void TrySendMessage(Socket serverSocket, SMB1Command request)
        {
            SMBMessage message = new SMBMessage();
            message.Commands.Add(request);
            TrySendMessage(serverSocket, message);
        }

        public static void TrySendMessage(Socket serverSocket, SMBMessage message)
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
