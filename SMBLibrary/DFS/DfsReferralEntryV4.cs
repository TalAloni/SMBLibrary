/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

namespace SMBLibrary.DFS
{
    /// <summary>
    /// [MS-DFSC] 2.2.5.4 DFS_REFERRAL_V4
    /// V4 is structurally identical to V3 but adds the TargetSetBoundary flag semantics.
    /// The TargetSetBoundary flag (bit 0x0004) indicates the first target in a target set,
    /// allowing clients to group targets for failover purposes.
    /// </summary>
    public class DfsReferralEntryV4 : DfsReferralEntryV3
    {
        public DfsReferralEntryV4()
        {
            VersionNumber = 4;
        }

        public DfsReferralEntryV4(byte[] buffer, ref int offset) : base(buffer, ref offset)
        {
        }

        /// <summary>
        /// Returns true if this entry marks the boundary of a target set (V4 only).
        /// When true, this is the first target in a new target set.
        /// </summary>
        public bool IsTargetSetBoundary
        {
            get { return (ReferralEntryFlags & DfsReferralEntryFlags.TargetSetBoundary) != 0; }
        }
    }
}
