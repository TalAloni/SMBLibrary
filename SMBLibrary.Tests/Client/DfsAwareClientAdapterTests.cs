using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsAwareClientAdapterTests
    {
        private class FakeResolver : IDfsClientResolver
        {
            public DfsResolutionResult ResultToReturn;

            public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
            {
                return ResultToReturn;
            }
        }

        private class FakeFileStore : INTFileStore
        {
            public string LastPath;
            public string LastQueryFileName;
            public NTStatus StatusToReturn = NTStatus.STATUS_SUCCESS;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                LastPath = path;
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
                throw new NotImplementedException();
            }

            public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
            {
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
                LastQueryFileName = fileName;
                result = new List<QueryDirectoryFileInformation>();
                return StatusToReturn;
            }

            public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
            {
                throw new NotImplementedException();
            }

            public NTStatus SetFileInformation(object handle, FileInformation information)
            {
                throw new NotImplementedException();
            }

            public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
            {
                throw new NotImplementedException();
            }

            public NTStatus SetFileSystemInformation(FileSystemInformation information)
            {
                throw new NotImplementedException();
            }

            public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation)
            {
                throw new NotImplementedException();
            }

            public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor)
            {
                throw new NotImplementedException();
            }

            public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context)
            {
                throw new NotImplementedException();
            }

            public NTStatus Cancel(object ioRequest)
            {
                throw new NotImplementedException();
            }

            public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void CreateFile_WhenResolverReturnsNotApplicable_UsesOriginalPath()
        {
            FakeFileStore inner = new FakeFileStore();
            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\\share\\path";

            resolver.ResultToReturn = new DfsResolutionResult();
            resolver.ResultToReturn.Status = DfsResolutionStatus.NotApplicable;
            resolver.ResultToReturn.ResolvedPath = originalPath;
            resolver.ResultToReturn.OriginalPath = originalPath;

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            Assert.AreEqual(originalPath, inner.LastPath);
            Assert.AreEqual(inner.StatusToReturn, status);
        }

        [TestMethod]
        public void CreateFile_WhenResolverReturnsSuccess_UsesResolvedPath()
        {
            FakeFileStore inner = new FakeFileStore();
            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\\share\\path";
            string resolvedPath = @"\\target\\share\\path";

            resolver.ResultToReturn = new DfsResolutionResult();
            resolver.ResultToReturn.Status = DfsResolutionStatus.Success;
            resolver.ResultToReturn.ResolvedPath = resolvedPath;
            resolver.ResultToReturn.OriginalPath = originalPath;

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            object handle;
            FileStatus fileStatus;
            adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            Assert.AreEqual(resolvedPath, inner.LastPath);
        }

        [TestMethod]
        public void QueryDirectory_WhenResolverReturnsNotApplicable_UsesOriginalFileName()
        {
            FakeFileStore inner = new FakeFileStore();
            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\\share\\dir\\*";

            resolver.ResultToReturn = new DfsResolutionResult();
            resolver.ResultToReturn.Status = DfsResolutionStatus.NotApplicable;
            resolver.ResultToReturn.ResolvedPath = originalPath;
            resolver.ResultToReturn.OriginalPath = originalPath;

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            List<QueryDirectoryFileInformation> result;
            NTStatus status = adapter.QueryDirectory(out result, null, originalPath, FileInformationClass.FileDirectoryInformation);

            Assert.AreEqual(originalPath, inner.LastQueryFileName);
            Assert.AreEqual(inner.StatusToReturn, status);
        }

        [TestMethod]
        public void QueryDirectory_WhenResolverReturnsSuccess_UsesResolvedPath()
        {
            FakeFileStore inner = new FakeFileStore();
            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\\share\\dir\\*";
            string resolvedPath = @"\\target\\share\\dir\\*";

            resolver.ResultToReturn = new DfsResolutionResult();
            resolver.ResultToReturn.Status = DfsResolutionStatus.Success;
            resolver.ResultToReturn.ResolvedPath = resolvedPath;
            resolver.ResultToReturn.OriginalPath = originalPath;

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            List<QueryDirectoryFileInformation> result;
            adapter.QueryDirectory(out result, null, originalPath, FileInformationClass.FileDirectoryInformation);

            Assert.AreEqual(resolvedPath, inner.LastQueryFileName);
        }
    }
}
