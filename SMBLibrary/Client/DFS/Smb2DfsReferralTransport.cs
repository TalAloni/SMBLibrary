using System;
using SMBLibrary;
using SMBLibrary.SMB2;

namespace SMBLibrary.Client.DFS
{
    public class Smb2DfsReferralTransport : IDfsReferralTransport
    {
        public delegate NTStatus Smb2IoctlSender(IOCtlRequest request, out byte[] output, out uint outputCount);

        private readonly Smb2IoctlSender _sender;

        public Smb2DfsReferralTransport(Smb2IoctlSender sender)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            _sender = sender;
        }

        public NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount)
        {
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(dfsPath, maxOutputSize);
            return _sender(request, out buffer, out outputCount);
        }

        internal static IDfsReferralTransport CreateUsingDeviceIOControl(INTFileStore fileStore, object handle)
        {
            if (fileStore == null)
            {
                throw new ArgumentNullException("fileStore");
            }

            Smb2IoctlSender sender = delegate (IOCtlRequest request, out byte[] output, out uint outputCount)
            {
                byte[] deviceOutput;
                NTStatus status = fileStore.DeviceIOControl(handle, request.CtlCode, request.Input, out deviceOutput, (int)request.MaxOutputResponse);

                if (deviceOutput != null)
                {
                    output = deviceOutput;
                    outputCount = (uint)deviceOutput.Length;
                }
                else
                {
                    output = null;
                    outputCount = 0;
                }

                return status;
            };

            return new Smb2DfsReferralTransport(sender);
        }
    }
}
