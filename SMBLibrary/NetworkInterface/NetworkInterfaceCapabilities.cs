/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;

namespace SMBLibrary.NetworkInterface
{
    /// <summary>
    /// [MS-SMB2] 2.2.32.5.1.1 - Capability field of a NETWORK_INTERFACE_INFO entry.
    /// </summary>
    [Flags]
    public enum NetworkInterfaceCapabilities : uint
    {
        RssCapable = 0x00000001,  // RSS_CAPABLE
        RdmaCapable = 0x00000002, // RDMA_CAPABLE
    }
}
