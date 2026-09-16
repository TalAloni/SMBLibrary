/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using SMBLibrary.NetworkInterface;
using SMBLibrary.SMB2;

namespace SMBLibrary.Client.NetworkInterface
{
    /// <summary>
    /// Client-side helper for the FSCTL_QUERY_NETWORK_INTERFACE_INFO request ([MS-SMB2] 2.2.32.5),
    /// used by a SMB3 Multichannel-capable client to discover which additional server network
    /// interfaces / addresses it may open bound channels to (see SMB2Client.EstablishAdditionalChannel).
    /// </summary>
    public class NetworkInterfaceInfoHelper
    {
        // [MS-SMB2] 3.3.5.15: FSCTL_QUERY_NETWORK_INTERFACE_INFO requests MUST have FileId set to
        // 0xFFFFFFFFFFFFFFFF (like FSCTL_VALIDATE_NEGOTIATE_INFO / FSCTL_PIPE_WAIT).
        public static readonly FileID NetworkInterfaceInfoFileId = new FileID()
        {
            Persistent = 0xFFFFFFFFFFFFFFFF,
            Volatile = 0xFFFFFFFFFFFFFFFF
        };

        public static readonly int MaxOutputBufferSize = 65536;

        public static NTStatus QueryNetworkInterfaceInfo(ISMBFileStore fileStore, out QueryNetworkInterfaceInfoResponse response)
        {
            response = null;
            byte[] outputBytes;
            NTStatus status = fileStore.DeviceIOControl(NetworkInterfaceInfoFileId, (uint)IoControlCode.FSCTL_QUERY_NETWORK_INTERFACE_INFO, null, out outputBytes, MaxOutputBufferSize);

            if (status == NTStatus.STATUS_SUCCESS && outputBytes != null)
            {
                response = new QueryNetworkInterfaceInfoResponse(outputBytes);
            }

            return status;
        }
    }
}
