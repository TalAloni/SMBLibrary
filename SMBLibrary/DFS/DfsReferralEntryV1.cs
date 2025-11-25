/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DFSC] 2.2.4.1 DFS_REFERRAL_V1
    /// </summary>
    public class DfsReferralEntryV1 : DfsReferralEntry
    {
        public ushort VersionNumber { get; set; }
        public ushort Size { get; set; }
        public DfsServerType ServerType { get; set; }
        public DfsReferralEntryFlags ReferralEntryFlags { get; set; }
        public uint TimeToLive { get; set; }
        public string DfsPath { get; set; }
        public string NetworkAddress { get; set; }
    }
}
