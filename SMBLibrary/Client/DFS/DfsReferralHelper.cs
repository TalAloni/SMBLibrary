/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using SMBLibrary.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Client.DFS
{
    public class DfsReferralHelper
    {
        // MS-SMB2 §2.2.31: sentinel FileId for DFS referral requests
        public static readonly FileID DfsReferralFileId = new FileID()
        {
            Persistent = 0xFFFFFFFFFFFFFFFF,
            Volatile = 0xFFFFFFFFFFFFFFFF
        };

        public static readonly int MaxOutputBufferSize = 8192;

        public static NTStatus GetDfsReferral(ISMBFileStore fileStore, string dfsPath, out ResponseGetDfsReferral referralResponse)
        {
            referralResponse = null;

            // MS-DFSC §2.2.2: request V4 referrals (highest version)
            RequestGetDfsReferral request = new RequestGetDfsReferral();
            request.MaxReferralLevel = 4;
            request.RequestFileName = dfsPath;
            byte[] inputBytes = request.GetBytes();

            byte[] outputBytes;
            NTStatus status = fileStore.DeviceIOControl(DfsReferralFileId, (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS, inputBytes, out outputBytes, MaxOutputBufferSize);

            if (status == NTStatus.STATUS_SUCCESS && outputBytes != null)
            {
                referralResponse = new ResponseGetDfsReferral(outputBytes);
            }

            return status;
        }
    }
}
