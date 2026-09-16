/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.SMB1;
using System;

namespace SMBLibrary.Tests.SMB1
{
    [TestClass]
    public class NegotiateResponseTests
    {
        [TestMethod]
        public void WriteAndReadNegotiateResponse_MaxNumberVcsRoundtrip()
        {
            // Arrange - MaxNumberVcs ("Maximum Number of Virtual Circuits") is the server-advertised
            // limit on how many concurrent SMB1 connections/sessions the server accepts from this
            // client (consumed by SMB1Client.MaxNumberOfVirtualCircuits).
            NegotiateResponse response = new NegotiateResponse();
            response.DialectIndex = 0;
            response.MaxMpxCount = 50;
            response.MaxNumberVcs = 1;
            response.MaxBufferSize = 16644;
            response.Capabilities = Capabilities.Unicode | Capabilities.NTSMB | Capabilities.RpcRemoteApi | Capabilities.NTStatusCode;
            response.SystemTime = DateTime.UtcNow;
            response.Challenge = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            response.DomainName = "WORKGROUP";
            response.ServerName = "SERVER1";

            // Act
            byte[] bytes = response.GetBytes(true);
            NegotiateResponse rewritten = new NegotiateResponse(bytes, 0, true);

            // Assert
            Assert.AreEqual((ushort)1, rewritten.MaxNumberVcs);
            Assert.AreEqual((ushort)50, rewritten.MaxMpxCount);
        }

        [TestMethod]
        public void WriteAndReadNegotiateResponse_MaxNumberVcsAboveOneAllowsMultipleConnections()
        {
            // Arrange - some servers (and legacy CIFS/NT4.0-era devices in particular) may only
            // ever report 1, but nothing in the wire format limits it to 1; a pool/consumer relying
            // on this field must handle values greater than 1 correctly.
            NegotiateResponse response = new NegotiateResponse();
            response.MaxNumberVcs = 10;
            response.Capabilities = Capabilities.Unicode;
            response.SystemTime = DateTime.UtcNow;

            // Act
            byte[] bytes = response.GetBytes(true);
            NegotiateResponse rewritten = new NegotiateResponse(bytes, 0, true);

            // Assert
            Assert.AreEqual((ushort)10, rewritten.MaxNumberVcs);
        }
    }
}
