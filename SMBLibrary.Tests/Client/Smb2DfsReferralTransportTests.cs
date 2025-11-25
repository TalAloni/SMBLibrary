using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class Smb2DfsReferralTransportTests
    {
        private class FakeFileStore : INTFileStore
        {
            public object LastHandle;
            public uint LastCtlCode;
            public byte[] LastInput;
            public int LastMaxOutputLength;
            public NTStatus StatusToReturn = NTStatus.STATUS_SUCCESS;
            public byte[] OutputToReturn;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                handle = null;
                fileStatus = FileStatus.FILE_DOES_NOT_EXIST;
                throw new NotImplementedException();
            }

            public NTStatus CloseFile(object handle)
            {
                throw new NotImplementedException();
            }

            public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
            {
                data = null;
                throw new NotImplementedException();
            }

            public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
            {
                numberOfBytesWritten = 0;
                throw new NotImplementedException();
            }

            public NTStatus FlushFileBuffers(object handle)
            {
                throw new NotImplementedException();
            }

            public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock)
            {
                throw new NotImplementedException();
            }

            public NTStatus UnlockFile(object handle, long byteOffset, long length)
            {
                throw new NotImplementedException();
            }

            public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass)
            {
                result = null;
                throw new NotImplementedException();
            }

            public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
            {
                result = null;
                throw new NotImplementedException();
            }

            public NTStatus SetFileInformation(object handle, FileInformation information)
            {
                throw new NotImplementedException();
            }

            public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
            {
                result = null;
                throw new NotImplementedException();
            }

            public NTStatus SetFileSystemInformation(FileSystemInformation information)
            {
                throw new NotImplementedException();
            }

            public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation)
            {
                result = null;
                throw new NotImplementedException();
            }

            public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor)
            {
                throw new NotImplementedException();
            }

            public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context)
            {
                ioRequest = null;
                throw new NotImplementedException();
            }

            public NTStatus Cancel(object ioRequest)
            {
                throw new NotImplementedException();
            }

            public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
            {
                LastHandle = handle;
                LastCtlCode = ctlCode;
                LastInput = input;
                LastMaxOutputLength = maxOutputLength;
                output = OutputToReturn;
                return StatusToReturn;
            }
        }

        private class CapturingIoctlSender
        {
            public IOCtlRequest LastRequest;
            public NTStatus StatusToReturn;
            public byte[] OutputToReturn;
            public uint OutputCountToReturn;

            public NTStatus Send(IOCtlRequest request, out byte[] output, out uint outputCount)
            {
                LastRequest = request;
                output = OutputToReturn;
                outputCount = OutputCountToReturn;
                return StatusToReturn;
            }
        }

        [TestMethod]
        public void TryGetReferrals_UsesSenderAndReturnsStatusAndBuffer()
        {
            string dfsPath = "\\\\contoso.com\\Public";
            uint maxOutputSize = 4096;

            CapturingIoctlSender sender = new CapturingIoctlSender();
            sender.StatusToReturn = NTStatus.STATUS_SUCCESS;
            sender.OutputToReturn = new byte[] { 0x01, 0x02, 0x03 };
            sender.OutputCountToReturn = 3;

            Smb2DfsReferralTransport transport = new Smb2DfsReferralTransport(sender.Send);

            byte[] buffer;
            uint outputCount;
            NTStatus status = transport.TryGetReferrals("server", dfsPath, maxOutputSize, out buffer, out outputCount);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsNotNull(buffer);
            Assert.AreEqual(3u, outputCount);
            Assert.AreEqual(3, buffer.Length);
            Assert.AreEqual(0x01, buffer[0]);
            Assert.AreEqual(0x02, buffer[1]);
            Assert.AreEqual(0x03, buffer[2]);

            Assert.IsNotNull(sender.LastRequest);
            Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS, sender.LastRequest.CtlCode);
            Assert.IsTrue(sender.LastRequest.IsFSCtl);
            Assert.AreEqual(maxOutputSize, sender.LastRequest.MaxOutputResponse);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, sender.LastRequest.FileId.Persistent);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, sender.LastRequest.FileId.Volatile);
            Assert.IsNotNull(sender.LastRequest.Input);
            Assert.IsTrue(sender.LastRequest.Input.Length > 0);
        }

        [TestMethod]
        public void CreateUsingDeviceIOControl_UsesDeviceIoControlAndReturnsStatusAndBuffer()
        {
            string dfsPath = "\\\\contoso.com\\Public";
            uint maxOutputSize = 4096;

            FakeFileStore fileStore = new FakeFileStore();
            fileStore.StatusToReturn = NTStatus.STATUS_SUCCESS;
            fileStore.OutputToReturn = new byte[] { 0x10, 0x20 };
            object handle = new object();

            IDfsReferralTransport transport = Smb2DfsReferralTransport.CreateUsingDeviceIOControl(fileStore, handle);

            byte[] buffer;
            uint outputCount;
            NTStatus status = transport.TryGetReferrals("server", dfsPath, maxOutputSize, out buffer, out outputCount);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsNotNull(buffer);
            Assert.AreEqual(2u, outputCount);
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(0x10, buffer[0]);
            Assert.AreEqual(0x20, buffer[1]);

            Assert.AreEqual(handle, fileStore.LastHandle);
            Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS, fileStore.LastCtlCode);
            Assert.AreEqual((int)maxOutputSize, fileStore.LastMaxOutputLength);
            Assert.IsNotNull(fileStore.LastInput);
            Assert.IsTrue(fileStore.LastInput.Length > 0);
        }
    }
}
