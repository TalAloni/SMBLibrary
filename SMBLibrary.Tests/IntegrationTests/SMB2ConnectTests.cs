/* Copyright (C) 2024-2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Client;
using SMBLibrary.Server;
using System;
using System.Net;
using System.Net.Sockets;

namespace SMBLibrary.Tests.IntegrationTests
{
    public class TestSMB2Client : SMB2Client
    {
        public int? LastConnectSocketPort { get; private set; }

        protected override bool ConnectSocket(IPAddress serverAddress, int port)
        {
            LastConnectSocketPort = port;
            return base.ConnectSocket(serverAddress, port);
        }
    }

    [TestClass]
    public class SMB2ConnectTests
    {
        private static Random s_seedGenerator = new Random();

        private int m_serverPort;
        private SMBServer m_server;

        [TestInitialize]
        public void Initialize()
        {
            // Setup test server on a random port
            m_serverPort = 1000 + new Random(s_seedGenerator.Next()).Next(50000);
            SMBShareCollection shares = new SMBShareCollection();
            IGSSMechanism gssMechanism = new IndependentNTLMAuthenticationProvider((username) => "password");
            GSSProvider gssProvider = new GSSProvider(gssMechanism);
            m_server = new SMBServer(shares, gssProvider);
            m_server.Start(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, false, true, false, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_server.Stop();
        }

        [TestMethod]
        public void When_ConnectWithIPAddressAndCustomPort_ShouldConnectSuccessfully()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();

            // Act
            bool isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);

            // Assert
            Assert.IsTrue(isConnected);
            Assert.IsTrue(client.IsConnected);
            
            // Verify the actual port passed to ConnectSocket
            Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
            Assert.AreEqual(m_serverPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with port {m_serverPort} but was called with port {client.LastConnectSocketPort.Value}");
        }

        [TestMethod]
        public void When_ConnectWithServerNameAndCustomPort_ShouldConnectSuccessfully()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();

            // Act
            bool isConnected = client.Connect("localhost", SMBTransportType.DirectTCPTransport, m_serverPort);

            // Assert
            Assert.IsTrue(isConnected);
            Assert.IsTrue(client.IsConnected);
            
            // Verify the actual port passed to ConnectSocket
            Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
            Assert.AreEqual(m_serverPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with port {m_serverPort} but was called with port {client.LastConnectSocketPort.Value}");
        }

        [TestMethod]
        public void When_ConnectWithIPAddressAndPortZero_ShouldUseDefaultPort445()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();
            int expectedDefaultPort = 445; // DirectTCPPort constant

            // Try to start a server on the default port to verify it's being used
            SMBServer defaultPortServer = null;
            bool serverStarted = false;
            try
            {
                SMBShareCollection shares = new SMBShareCollection();
                IGSSMechanism gssMechanism = new IndependentNTLMAuthenticationProvider((username) => "password");
                GSSProvider gssProvider = new GSSProvider(gssMechanism);
                defaultPortServer = new SMBServer(shares, gssProvider);
                defaultPortServer.Start(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, expectedDefaultPort, false, true, false, null);
                serverStarted = true;
            }
            catch (SocketException)
            {
                //Leads to inconclusive test result if we can't bind to the default port (requires admin privileges or is already in use)
                //Still checks if the client uses the correct default port when attempting to connect
            }

            try
            {
                // Act
                // Port 0 should be treated as "use default port" (445 for DirectTCP)
                bool isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, 0);

                // Verify the actual port passed to ConnectSocket is 445
                Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
                Assert.AreEqual(expectedDefaultPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with default port {expectedDefaultPort} but was called with port {client.LastConnectSocketPort.Value}");
                if (serverStarted || isConnected) //Either the started server was used or an existing process was used to connect to the default port, both cases indicate that the client is correctly using the default port when 0 is specified
                {
                    Assert.IsTrue(isConnected, "Connection to default port should succeed when server is running");
                }
                else
                {
                    Assert.Inconclusive($"Cannot bind to port {expectedDefaultPort} (requires admin privileges or port is in use). Connection not verified.");
                }
            }
            finally
            {
                if (serverStarted && defaultPortServer != null)
                {
                    defaultPortServer.Stop();
                }
            }
        }

        [TestMethod]
        public void When_ConnectWithServerNameAndPortZero_ShouldUseDefaultPort445()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();
            int expectedDefaultPort = 445; // DirectTCPPort constant

            // Try to start a server on the default port to verify it's being used
            SMBServer defaultPortServer = null;
            bool serverStarted = false;
            try
            {
                SMBShareCollection shares = new SMBShareCollection();
                IGSSMechanism gssMechanism = new IndependentNTLMAuthenticationProvider((username) => "password");
                GSSProvider gssProvider = new GSSProvider(gssMechanism);
                defaultPortServer = new SMBServer(shares, gssProvider);
                defaultPortServer.Start(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, expectedDefaultPort, false, true, false, null);
                serverStarted = true;
            }
            catch (SocketException)
            {
                //Leads to inconclusive test result if we can't bind to the default port (requires admin privileges or is already in use)
                //Still checks if the client uses the correct default port when attempting to connect
            }

            try
            {
                // Act
                // Port 0 should be treated as "use default port" (445 for DirectTCP)
                bool isConnected = client.Connect("localhost", SMBTransportType.DirectTCPTransport, 0);

                // Verify the actual port passed to ConnectSocket is 445
                Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
                Assert.AreEqual(expectedDefaultPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with default port {expectedDefaultPort} but was called with port {client.LastConnectSocketPort.Value}");
                if (serverStarted || isConnected) //Either the started server was used or an existing process was used to connect to the default port, both cases indicate that the client is correctly using the default port when 0 is specified
                {
                    Assert.IsTrue(isConnected, "Connection to default port should succeed when server is running");
                }
                else
                {
                    Assert.Inconclusive($"Cannot bind to port {expectedDefaultPort} (requires admin privileges or port is in use). Connection not verified.");
                }
            }
            finally
            {
                if (serverStarted && defaultPortServer != null)
                {
                    defaultPortServer.Stop();
                }
            }
        }

        [TestMethod]
        public void When_ConnectWithInvalidPort_ShouldFailGracefully()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();
            int invalidPort = 1; // Port 1 should not have an SMB server

            // Act
            bool isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, invalidPort);

            // Assert
            Assert.IsFalse(isConnected);
            Assert.IsFalse(client.IsConnected);
            Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
            Assert.AreEqual(invalidPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with port {invalidPort} but was called with port {client.LastConnectSocketPort.Value}");
        }

        [TestMethod]
        public void When_DisconnectAfterConnect_IsConnectedShouldBeFalse()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
            Assert.IsTrue(client.IsConnected);

            // Act
            client.Disconnect();

            // Assert
            Assert.IsFalse(client.IsConnected);
        }

        [TestMethod]
        public void When_ConnectAndLogin_ShouldWorkWithCustomPort()
        {
            // Arrange
            TestSMB2Client client = new TestSMB2Client();

            // Act
            bool isConnected = client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
            NTStatus loginStatus = client.Login("", "John", "password");

            // Assert
            Assert.IsTrue(isConnected);
            Assert.IsTrue(client.IsConnected);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, loginStatus);
            
            // Verify the actual port passed to ConnectSocket
            Assert.IsNotNull(client.LastConnectSocketPort, "ConnectSocket was not called");
            Assert.AreEqual(m_serverPort, client.LastConnectSocketPort.Value, $"Expected ConnectSocket to be called with port {m_serverPort} but was called with port {client.LastConnectSocketPort.Value}");
        }
    }
}
