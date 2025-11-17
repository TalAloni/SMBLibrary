using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsFileStoreFactoryTests
    {
        private class FakeFileStore : INTFileStore
        {
            public string LastCreatePath;
            public object LastDeviceIoControlHandle;
            public uint LastCtlCode;
            public byte[] LastInput;
            public int LastMaxOutputLength;

            public NTStatus CreateStatusToReturn = NTStatus.STATUS_SUCCESS;
            public NTStatus DeviceIoControlStatusToReturn = NTStatus.STATUS_FS_DRIVER_REQUIRED;
            public byte[] DeviceIoControlOutputToReturn;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                LastCreatePath = path;
                handle = new object();
                fileStatus = FileStatus.FILE_OPENED;
                return CreateStatusToReturn;
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
                LastDeviceIoControlHandle = handle;
                LastCtlCode = ctlCode;
                LastInput = input;
                LastMaxOutputLength = maxOutputLength;
                output = DeviceIoControlOutputToReturn;
                return DeviceIoControlStatusToReturn;
            }
        }

        [TestMethod]
        public void CreateDfsAwareFileStore_WhenServerNotDfsCapable_InvokesDeviceIoControlAndUsesOriginalPath()
        {
            FakeFileStore inner = new FakeFileStore();
            inner.CreateStatusToReturn = NTStatus.STATUS_SUCCESS;
            inner.DeviceIoControlStatusToReturn = NTStatus.STATUS_FS_DRIVER_REQUIRED;
            inner.DeviceIoControlOutputToReturn = null;

            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            object dfsHandle = new object();
            INTFileStore dfsAware = DfsFileStoreFactory.CreateDfsAwareFileStore(inner, dfsHandle, options);

            string originalPath = "\\\\server\\share\\path";
            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsAware.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(originalPath, inner.LastCreatePath);

            Assert.AreEqual(dfsHandle, inner.LastDeviceIoControlHandle);
            Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS, inner.LastCtlCode);
            Assert.AreEqual(0, inner.LastMaxOutputLength); // we currently pass 0 for maxOutputSize in resolver
            Assert.IsNotNull(inner.LastInput);
            Assert.IsTrue(inner.LastInput.Length > 0);
        }
    }
}
