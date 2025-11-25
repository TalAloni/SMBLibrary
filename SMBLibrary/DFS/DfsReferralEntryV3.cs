/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DFSC] 2.2.4.3 DFS_REFERRAL_V3
    /// V3 supports both normal referrals and NameListReferrals (for SYSVOL/NETLOGON).
    /// </summary>
    public class DfsReferralEntryV3 : DfsReferralEntry
    {
        public ushort VersionNumber { get; set; }
        public ushort Size { get; set; }
        public DfsServerType ServerType { get; set; }
        public DfsReferralEntryFlags ReferralEntryFlags { get; set; }
        public uint TimeToLive { get; set; }

        // Normal referral fields (when IsNameListReferral is false)
        public string DfsPath { get; set; }
        public string DfsAlternatePath { get; set; }
        public string NetworkAddress { get; set; }

        // NameListReferral fields (when IsNameListReferral is true)
        public Guid ServiceSiteGuid { get; set; }
        public string SpecialName { get; set; }
        public List<string> ExpandedNames { get; set; }

        /// <summary>
        /// Returns true if this is a NameListReferral (used for SYSVOL/NETLOGON DC lists).
        /// </summary>
        public bool IsNameListReferral
        {
            get { return (ReferralEntryFlags & DfsReferralEntryFlags.NameListReferral) != 0; }
        }
    }
}
