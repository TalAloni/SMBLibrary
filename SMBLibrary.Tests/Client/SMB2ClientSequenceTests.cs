/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using SMBLibrary.SMB2;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SMBLibrary.Tests.Client
{
    /// <summary>
    /// The message id of the request following NEGOTIATE.
    /// </summary>
    /// <remarks>
    /// See https://github.com/TalAloni/SMBLibrary/pull/370.
    /// A server rejects an id it has already granted, so the sequence number must advance.
    /// The window is a few nanoseconds wide, so reproducing it consistently depends on the sending thread being descheduled inside it. 
    /// Holding the window open makes it happen every time: add System.Threading.Thread.Sleep(1) in TrySendCommand.
    /// </remarks>
    [TestClass]
    public class SMB2ClientSequenceTests
    {
        private const int Handshakes = 500;

        private const int ResponseTimeoutInMilliseconds = 150;

        private TcpListener m_listener;
        private int m_port;
        private volatile bool m_stopping;

        /// <summary>The message ids seen after NEGOTIATE. Every one of them should be 1.</summary>
        private readonly System.Collections.Generic.List<ulong> m_idsAfterNegotiate =
            new System.Collections.Generic.List<ulong>();

        [TestInitialize]
        public void Initialize()
        {
            m_listener = new TcpListener(IPAddress.Loopback, 0);
            m_listener.Start();
            m_port = ((IPEndPoint)m_listener.LocalEndpoint).Port;
            new Thread(Serve) { IsBackground = true }.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_stopping = true;
            m_listener.Stop();
        }

        [TestMethod]
        public void When_NegotiateIsAnsweredImmediately_TheNextRequestDoesNotReuseItsMessageID()
        {
            for (int index = 0; index < Handshakes; index++)
            {
                SequenceTestClient client = new SequenceTestClient(ResponseTimeoutInMilliseconds);
                try
                {
                    if (!client.Connect(IPAddress.Loopback, m_port))
                    {
                        continue;
                    }

                    // Fails but the request goes out which is all this needs.
                    client.Login(String.Empty, "user", "password");
                }
                catch (Exception)
                {
                }
                finally
                {
                    try { client.Disconnect(); } catch (Exception) { }
                }
            }

            lock (m_idsAfterNegotiate)
            {
                Assert.AreNotEqual(0, m_idsAfterNegotiate.Count, "no handshake completed");

                foreach (ulong messageID in m_idsAfterNegotiate)
                {
                    Assert.AreEqual(1UL, messageID, "The request after NEGOTIATE reused its message id, which a server rejects");
                }
            }
        }

        /// <summary>Enough of a server to answer NEGOTIATE and read one more request.</summary>
        private void Serve()
        {
            while (!m_stopping)
            {
                TcpClient connection;
                try
                {
                    connection = m_listener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    return;
                }

                using (connection)
                {
                    try
                    {
                        NetworkStream stream = connection.GetStream();
                        if (ReadMessage(stream) == null)
                        {
                            continue;
                        }

                        // Answer immediately advertising LargeMTU. Makes the client set Connection.SupportsMultiCredit
                        Send(stream, GetNegotiateResponse());

                        byte[] next = ReadMessage(stream);
                        if (next == null)
                        {
                            continue;
                        }

                        ulong messageID = BitConverter.ToUInt64(next, 24);
                        lock (m_idsAfterNegotiate)
                        {
                            m_idsAfterNegotiate.Add(messageID);
                        }
                    }
                    catch (Exception)
                    {
                        // Client gave up, ignore
                    }
                }
            }
        }

        private static byte[] GetNegotiateResponse()
        {
            NegotiateResponse response = new NegotiateResponse();
            response.DialectRevision = SMB2Dialect.SMB210;
            response.SecurityMode = SecurityMode.SigningEnabled;
            response.Capabilities = Capabilities.LargeMTU;
            response.MaxTransactSize = 65536;
            response.MaxReadSize = 65536;
            response.MaxWriteSize = 65536;
            response.ServerGuid = Guid.NewGuid();
            response.SystemTime = DateTime.UtcNow;
            response.ServerStartTime = DateTime.UtcNow;
            response.Header.Status = NTStatus.STATUS_SUCCESS;
            response.Header.Credits = 32;
            response.Header.MessageID = 0;
            return response.GetBytes();
        }

        private static byte[] ReadMessage(NetworkStream stream)
        {
            byte[] header = ReadExactly(stream, 4);
            if (header == null)
            {
                return null;
            }

            // Direct TCP transport: a zero, then three bytes of length.
            int length = (header[1] << 16) | (header[2] << 8) | header[3];
            return ReadExactly(stream, length);
        }

        private static byte[] ReadExactly(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                int received = stream.Read(buffer, read, count - read);
                if (received <= 0)
                {
                    return null;
                }

                read += received;
            }

            return buffer;
        }

        private static void Send(NetworkStream stream, byte[] message)
        {
            byte[] packet = new byte[4 + message.Length];
            packet[1] = (byte)(message.Length >> 16);
            packet[2] = (byte)(message.Length >> 8);
            packet[3] = (byte)message.Length;
            Buffer.BlockCopy(message, 0, packet, 4, message.Length);
            stream.Write(packet, 0, packet.Length);
            stream.Flush();
        }

        private class SequenceTestClient : SMB2Client
        {
            public SequenceTestClient(int responseTimeoutInMilliseconds) : base(responseTimeoutInMilliseconds)
            {
            }

            public bool Connect(IPAddress address, int port)
            {
                return Connect(address, SMBTransportType.DirectTCPTransport, port);
            }
        }
    }
}
