/* Copyright (C) 2024-2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2ClientTests
    {
        private static readonly int s_minPort = 1025;
        private static readonly int s_maxPort = 50000;
        private static int s_nextServerPort = s_minPort + new Random().Next(s_maxPort - s_minPort);

        private int m_serverPort;
        private TcpListener m_tcpListener;
        private bool m_clientConnected;

        [TestInitialize]
        public void Initialize()
        {
            m_serverPort = Interlocked.Increment(ref s_nextServerPort);
            m_tcpListener = new TcpListener(IPAddress.Loopback, m_serverPort);
            m_tcpListener.Start();
        }

        private void AcceptTcpClient_DoNotReply(IAsyncResult ar)
        {
            TcpClient client = m_tcpListener.EndAcceptTcpClient(ar);
            m_clientConnected = true;
        }

        private void AcceptTcpClient_SendNonSmbData(IAsyncResult ar)
        {
            TcpClient client = m_tcpListener.EndAcceptTcpClient(ar);
            m_clientConnected = true;
            byte[] buffer = new byte[4];
            client.Client.Send(buffer);
        }

        [TestMethod]
        public void When_SMB2ClientConnectsAndServerDoesNotReply_ShouldReachTimeout()
        {
            m_tcpListener.BeginAcceptTcpClient(AcceptTcpClient_DoNotReply, null);

            int timeoutInMilliseconds = 1000;
            SMB2Client client = new SMB2Client(timeoutInMilliseconds);

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Stopwatch stopwatch = new Stopwatch();
            bool isConnected = false;
            new Thread(() =>
            {
                stopwatch.Start();
                isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
                stopwatch.Stop();
            }).Start();

            while (!m_clientConnected)
            {
                Thread.Sleep(1);
            }
            Assert.IsFalse(isConnected);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200);
        }

        [TestMethod]
        public void When_SMB2ClientConnectsAndServerSendNonSmbData_ShouldNotReachTimeout()
        {
            m_tcpListener.BeginAcceptTcpClient(AcceptTcpClient_SendNonSmbData, null);
            int timeoutInMilliseconds = 1000;
            SMB2Client client = new SMB2Client(timeoutInMilliseconds);

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Stopwatch stopwatch = new Stopwatch();
            bool isConnected = false;
            new Thread(() =>
            {
                stopwatch.Start();
                isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
                stopwatch.Stop();
            }).Start();

            while (!m_clientConnected)
            {
                Thread.Sleep(1);
            }
            Assert.IsFalse(isConnected);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200);
        }

        // Added for EhPFileBridge - not upstream yet.
        private byte[] m_capturedNegotiateRequestBytes;

        private void AcceptTcpClient_CaptureNegotiateRequest(IAsyncResult ar)
        {
            TcpClient client = m_tcpListener.EndAcceptTcpClient(ar);
            m_clientConnected = true;

            // NetBIOS session message: 1-byte type + 3-byte (big-endian) length, then the SMB2 packet.
            byte[] nbtHeader = ReadExactly(client, 4);
            int length = (nbtHeader[1] << 16) | (nbtHeader[2] << 8) | nbtHeader[3];
            m_capturedNegotiateRequestBytes = ReadExactly(client, length);
        }

        private static byte[] ReadExactly(TcpClient client, int length)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = client.Client.Receive(buffer, totalRead, length - totalRead, SocketFlags.None);
                if (read == 0)
                {
                    break;
                }
                totalRead += read;
            }
            return buffer;
        }

        [TestMethod]
        public void When_SMB2ClientConnects_NegotiateRequestAdvertisesMultiChannelCapability()
        {
            // [MS-SMB2] 3.2.5.2 requires a client to advertise SMB2_GLOBAL_CAP_MULTI_CHANNEL in its
            // NEGOTIATE request for the server to ever consider binding additional channels to a
            // session established over this connection (see SMB2Client.ServerSupportsMultiChannel /
            // SMBConnectionPool). This does not depend on the server actually supporting it.
            m_tcpListener.BeginAcceptTcpClient(AcceptTcpClient_CaptureNegotiateRequest, null);
            int timeoutInMilliseconds = 300;
            SMB2Client client = new SMB2Client(timeoutInMilliseconds);

            new Thread(() =>
            {
                client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
            }).Start();

            while (!m_clientConnected)
            {
                Thread.Sleep(1);
            }
            // Give the client a moment to finish writing the NEGOTIATE request after connecting.
            Thread.Sleep(50);

            Assert.IsNotNull(m_capturedNegotiateRequestBytes);
            SMBLibrary.SMB2.NegotiateRequest negotiateRequest = new SMBLibrary.SMB2.NegotiateRequest(m_capturedNegotiateRequestBytes, 0);
            Assert.IsTrue((negotiateRequest.Capabilities & SMBLibrary.SMB2.Capabilities.MultiChannel) > 0);
        }
    }
}
