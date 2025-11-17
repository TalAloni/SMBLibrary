using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsClientFactoryTests
    {
        private class FakeFileStore : ISMBFileStore
        {
            public bool CreateFileCalled;

            public NTStatus StatusToReturn = NTStatus.STATUS_SUCCESS;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                CreateFileCalled = true;
                handle = new object();
                fileStatus = FileStatus.FILE_OPENED;
                return StatusToReturn;
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

            public NTStatus QueryDirectory(out System.Collections.Generic.List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass)
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
                output = null;
                throw new NotImplementedException();
            }

            public NTStatus Disconnect()
            {
                throw new NotImplementedException();
            }

            public uint MaxReadSize
            {
                get { return 0; }
            }

            public uint MaxWriteSize
            {
                get { return 0; }
            }
        }

        [TestMethod]
        public void CreateDfsAwareFileStore_WhenOptionsNull_ReturnsInnerStore()
        {
            FakeFileStore inner = new FakeFileStore();

            ISMBFileStore result = DfsClientFactory.CreateDfsAwareFileStore(inner, null, null);

            Assert.AreSame(inner, result);
        }

        [TestMethod]
        public void CreateDfsAwareFileStore_WhenDfsDisabled_ReturnsInnerStore()
        {
            FakeFileStore inner = new FakeFileStore();
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = false;

            ISMBFileStore result = DfsClientFactory.CreateDfsAwareFileStore(inner, null, options);

            Assert.AreSame(inner, result);
        }
    }
}
