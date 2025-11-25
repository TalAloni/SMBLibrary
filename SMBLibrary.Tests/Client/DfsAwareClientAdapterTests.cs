using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsAwareClientAdapterTests
    {
        private class FakeResolver : IDfsClientResolver
        {
            public DfsResolutionResult ResultToReturn;
            public int ResolveCallCount = 0;
            public List<string> PathsRequested = new List<string>();

            /// <summary>
            /// If set, returns this result on subsequent calls (for retry scenarios).
            /// </summary>
            public DfsResolutionResult SecondResultToReturn;

            public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
            {
                ResolveCallCount++;
                PathsRequested.Add(originalPath);

                // Return second result on retry calls
                if (SecondResultToReturn != null && ResolveCallCount > 1)
                {
                    return SecondResultToReturn;
                }

                return ResultToReturn;
            }
        }

        private class FakeFileStore : ISMBFileStore
        {
            public string LastPath;
            public string LastQueryFileName;
            public NTStatus StatusToReturn = NTStatus.STATUS_SUCCESS;
            public int CreateFileCallCount = 0;
            public List<string> PathsReceived = new List<string>();

            /// <summary>
            /// If set, the first N calls return STATUS_PATH_NOT_COVERED, then StatusToReturn.
            /// </summary>
            public int ReturnNotCoveredForFirstNCalls = 0;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                CreateFileCallCount++;
                PathsReceived.Add(path);
                LastPath = path;
                handle = new object();
                fileStatus = FileStatus.FILE_OPENED;

                if (ReturnNotCoveredForFirstNCalls > 0 && CreateFileCallCount <= ReturnNotCoveredForFirstNCalls)
                {
                    return NTStatus.STATUS_PATH_NOT_COVERED;
                }

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

        #region STATUS_PATH_NOT_COVERED Retry Tests

        [TestMethod]
        public void CreateFile_WhenPathNotCovered_RetriesWithResolvedPath()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.ReturnNotCoveredForFirstNCalls = 1; // First call fails, second succeeds

            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\domain\dfs\folder";
            string resolvedPath = @"\\server\share\folder";

            // First call returns original (no resolution yet)
            resolver.ResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.NotApplicable,
                ResolvedPath = originalPath,
                OriginalPath = originalPath
            };

            // Second call (after PATH_NOT_COVERED) returns resolved path
            resolver.SecondResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.Success,
                ResolvedPath = resolvedPath,
                OriginalPath = originalPath
            };

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(2, inner.CreateFileCallCount, "Should have retried once");
            Assert.AreEqual(originalPath, inner.PathsReceived[0], "First attempt should use original path");
            Assert.AreEqual(resolvedPath, inner.PathsReceived[1], "Retry should use resolved path");
        }

        [TestMethod]
        public void CreateFile_WhenPathNotCovered_AndResolutionFails_ReturnsError()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.ReturnNotCoveredForFirstNCalls = 10; // Always fails

            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\domain\dfs\folder";

            // Resolution always returns null/failure
            resolver.ResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.Error,
                ResolvedPath = null,
                OriginalPath = originalPath
            };

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            // Assert - Should return the error without infinite retries
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, status);
            Assert.AreEqual(1, inner.CreateFileCallCount, "Should not retry when resolution fails");
        }

        [TestMethod]
        public void CreateFile_WhenPathNotCovered_AndSamePathReturned_StopsRetrying()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.ReturnNotCoveredForFirstNCalls = 10; // Always fails

            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\domain\dfs\folder";

            // Resolution returns same path (no resolution available)
            resolver.ResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.Success,
                ResolvedPath = originalPath,
                OriginalPath = originalPath
            };

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            // Assert - Should not infinitely retry with same path
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, status);
            Assert.AreEqual(1, inner.CreateFileCallCount, "Should not retry when path doesn't change");
        }

        [TestMethod]
        public void CreateFile_WhenPathNotCovered_RespectsMaxRetries()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.ReturnNotCoveredForFirstNCalls = 100; // Always fails

            DfsClientOptions options = new DfsClientOptions();
            string basePath = @"\\domain\dfs\folder";

            // Create a resolver that returns incrementing paths (simulating interlink loops)
            var loopingResolver = new LoopingResolver();

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, loopingResolver, options, maxRetries: 3);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, basePath, 0, 0, 0, 0, 0, null);

            // Assert - Should stop after max retries
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, status);
            Assert.IsTrue(inner.CreateFileCallCount <= 4, "Should stop after max retries (initial + 3 retries)");
        }

        [TestMethod]
        public void CreateFile_WhenSuccessOnFirstTry_NoRetry()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.StatusToReturn = NTStatus.STATUS_SUCCESS;

            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\share\file";

            resolver.ResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.NotApplicable,
                ResolvedPath = originalPath,
                OriginalPath = originalPath
            };

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(1, inner.CreateFileCallCount, "Should only call once on success");
        }

        [TestMethod]
        public void CreateFile_WhenOtherError_NoRetry()
        {
            // Arrange
            FakeFileStore inner = new FakeFileStore();
            inner.StatusToReturn = NTStatus.STATUS_ACCESS_DENIED;

            FakeResolver resolver = new FakeResolver();
            DfsClientOptions options = new DfsClientOptions();
            string originalPath = @"\\server\share\file";

            resolver.ResultToReturn = new DfsResolutionResult
            {
                Status = DfsResolutionStatus.NotApplicable,
                ResolvedPath = originalPath,
                OriginalPath = originalPath
            };

            DfsAwareClientAdapter adapter = new DfsAwareClientAdapter(inner, resolver, options);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = adapter.CreateFile(out handle, out fileStatus, originalPath, 0, 0, 0, 0, 0, null);

            // Assert - Should not retry for non-DFS errors
            Assert.AreEqual(NTStatus.STATUS_ACCESS_DENIED, status);
            Assert.AreEqual(1, inner.CreateFileCallCount, "Should not retry for non-DFS errors");
        }

        #endregion

        /// <summary>
        /// Helper resolver that returns different paths on each call (simulating interlink resolution).
        /// </summary>
        private class LoopingResolver : IDfsClientResolver
        {
            private int _callCount = 0;

            public DfsResolutionResult Resolve(DfsClientOptions options, string originalPath)
            {
                _callCount++;
                return new DfsResolutionResult
                {
                    Status = DfsResolutionStatus.Success,
                    ResolvedPath = originalPath + "_" + _callCount,
                    OriginalPath = originalPath
                };
            }
        }
    }
}
