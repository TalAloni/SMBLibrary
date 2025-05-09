using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using System;
using System.Net;
using SMBLibrary.Server;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Client.Authentication;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2SigningTests
    {
        private TcpRelay m_tcpRelay;
        private SMBServer m_smbServer;
        private SMB2Client client;
        private static readonly byte[] m_sessionKey = new byte[16];
        private static readonly byte[] m_fakeIdentifier = new byte[] { 1, 2, 3, 4, 5, 6 };

        [TestInitialize]
        public void Initialize()
        {
            new Random().NextBytes(m_sessionKey);

            SMBShareCollection shares = new SMBShareCollection();
            GSSProvider securityProvider = new GSSProvider(new FakeAuthenticationProvider());
            m_smbServer = new SMBServer(shares, securityProvider);
            m_smbServer.Start(IPAddress.Any, SMBTransportType.NetBiosOverTCP, 30002, false, true, true, null);

            m_tcpRelay = new TcpRelay();
            Task.Run(m_tcpRelay.Start);

            client = new SMB2Client();
            client.SigningRequired = true;
            m_tcpRelay.SignatureManipulationEnabled = true;
            Assert.IsTrue(client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, 30001, 3000));
        }

        [TestCleanup]
        public void CleanUp()
        {
            client?.Disconnect();
            m_tcpRelay.Stop();
            m_smbServer.Stop();
        }

        [TestMethod]
        public void When_SignatureIsValid_ShouldNotBeInvalid()
        {
            client.DisconnectOnInvalidSignature = false;
            m_tcpRelay.SignatureManipulationEnabled = false;

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, client.Login(new FakeAuthenticationClient()));

            client.TreeConnect("test", out NTStatus status);
            Assert.AreEqual(NTStatus.STATUS_OBJECT_PATH_NOT_FOUND, status);
            Assert.IsTrue(client.IsConnected);
        }

        [TestMethod]
        public void When_SigningEnabledAndSignatureIsInvalid_LoginShouldFail()
        {
            client.DisconnectOnInvalidSignature = false;
            m_tcpRelay.SignatureManipulationEnabled = true;

            Assert.AreEqual(NTStatus.STATUS_INVALID_SMB, client.Login(new FakeAuthenticationClient()));
            Assert.IsTrue(client.IsConnected);
        }

        [TestMethod]
        public void When_SigningEnabledAndSignatureIsInvalid_LoginShouldDisconnect()
        {
            client.DisconnectOnInvalidSignature = true;
            m_tcpRelay.SignatureManipulationEnabled = true;

            Assert.AreEqual(NTStatus.STATUS_INVALID_SMB, client.Login(new FakeAuthenticationClient()));
            Assert.IsFalse(client.IsConnected);
        }

        [TestMethod]
        public void When_SigningEnabledAndSignatureIsInvalid_CommandShouldFail()
        {
            m_tcpRelay.SignatureManipulationEnabled = false;
            client.DisconnectOnInvalidSignature = false;

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, client.Login(new FakeAuthenticationClient()));

            m_tcpRelay.SignatureManipulationEnabled = true;

            client.TreeConnect("test", out NTStatus status);
            Assert.AreEqual(NTStatus.STATUS_INVALID_SMB, status);
            Assert.IsTrue(client.IsConnected);
        }

        [TestMethod]
        public void When_SigningEnabledAndSignatureIsInvalid_CommandShouldDisconnect()
        {
            m_tcpRelay.SignatureManipulationEnabled = false;
            client.DisconnectOnInvalidSignature = true;

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, client.Login(new FakeAuthenticationClient()));

            m_tcpRelay.SignatureManipulationEnabled = true;

            client.TreeConnect("test", out NTStatus status);
            Assert.AreEqual(NTStatus.STATUS_INVALID_SMB, status);
            Assert.IsFalse(client.IsConnected);
        }

        private class FakeAuthenticationClient : IAuthenticationClient
        {
            public byte[] GetSessionKey()
            {
                return m_sessionKey;
            }

            public byte[] InitializeSecurityContext(byte[] securityBlob)
            {
                SimpleProtectedNegotiationTokenInit initToken = new SimpleProtectedNegotiationTokenInit();
                initToken.MechanismTypeList = new List<byte[]>() { m_fakeIdentifier };
                return initToken.GetBytes(true);
            }
        }

        private class FakeAuthenticationProvider : IGSSMechanism
        {
            public byte[] Identifier => m_fakeIdentifier;

            public NTStatus AcceptSecurityContext(ref object context, byte[] inputToken, out byte[] outputToken)
            {
                outputToken = null;
                return NTStatus.STATUS_SUCCESS;
            }

            public bool DeleteSecurityContext(ref object context)
            {
                return true;
            }

            public object GetContextAttribute(object context, GSSAttributeName attributeName)
            {
                switch (attributeName)
                {
                    case GSSAttributeName.SessionKey:
                        return m_sessionKey;
                }
                return null;
            }
        }

        private class TcpRelay
        {
            private TcpListener m_relay;
            private TcpClient m_localClient;
            private TcpClient m_remoteClient;
            private Thread m_clientToServer;
            private Thread m_serverToClient;

            public bool SignatureManipulationEnabled { get; set; } = false;

            public void Start()
            {
                m_remoteClient = new TcpClient();
                m_remoteClient.Connect(IPAddress.Loopback, 30002);

                m_relay = new TcpListener(IPAddress.Any, 30001);
                m_relay.Start();

                m_localClient = m_relay.AcceptTcpClient();

                NetworkStream localStream = m_localClient.GetStream();
                NetworkStream remoteStream = m_remoteClient.GetStream();

                m_clientToServer = new Thread(() => ForwardData(localStream, remoteStream));
                m_serverToClient = new Thread(() => ForwardData(remoteStream, localStream));

                m_clientToServer.Start();
                m_serverToClient.Start();
            }

            public void Stop()
            {
                m_localClient?.Dispose();
                m_remoteClient?.Dispose();
                m_relay?.Stop();
                m_clientToServer.Join();
                m_serverToClient.Join();
            }

            private void ForwardData(NetworkStream input, NetworkStream output)
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                try
                {
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (TryGetSmb2HeaderOffset(buffer, bytesRead, out int smbOffset) && IsSigned(buffer, smbOffset) && SignatureManipulationEnabled)
                        {
                            ManipulateSignature(buffer, smbOffset);
                        }

                        output.Write(buffer, 0, bytesRead);
                    }
                }
                catch
                {
                    // Likely disconnected
                }
            }

            private static bool TryGetSmb2HeaderOffset(byte[] data, int length, out int offset)
            {
                offset = -1;

                // NetBIOS Session Header?
                if (length >= 68 && data[0] == 0x00)
                {
                    if (data[4] == 0xFE && data[5] == (byte)'S' && data[6] == (byte)'M' && data[7] == (byte)'B')
                    {
                        offset = 4;
                        return true;
                    }
                }
                else if (length >= 64 && data[0] == 0xFE && data[1] == (byte)'S' && data[2] == (byte)'M' && data[3] == (byte)'B')
                {
                    offset = 0;
                    return true;
                }

                return false;
            }

            private static bool IsSigned(byte[] data, int offset)
            {
                uint flags = BitConverter.ToUInt32(data, offset + 16);
                return (flags & 0x00000008) != 0;
            }

            private static void ManipulateSignature(byte[] data, int offset)
            {
                Random rand = new Random();

                // Change a random signature byte
                int idx = rand.Next(48, 48 + 16);

                // Generate a new random byte and make sure it is different from the current value
                int byteValue;
                do
                {
                    byteValue = rand.Next(0, 256);
                }
                while (data[offset + idx] == byteValue);

                data[offset + idx] = (byte) byteValue;
            }
        }
    }
}