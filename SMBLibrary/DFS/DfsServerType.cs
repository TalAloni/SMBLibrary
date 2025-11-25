/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DFSC] 2.2.4.x DFS_REFERRAL_Vx - ServerType
    /// </summary>
    public enum DfsServerType : ushort
    {
        /// <summary>
        /// The target is a non-root DFS server (link target or storage server).
        /// </summary>
        NonRoot = 0x0000,

        /// <summary>
        /// The target is a root DFS server (namespace server).
        /// </summary>
        Root = 0x0001,
    }
}
