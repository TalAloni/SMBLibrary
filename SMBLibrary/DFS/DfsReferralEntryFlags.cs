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
    /// [MS-DFSC] 2.2.4.x DFS_REFERRAL_Vx - ReferralEntryFlags
    /// </summary>
    [Flags]
    public enum DfsReferralEntryFlags : ushort
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// NameListReferral bit - The referral entry is a NameListReferral containing
        /// a list of target names (e.g., domain controller list for SYSVOL/NETLOGON).
        /// </summary>
        NameListReferral = 0x0002,

        /// <summary>
        /// TargetSetBoundary bit (V4 only) - The first target in a target set.
        /// Used for target set grouping in V4 referrals.
        /// </summary>
        TargetSetBoundary = 0x0004,
    }
}
