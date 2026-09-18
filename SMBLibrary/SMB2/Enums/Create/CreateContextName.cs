/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

namespace SMBLibrary.SMB2
{
    /// <summary>
    /// [MS-SMB2] 2.2.13.2 - The names of the create contexts defined by the specification.
    /// </summary>
    public static class CreateContextName
    {
        /// <summary>SMB2_CREATE_EA_BUFFER</summary>
        public const string ExtendedAttributes = "ExtA";

        /// <summary>SMB2_CREATE_SD_BUFFER</summary>
        public const string SecurityDescriptor = "SecD";

        /// <summary>SMB2_CREATE_DURABLE_HANDLE_REQUEST</summary>
        public const string DurableHandleRequest = "DHnQ";

        /// <summary>SMB2_CREATE_DURABLE_HANDLE_RECONNECT</summary>
        public const string DurableHandleReconnect = "DHnC";

        /// <summary>SMB2_CREATE_ALLOCATION_SIZE</summary>
        public const string AllocationSize = "AlSi";

        /// <summary>SMB2_CREATE_QUERY_MAXIMAL_ACCESS_REQUEST</summary>
        public const string QueryMaximalAccess = "MxAc";

        /// <summary>
        /// SMB2_CREATE_TIMEWARP_TOKEN. Opens the version of the file that existed at the given
        /// timestamp, i.e. the copy held by the matching shadow copy (previous version) of the share.
        /// </summary>
        public const string TimeWarpToken = "TWrp";

        /// <summary>SMB2_CREATE_QUERY_ON_DISK_ID</summary>
        public const string QueryOnDiskID = "QFid";

        /// <summary>SMB2_CREATE_REQUEST_LEASE</summary>
        public const string RequestLease = "RqLs";
    }
}
