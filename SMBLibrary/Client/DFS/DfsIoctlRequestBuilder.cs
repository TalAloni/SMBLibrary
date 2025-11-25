using System;
using SMBLibrary;
using SMBLibrary.SMB2;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Helper for constructing DFS-related SMB2 IOCTL requests.
    /// </summary>
    internal static class DfsIoctlRequestBuilder
    {
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
            request.FileId = new FileID
            {
                Persistent = 0xFFFFFFFFFFFFFFFFUL,
                Volatile = 0xFFFFFFFFFFFFFFFFUL,
            };
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
            dfsRequest.SiteName = siteName;
            byte[] inputBuffer = dfsRequest.GetBytes();

            IOCtlRequest request = new IOCtlRequest();
            request.CtlCode = (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS_EX;
            request.FileId = new FileID
            {
                Persistent = 0xFFFFFFFFFFFFFFFFUL,
                Volatile = 0xFFFFFFFFFFFFFFFFUL,
            };
            request.IsFSCtl = true;
            request.MaxOutputResponse = maxOutputResponse;
            request.Input = inputBuffer;
            request.Output = new byte[0];

            return request;
        }
    }
}
