/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.NetworkInterface;
using System;
using System.Net;
using Utilities;

namespace SMBLibrary.Tests.NetworkInterface
{
    [TestClass]
    public class QueryNetworkInterfaceInfoResponseTests
    {
        [TestMethod]
        public void ParseQueryNetworkInterfaceInfoResponse_WithSingleIPv4Entry()
        {
            // Arrange - one NETWORK_INTERFACE_INFO entry (Next = 0, i.e. the only/last entry),
            // RSS-capable, 1 Gbps link speed, sockaddr_storage carrying an IPv4 address.
            byte[] buffer = BuildEntry(next: 0, ifIndex: 1, capability: NetworkInterfaceCapabilities.RssCapable,
                linkSpeed: 1000000000, sockAddrStorage: BuildIPv4SockAddrStorage(IPAddress.Parse("192.168.1.10")));

            // Act
            QueryNetworkInterfaceInfoResponse response = new QueryNetworkInterfaceInfoResponse(buffer);

            // Assert
            Assert.AreEqual(1, response.Entries.Count);
            NetworkInterfaceInfo entry = response.Entries[0];
            Assert.AreEqual((uint)1, entry.IfIndex);
            Assert.IsTrue(entry.IsRssCapable);
            Assert.IsFalse(entry.IsRdmaCapable);
            Assert.AreEqual((ulong)1000000000, entry.LinkSpeed);
            Assert.AreEqual(IPAddress.Parse("192.168.1.10"), entry.IPAddress);
        }

        [TestMethod]
        public void ParseQueryNetworkInterfaceInfoResponse_WithMultipleEntriesAndMixedFamilies()
        {
            // Arrange - two chained entries: first IPv4/RDMA-capable, second IPv6/RSS-capable.
            byte[] firstEntry = BuildEntry(next: 0, ifIndex: 2, capability: NetworkInterfaceCapabilities.RdmaCapable,
                linkSpeed: 10000000000, sockAddrStorage: BuildIPv4SockAddrStorage(IPAddress.Parse("10.0.0.5")));
            byte[] secondEntry = BuildEntry(next: 0, ifIndex: 3, capability: NetworkInterfaceCapabilities.RssCapable,
                linkSpeed: 1000000000, sockAddrStorage: BuildIPv6SockAddrStorage(IPAddress.Parse("fe80::1")));

            byte[] buffer = new byte[firstEntry.Length + secondEntry.Length];
            // Next field (first 4 bytes of firstEntry) points past the first entry to the second one.
            LittleEndianWriter.WriteUInt32(firstEntry, 0, (uint)firstEntry.Length);
            Array.Copy(firstEntry, 0, buffer, 0, firstEntry.Length);
            Array.Copy(secondEntry, 0, buffer, firstEntry.Length, secondEntry.Length);

            // Act
            QueryNetworkInterfaceInfoResponse response = new QueryNetworkInterfaceInfoResponse(buffer);

            // Assert
            Assert.AreEqual(2, response.Entries.Count);
            Assert.AreEqual((uint)2, response.Entries[0].IfIndex);
            Assert.IsTrue(response.Entries[0].IsRdmaCapable);
            Assert.AreEqual(IPAddress.Parse("10.0.0.5"), response.Entries[0].IPAddress);

            Assert.AreEqual((uint)3, response.Entries[1].IfIndex);
            Assert.IsTrue(response.Entries[1].IsRssCapable);
            Assert.AreEqual(IPAddress.Parse("fe80::1"), response.Entries[1].IPAddress);
        }

        private static byte[] BuildEntry(uint next, uint ifIndex, NetworkInterfaceCapabilities capability, ulong linkSpeed, byte[] sockAddrStorage)
        {
            byte[] buffer = new byte[4 + NetworkInterfaceInfo.FixedLength];
            LittleEndianWriter.WriteUInt32(buffer, 0, next);
            LittleEndianWriter.WriteUInt32(buffer, 4, ifIndex);
            LittleEndianWriter.WriteUInt32(buffer, 8, (uint)capability);
            LittleEndianWriter.WriteUInt32(buffer, 12, 0); // Reserved
            LittleEndianWriter.WriteUInt64(buffer, 16, linkSpeed);
            Array.Copy(sockAddrStorage, 0, buffer, 24, sockAddrStorage.Length);
            return buffer;
        }

        private static byte[] BuildIPv4SockAddrStorage(IPAddress address)
        {
            byte[] sockAddr = new byte[128];
            LittleEndianWriter.WriteUInt16(sockAddr, 0, 2); // AF_INET
            Array.Copy(address.GetAddressBytes(), 0, sockAddr, 4, 4);
            return sockAddr;
        }

        private static byte[] BuildIPv6SockAddrStorage(IPAddress address)
        {
            byte[] sockAddr = new byte[128];
            LittleEndianWriter.WriteUInt16(sockAddr, 0, 23); // AF_INET6
            Array.Copy(address.GetAddressBytes(), 0, sockAddr, 8, 16);
            return sockAddr;
        }
    }
}
