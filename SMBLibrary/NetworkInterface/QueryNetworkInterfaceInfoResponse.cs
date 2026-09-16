/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.NetworkInterface
{
    /// <summary>
    /// [MS-SMB2] 2.2.32.5.1 - Response to a FSCTL_QUERY_NETWORK_INTERFACE_INFO request: a
    /// singly-linked list of NETWORK_INTERFACE_INFO entries, each one preceded by a 4-byte "Next"
    /// field holding the byte offset (relative to the start of that entry) of the next entry, or
    /// 0 for the last one.
    /// </summary>
    public class QueryNetworkInterfaceInfoResponse
    {
        private const int NextFieldLength = 4;

        public List<NetworkInterfaceInfo> Entries;

        public QueryNetworkInterfaceInfoResponse()
        {
            Entries = new List<NetworkInterfaceInfo>();
        }

        public QueryNetworkInterfaceInfoResponse(byte[] buffer)
        {
            Entries = new List<NetworkInterfaceInfo>();
            if (buffer == null || buffer.Length == 0)
            {
                return;
            }

            int entryOffset = 0;
            while (true)
            {
                if (buffer.Length < entryOffset + NextFieldLength + NetworkInterfaceInfo.FixedLength)
                {
                    throw new ArgumentException("Buffer too small for NETWORK_INTERFACE_INFO entry", nameof(buffer));
                }

                uint next = LittleEndianConverter.ToUInt32(buffer, entryOffset);
                Entries.Add(NetworkInterfaceInfo.Read(buffer, entryOffset + NextFieldLength));

                if (next == 0)
                {
                    break;
                }
                entryOffset += (int)next;
            }
        }
    }
}
