/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

namespace SMBLibrary
{
    /// <summary>
    /// Indicates whether a referral cache entry is for a DFS root or a DFS link.
    /// Per MS-DFSC 3.1.5.1 referral cache.
    /// </summary>
    public enum ReferralCacheEntryType
    {
        /// <summary>
        /// The entry is for a DFS root (namespace).
        /// </summary>
        Root = 0,

        /// <summary>
        /// The entry is for a DFS link (folder redirect).
        /// </summary>
        Link = 1
    }
}
