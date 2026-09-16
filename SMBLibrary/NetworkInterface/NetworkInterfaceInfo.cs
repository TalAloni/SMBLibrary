/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Net;
using System.Net.Sockets;
using Utilities;

namespace SMBLibrary.NetworkInterface
{
    /// <summary>
    /// [MS-SMB2] 2.2.32.5.1 NETWORK_INTERFACE_INFO
    /// One entry of a FSCTL_QUERY_NETWORK_INTERFACE_INFO response, describing a single network
    /// interface the server is willing to accept SMB3 Multichannel connections on.
    /// </summary>
    public class NetworkInterfaceInfo
    {
        public const int FixedLength = 148; // IfIndex .. SockAddr_Storage (excludes the leading 4-byte Next field)

        private const int SockAddrStorageLength = 128;
        private const ushort AF_INET = 2;
        private const ushort AF_INET6 = 23; // matches Windows' WSA AF_INET6 value used on the wire

        public uint IfIndex;
        public NetworkInterfaceCapabilities Capability;
        public uint Reserved;
        public ulong LinkSpeed; // bits per second
        public IPAddress IPAddress; // null if the address family could not be parsed

        public bool IsRssCapable
        {
            get
            {
                return (Capability & NetworkInterfaceCapabilities.RssCapable) > 0;
            }
        }

        public bool IsRdmaCapable
        {
            get
            {
                return (Capability & NetworkInterfaceCapabilities.RdmaCapable) > 0;
            }
        }

        /// <summary>
        /// Reads a single NETWORK_INTERFACE_INFO entry (without the leading 4-byte Next field,
        /// which the caller uses to locate successive entries - see QueryNetworkInterfaceInfoResponse).
        /// </summary>
        public static NetworkInterfaceInfo Read(byte[] buffer, int offset)
        {
            NetworkInterfaceInfo result = new NetworkInterfaceInfo();
            result.IfIndex = LittleEndianConverter.ToUInt32(buffer, offset + 0);
            result.Capability = (NetworkInterfaceCapabilities)LittleEndianConverter.ToUInt32(buffer, offset + 4);
            result.Reserved = LittleEndianConverter.ToUInt32(buffer, offset + 8);
            result.LinkSpeed = LittleEndianConverter.ToUInt64(buffer, offset + 12);
            result.IPAddress = ReadSockAddrStorage(buffer, offset + 20);
            return result;
        }

        /// <summary>
        /// [MS-SMB2] 2.2.32.5.1.1 sockaddr_storage - only the AF_INET / AF_INET6 shapes that
        /// Windows and Samba actually put on the wire are parsed; any other family yields null.
        /// </summary>
        private static IPAddress ReadSockAddrStorage(byte[] buffer, int offset)
        {
            ushort family = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            if (family == AF_INET)
            {
                byte[] addressBytes = ByteReader.ReadBytes(buffer, offset + 4, 4);
                return new IPAddress(addressBytes);
            }
            else if (family == AF_INET6)
            {
                byte[] addressBytes = ByteReader.ReadBytes(buffer, offset + 8, 16);
                uint scopeId = LittleEndianConverter.ToUInt32(buffer, offset + 24);
                return new IPAddress(addressBytes, scopeId);
            }
            return null;
        }
    }
}
