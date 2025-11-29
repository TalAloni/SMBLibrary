/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.DFS;
using System;
using System.Collections.Generic;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ResponseGetDfsReferralTests
    {
        [TestMethod]
        public void ParseResponseGetDfsReferralWithSingleDfsReferralEntryV4()
        {
            // Arrange
            // Returned by Windows Server 2008 R2 SP1
            byte[] buffer = new byte[]
            {
                0x3e ,0x00 ,0x01 ,0x00 ,0x03 ,0x00 ,0x00 ,0x00 ,0x04 ,0x00 ,0x22 ,0x00 ,0x01 ,0x00 ,0x04 ,0x00,
                0x2c ,0x01 ,0x00 ,0x00 ,0x22 ,0x00 ,0x62 ,0x00 ,0xa2 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00,
                0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x00 ,0x5c ,0x00 ,0x57 ,0x00 ,0x49 ,0x00,
                0x4e ,0x00 ,0x2d ,0x00 ,0x4d ,0x00 ,0x51 ,0x00 ,0x38 ,0x00 ,0x33 ,0x00 ,0x44 ,0x00 ,0x45 ,0x00,
                0x35 ,0x00 ,0x4e ,0x00 ,0x47 ,0x00 ,0x37 ,0x00 ,0x32 ,0x00 ,0x5c ,0x00 ,0x44 ,0x00 ,0x66 ,0x00,
                0x73 ,0x00 ,0x20 ,0x00 ,0x4e ,0x00 ,0x61 ,0x00 ,0x6d ,0x00 ,0x65 ,0x00 ,0x73 ,0x00 ,0x70 ,0x00,
                0x61 ,0x00 ,0x63 ,0x00 ,0x65 ,0x00 ,0x31 ,0x00 ,0x00 ,0x00 ,0x5c ,0x00 ,0x57 ,0x00 ,0x49 ,0x00,
                0x4e ,0x00 ,0x2d ,0x00 ,0x4d ,0x00 ,0x51 ,0x00 ,0x38 ,0x00 ,0x33 ,0x00 ,0x44 ,0x00 ,0x45 ,0x00,
                0x35 ,0x00 ,0x4e ,0x00 ,0x47 ,0x00 ,0x37 ,0x00 ,0x32 ,0x00 ,0x5c ,0x00 ,0x44 ,0x00 ,0x66 ,0x00,
                0x73 ,0x00 ,0x20 ,0x00 ,0x4e ,0x00 ,0x61 ,0x00 ,0x6d ,0x00 ,0x65 ,0x00 ,0x73 ,0x00 ,0x70 ,0x00,
                0x61 ,0x00 ,0x63 ,0x00 ,0x65 ,0x00 ,0x31 ,0x00 ,0x00 ,0x00 ,0x5c ,0x00 ,0x57 ,0x00 ,0x49 ,0x00,
                0x4e ,0x00 ,0x2d ,0x00 ,0x4d ,0x00 ,0x51 ,0x00 ,0x38 ,0x00 ,0x33 ,0x00 ,0x44 ,0x00 ,0x45 ,0x00,
                0x35 ,0x00 ,0x4e ,0x00 ,0x47 ,0x00 ,0x37 ,0x00 ,0x32 ,0x00 ,0x5c ,0x00 ,0x44 ,0x00 ,0x66 ,0x00,
                0x73 ,0x00 ,0x20 ,0x00 ,0x4e ,0x00 ,0x61 ,0x00 ,0x6d ,0x00 ,0x65 ,0x00 ,0x73 ,0x00 ,0x70 ,0x00,
                0x61 ,0x00 ,0x63 ,0x00 ,0x65 ,0x00 ,0x31 ,0x00 ,0x00 ,0x00
            };

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(62, response.PathConsumed);
            Assert.AreEqual(DfsReferralHeaderFlags.ReferalServers | DfsReferralHeaderFlags.StorageServers , response.ReferralHeaderFlags);
            Assert.AreEqual(1, response.ReferralEntries.Count);
            Assert.IsInstanceOfType(response.ReferralEntries[0], typeof(DfsReferralEntryV4));

            DfsReferralEntryV4 entry = (DfsReferralEntryV4)response.ReferralEntries[0];
            Assert.AreEqual((uint)300, entry.TimeToLive);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry.ReferralEntryFlags);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.DfsPath);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.DfsAlternatePath);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.NetworkAddress);
            Assert.AreEqual(Guid.Empty, entry.ServiceSiteGuid);
        }

        [TestMethod]
        public void Parse_ResponseGetDfsReferralWithSingleDfsReferralEntryV4_GetBytes()
        {
            // Arrange
            ResponseGetDfsReferral response = new ResponseGetDfsReferral();
            response.PathConsumed = 62;
            response.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferalServers | DfsReferralHeaderFlags.StorageServers;
            response.ReferralEntries = new List<DfsReferralEntry>()
            {
                new DfsReferralEntryV4()
                {
                    TimeToLive = 300,
                    ReferralEntryFlags = DfsReferralEntryFlags.TargetSetBoundary,
                    DfsPath = "\\WIN-MQ83DE5NG72\\Dfs Namespace1",
                    DfsAlternatePath = "\\WIN-MQ83DE5NG72\\Dfs Namespace1",
                    NetworkAddress = "\\WIN-MQ83DE5NG72\\Dfs Namespace1",
                    ServiceSiteGuid = Guid.Empty
                }
            };

            // Act
            response = new ResponseGetDfsReferral(response.GetBytes());

            // Assert
            Assert.AreEqual(62, response.PathConsumed);
            Assert.AreEqual(DfsReferralHeaderFlags.ReferalServers | DfsReferralHeaderFlags.StorageServers, response.ReferralHeaderFlags);
            Assert.AreEqual(1, response.ReferralEntries.Count);
            Assert.IsInstanceOfType(response.ReferralEntries[0], typeof(DfsReferralEntryV4));

            DfsReferralEntryV4 entry = (DfsReferralEntryV4)response.ReferralEntries[0];
            Assert.AreEqual((uint)300, entry.TimeToLive);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry.ReferralEntryFlags);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.DfsPath);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.DfsAlternatePath);
            Assert.AreEqual("\\WIN-MQ83DE5NG72\\Dfs Namespace1", entry.NetworkAddress);
            Assert.AreEqual(Guid.Empty, entry.ServiceSiteGuid);
        }
    }
}
