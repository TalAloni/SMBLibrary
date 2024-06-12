/* Copyright (C) 2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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

namespace SMBLibrary.Tests.IntegrationTests
{
    [TestClass]
    public class LoginTests
    {
        private int m_serverPort;
        private SMBServer m_server;

        [TestInitialize]
        public void Initialize()
        {
            m_serverPort = 1000 + new Random().Next(50000);
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
        public void When_ValidCredentialsProvided_LoginSucceed()
        {
            // Arrange
            SMB2Client client = new SMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, 5000);

            // Act
            NTStatus status = client.Login("", "John", "password");

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
        }

        [TestMethod]
        public void When_ClientDisconnectAndReconnect_LoginSucceed()
        {
            // Arrange
            SMB2Client client = new SMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, 5000);

            // Act
            NTStatus status = client.Login("", "John", "password");
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            status = client.Logoff();
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            client.Disconnect();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, 5000);
            status = client.Login("", "John", "password");

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
        }

        [TestMethod]
        public void When_InvalidCredentialsProvided_LoginFails()
        {
            // Arrange
            SMB2Client client = new SMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, 5000);

            // Act
            NTStatus status = client.Login("", "John", "Password");

            // Assert
            Assert.AreEqual(NTStatus.STATUS_LOGON_FAILURE, status);
        }
    }
}
