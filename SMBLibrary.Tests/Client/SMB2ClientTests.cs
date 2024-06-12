/* Copyright (C) 2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
        private int m_serverPort;
        private TcpListener m_tcpListener;
        private bool m_clientConnected;

        [TestInitialize]
        public void Initialize()
        {
            m_serverPort = 1000 + new Random().Next(50000);
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

            SMB2Client client = new SMB2Client();
            int timeoutInMilliseconds = 1000;

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Stopwatch stopwatch = new Stopwatch();
            bool isConnected = false;
            new Thread(() =>
            {
                stopwatch.Start();
                isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, timeoutInMilliseconds);
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
            SMB2Client client = new SMB2Client();
            int timeoutInMilliseconds = 1000;

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            Stopwatch stopwatch = new Stopwatch();
            bool isConnected = false;
            new Thread(() =>
            {
                stopwatch.Start();
                isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, timeoutInMilliseconds);
                stopwatch.Stop();
            }).Start();

            while (!m_clientConnected)
            {
                Thread.Sleep(1);
            }
            Assert.IsFalse(isConnected);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200);
        }
    }
}
