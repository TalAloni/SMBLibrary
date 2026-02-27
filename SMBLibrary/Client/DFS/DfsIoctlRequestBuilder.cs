using System;
using SMBLibrary;
using SMBLibrary.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Helper for constructing DFS-related SMB2 IOCTL requests.
    /// </summary>
    internal static class DfsIoctlRequestBuilder
    {
        /// <summary>
        /// Special FileID used for DFS referral IOCTLs that don't require an open file handle.
        /// Per MS-SMB2 2.2.31, this value (0xFFFFFFFFFFFFFFFF for both parts) indicates
        /// the FSCTL doesn't need an associated file.
        /// </summary>
        public static readonly FileID DfsReferralFileId = new FileID
        {
            Persistent = 0xFFFFFFFFFFFFFFFFUL,
            Volatile = 0xFFFFFFFFFFFFFFFFUL
        };

        /// <summary>
        /// Create an SMB2 IOCTL request for FSCTL_DFS_GET_REFERRALS.
        /// This helper does not send the request; it only sets up the command.
        /// </summary>
        /// <param name="dfsPath">The DFS path to request referrals for (UNC-style).</param>
        /// <param name="maxOutputResponse">Maximum response size the client is prepared to accept.</param>
        internal static IOCtlRequest CreateDfsReferralRequest(string dfsPath, uint maxOutputResponse)
        {
            if (dfsPath == null)
            {
                throw new ArgumentNullException("dfsPath");
            }

            // Build DFSC request payload
            RequestGetDfsReferral dfsRequest = new RequestGetDfsReferral();
            dfsRequest.MaxReferralLevel = 4; // Request V4 referrals for maximum interop
            dfsRequest.RequestFileName = dfsPath;
            byte[] inputBuffer = dfsRequest.GetBytes();

            IOCtlRequest request = new IOCtlRequest();
            request.CtlCode = (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS;
            request.FileId = DfsReferralFileId;
            request.IsFSCtl = true;
            request.MaxOutputResponse = maxOutputResponse;
            request.Input = inputBuffer;
            request.Output = new byte[0];

            return request;
        }

        /// <summary>
        /// Create an SMB2 IOCTL request for FSCTL_DFS_GET_REFERRALS_EX with optional site name.
        /// This helper does not send the request; it only sets up the command.
        /// </summary>
        /// <param name="dfsPath">The DFS path to request referrals for (UNC-style).</param>
        /// <param name="siteName">Optional site name for site-aware referral requests. Pass null if not used.</param>
        /// <param name="maxOutputResponse">Maximum response size the client is prepared to accept.</param>
        internal static IOCtlRequest CreateDfsReferralRequestEx(string dfsPath, string siteName, uint maxOutputResponse)
        {
            if (dfsPath == null)
            {
                throw new ArgumentNullException("dfsPath");
            }

            // Build DFSC extended request payload
            RequestGetDfsReferralEx dfsRequest = new RequestGetDfsReferralEx();
            dfsRequest.MaxReferralLevel = 4; // Request V4 referrals for maximum interop
            dfsRequest.RequestFileName = dfsPath;
            if (!string.IsNullOrEmpty(siteName))
            {
                dfsRequest.Flags = RequestGetDfsReferralExFlags.SiteName;
                dfsRequest.SiteName = siteName;
            }
            byte[] inputBuffer = dfsRequest.GetBytes();

            IOCtlRequest request = new IOCtlRequest();
            request.CtlCode = (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS_EX;
            request.FileId = DfsReferralFileId;
            request.IsFSCtl = true;
            request.MaxOutputResponse = maxOutputResponse;
            request.Input = inputBuffer;
            request.Output = new byte[0];

            return request;
        }
    }
}
