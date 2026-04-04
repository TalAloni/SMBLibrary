/* Copyright (C) 2024-2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
using System.Security.Cryptography;
using System.Threading;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2ClientTests
    {        
        // ensure that multiple tests run at the same instant in time receive different port numbers
        // by incrementing the port number for each test
        private static readonly int minPort = 1025;
        private static readonly int maxPort = 65535;
        private static int m_serverPort = minPort + new Random().Next(maxPort - minPort);
        
        private TcpListener m_tcpListener;
        private bool m_clientConnected;
        
        private static int GetNextPortNumber()
        {
            Interlocked.Increment(ref m_serverPort);
            
            if (m_serverPort > maxPort) 
                m_serverPort = minPort;

            return m_serverPort;
        }
        
        [TestInitialize]
        public void Initialize()
        {
            m_tcpListener = new TcpListener(IPAddress.Loopback, GetNextPortNumber());
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
    }
}
