/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DFSC] 2.2.4 RESP_GET_DFS_REFERRAL - ReferralHeaderFlags
    /// </summary>
    [Flags]
    public enum DfsReferralHeaderFlags : uint
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// R bit - The server returns referral servers and not storage servers.
        /// </summary>
        ReferralServers = 0x00000001,

        /// <summary>
        /// S bit - The server returns storage servers and not referral servers.
        /// </summary>
        StorageServers = 0x00000002,

        /// <summary>
        /// T bit - The server supports target failback.
        /// </summary>
        TargetFailback = 0x00000004,
    }
}
