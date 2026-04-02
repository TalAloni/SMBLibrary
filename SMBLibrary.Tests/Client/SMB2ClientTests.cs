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
        private int m_serverPort;
        private TcpListener m_tcpListener;
        private bool m_clientConnected;
        
        // ensure that multiple tests run at the same instant in time receive different port numbers
        // by using a random number generator that is NOT seeded solely by current time
        private RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        
        private int GetRandomPortNumber(int lowerBound, int upperBound)
        {
            if (lowerBound < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lowerBound), "lowerBound must be greater than 0");
            }
            
            if (upperBound < lowerBound || upperBound > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBound), "upperBound must be greater-or-equal to lowerBound and less than 65536");
            }
            
            // rejection sampling is overkill for this application, but whatever, at least the tests pass now.
            // use 2 bytes to generate a random number in the range 0 to 65535
            var bytes = new byte[2];
            int portNumber;
            do
            {
                rng.GetBytes(bytes);
                portNumber = BitConverter.ToUInt16(bytes, 0);
            } while (portNumber < lowerBound || portNumber > upperBound);

            return portNumber;
        }
        
        [TestInitialize]
        public void Initialize()
        {
            m_serverPort = GetRandomPortNumber(1025, 65535);
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
    }
}
