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
            Assert.AreEqual(DfsReferralHeaderFlags.ReferralServers | DfsReferralHeaderFlags.StorageServers , response.ReferralHeaderFlags);
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
        public void ParseResponseGetDfsReferralWithMultipleDfsReferralEntryV4()
        {
            // Arrange
            byte[] buffer = new byte[]
            {
                0x3c, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00, 0x04, 0x00, 0x22, 0x00, 0x00, 0x00, 0x04, 0x00,
                0x2c, 0x01, 0x00, 0x00, 0x44, 0x00, 0x82, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x22, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x2c, 0x01, 0x00, 0x00, 0x22, 0x00, 0x60, 0x00, 0xbc, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5c, 0x00, 0x4c, 0x00,
                0x41, 0x00, 0x42, 0x00, 0x2d, 0x00, 0x44, 0x00, 0x43, 0x00, 0x31, 0x00, 0x2e, 0x00, 0x4c, 0x00,
                0x41, 0x00, 0x42, 0x00, 0x2e, 0x00, 0x4c, 0x00, 0x4f, 0x00, 0x43, 0x00, 0x41, 0x00, 0x4c, 0x00,
                0x5c, 0x00, 0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x73, 0x00, 0x5c, 0x00, 0x53, 0x00,
                0x61, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x73, 0x00, 0x00, 0x00, 0x5c, 0x00, 0x4c, 0x00, 0x41, 0x00,
                0x42, 0x00, 0x2d, 0x00, 0x44, 0x00, 0x43, 0x00, 0x31, 0x00, 0x2e, 0x00, 0x4c, 0x00, 0x41, 0x00,
                0x42, 0x00, 0x2e, 0x00, 0x4c, 0x00, 0x4f, 0x00, 0x43, 0x00, 0x41, 0x00, 0x4c, 0x00, 0x5c, 0x00,
                0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x73, 0x00, 0x5c, 0x00, 0x53, 0x00, 0x61, 0x00,
                0x6c, 0x00, 0x65, 0x00, 0x73, 0x00, 0x00, 0x00, 0x5c, 0x00, 0x4c, 0x00, 0x41, 0x00, 0x42, 0x00,
                0x2d, 0x00, 0x46, 0x00, 0x53, 0x00, 0x32, 0x00, 0x5c, 0x00, 0x53, 0x00, 0x61, 0x00, 0x6c, 0x00,
                0x65, 0x00, 0x73, 0x00, 0x00, 0x00, 0x5c, 0x00, 0x4c, 0x00, 0x41, 0x00, 0x42, 0x00, 0x2d, 0x00,
                0x46, 0x00, 0x53, 0x00, 0x31, 0x00, 0x5c, 0x00, 0x53, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x65, 0x00,
                0x73, 0x00, 0x00, 0x00

            };

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(60, response.PathConsumed);
            Assert.AreEqual(DfsReferralHeaderFlags.StorageServers, response.ReferralHeaderFlags);
            Assert.AreEqual(2, response.ReferralEntries.Count);
            Assert.IsInstanceOfType(response.ReferralEntries[0], typeof(DfsReferralEntryV4));
            Assert.IsInstanceOfType(response.ReferralEntries[1], typeof(DfsReferralEntryV4));

            DfsReferralEntryV4 entry1 = (DfsReferralEntryV4)response.ReferralEntries[0];
            Assert.AreEqual((uint)300, entry1.TimeToLive);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry1.ReferralEntryFlags);
            Assert.AreEqual("\\LAB-DC1.LAB.LOCAL\\Files\\Sales", entry1.DfsPath);
            Assert.AreEqual("\\LAB-DC1.LAB.LOCAL\\Files\\Sales", entry1.DfsAlternatePath);
            Assert.AreEqual("\\LAB-FS2\\Sales", entry1.NetworkAddress);
            Assert.AreEqual(Guid.Empty, entry1.ServiceSiteGuid);

            DfsReferralEntryV4 entry2 = (DfsReferralEntryV4)response.ReferralEntries[1];
            Assert.AreEqual((uint)300, entry2.TimeToLive);
            Assert.AreEqual(DfsReferralEntryFlags.None, entry2.ReferralEntryFlags);
            Assert.AreEqual("\\LAB-DC1.LAB.LOCAL\\Files\\Sales", entry2.DfsPath);
            Assert.AreEqual("\\LAB-DC1.LAB.LOCAL\\Files\\Sales", entry2.DfsAlternatePath);
            Assert.AreEqual("\\LAB-FS1\\Sales", entry2.NetworkAddress);
            Assert.AreEqual(Guid.Empty, entry2.ServiceSiteGuid);
        }

        [TestMethod]
        public void Parse_ResponseGetDfsReferralWithSingleDfsReferralEntryV4_GetBytes()
        {
            // Arrange
            ResponseGetDfsReferral response = new ResponseGetDfsReferral();
            response.PathConsumed = 62;
            response.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferralServers | DfsReferralHeaderFlags.StorageServers;
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
            Assert.AreEqual(DfsReferralHeaderFlags.ReferralServers | DfsReferralHeaderFlags.StorageServers, response.ReferralHeaderFlags);
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
        public void V1_RoundTrip_PreservesAllFields()
        {
            // Arrange
            ResponseGetDfsReferral original = new ResponseGetDfsReferral();
            original.PathConsumed = 40;
            original.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferralServers;
            original.ReferralEntries = new List<DfsReferralEntry>()
            {
                new DfsReferralEntryV1()
                {
                    ServerType = DfsServerType.Root,
                    ReferralEntryFlags = DfsReferralEntryFlags.None,
                    ShareName = "\\\\server\\share"
                }
            };

            // Act
            byte[] buffer = original.GetBytes();
            ResponseGetDfsReferral parsed = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(original.PathConsumed, parsed.PathConsumed);
            Assert.AreEqual(original.ReferralHeaderFlags, parsed.ReferralHeaderFlags);
            Assert.AreEqual(1, parsed.ReferralEntries.Count);
            Assert.IsInstanceOfType(parsed.ReferralEntries[0], typeof(DfsReferralEntryV1));

            DfsReferralEntryV1 entry = (DfsReferralEntryV1)parsed.ReferralEntries[0];
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
            Assert.AreEqual("\\\\server\\share", entry.ShareName);
        }

        [TestMethod]
        public void V2_RoundTrip_PreservesAllFields()
        {
            // Arrange
            ResponseGetDfsReferral original = new ResponseGetDfsReferral();
            original.PathConsumed = 50;
            original.ReferralHeaderFlags = DfsReferralHeaderFlags.StorageServers;
            original.ReferralEntries = new List<DfsReferralEntry>()
            {
                new DfsReferralEntryV2()
                {
                    ServerType = DfsServerType.NonRoot,
                    ReferralEntryFlags = DfsReferralEntryFlags.None,
                    Proximity = 100,
                    TimeToLive = 600,
                    DfsPath = "\\\\domain\\namespace",
                    DfsAlternatePath = "\\\\domain\\namespace",
                    NetworkAddress = "\\\\server\\share"
                }
            };

            // Act
            byte[] buffer = original.GetBytes();
            ResponseGetDfsReferral parsed = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(1, parsed.ReferralEntries.Count);
            Assert.IsInstanceOfType(parsed.ReferralEntries[0], typeof(DfsReferralEntryV2));

            DfsReferralEntryV2 entry = (DfsReferralEntryV2)parsed.ReferralEntries[0];
            Assert.AreEqual((uint)100, entry.Proximity);
            Assert.AreEqual((uint)600, entry.TimeToLive);
            Assert.AreEqual("\\\\domain\\namespace", entry.DfsPath);
            Assert.AreEqual("\\\\domain\\namespace", entry.DfsAlternatePath);
            Assert.AreEqual("\\\\server\\share", entry.NetworkAddress);
        }

        [TestMethod]
        public void V3_RoundTrip_PreservesAllFields()
        {
            // Arrange
            Guid testGuid = Guid.NewGuid();
            ResponseGetDfsReferral original = new ResponseGetDfsReferral();
            original.PathConsumed = 60;
            original.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferralServers | DfsReferralHeaderFlags.StorageServers;
            original.ReferralEntries = new List<DfsReferralEntry>()
            {
                new DfsReferralEntryV3()
                {
                    ServerType = DfsServerType.Root,
                    ReferralEntryFlags = DfsReferralEntryFlags.None,
                    TimeToLive = 300,
                    DfsPath = "\\\\domain\\dfs",
                    DfsAlternatePath = "\\\\domain\\dfs",
                    NetworkAddress = "\\\\fileserver\\share",
                    ServiceSiteGuid = testGuid
                }
            };

            // Act
            byte[] buffer = original.GetBytes();
            ResponseGetDfsReferral parsed = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(1, parsed.ReferralEntries.Count);
            Assert.IsInstanceOfType(parsed.ReferralEntries[0], typeof(DfsReferralEntryV3));

            DfsReferralEntryV3 entry = (DfsReferralEntryV3)parsed.ReferralEntries[0];
            Assert.AreEqual((uint)300, entry.TimeToLive);
            Assert.AreEqual("\\\\domain\\dfs", entry.DfsPath);
            Assert.AreEqual("\\\\fileserver\\share", entry.NetworkAddress);
            Assert.AreEqual(testGuid, entry.ServiceSiteGuid);
        }

        [TestMethod]
        public void MultipleV3Referrals_RoundTrip_PreservesAll()
        {
            // Arrange
            ResponseGetDfsReferral original = new ResponseGetDfsReferral();
            original.PathConsumed = 40;
            original.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferralServers | DfsReferralHeaderFlags.StorageServers;
            original.ReferralEntries = new List<DfsReferralEntry>()
            {
                new DfsReferralEntryV3()
                {
                    ServerType = DfsServerType.Root,
                    TimeToLive = 300,
                    DfsPath = "\\\\domain\\dfs",
                    DfsAlternatePath = "\\\\domain\\dfs",
                    NetworkAddress = "\\\\server1\\share",
                    ServiceSiteGuid = Guid.Empty
                },
                new DfsReferralEntryV3()
                {
                    ServerType = DfsServerType.Root,
                    TimeToLive = 300,
                    DfsPath = "\\\\domain\\dfs",
                    DfsAlternatePath = "\\\\domain\\dfs",
                    NetworkAddress = "\\\\server2\\share",
                    ServiceSiteGuid = Guid.Empty
                }
            };

            // Act
            byte[] buffer = original.GetBytes();
            ResponseGetDfsReferral parsed = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(2, parsed.ReferralEntries.Count);
            Assert.AreEqual("\\\\server1\\share", ((DfsReferralEntryV3)parsed.ReferralEntries[0]).NetworkAddress);
            Assert.AreEqual("\\\\server2\\share", ((DfsReferralEntryV3)parsed.ReferralEntries[1]).NetworkAddress);
        }

        [TestMethod]
        public void EmptyReferralList_RoundTrip_Works()
        {
            // Arrange
            ResponseGetDfsReferral original = new ResponseGetDfsReferral();
            original.PathConsumed = 20;
            original.ReferralHeaderFlags = DfsReferralHeaderFlags.ReferralServers;
            original.ReferralEntries = new List<DfsReferralEntry>();

            // Act
            byte[] buffer = original.GetBytes();
            ResponseGetDfsReferral parsed = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual(20, parsed.PathConsumed);
            Assert.AreEqual(0, parsed.ReferralEntries.Count);
        }
    }
}
