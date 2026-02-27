using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Unit tests for DfsPathResolver implementing the 14-step MS-DFSC resolution algorithm.
    /// </summary>
    [TestClass]
    public class DfsPathResolverTests
    {
        #region Step 1: Single Component / IPC$ Check

        [TestMethod]
        public void Resolve_SingleComponentPath_ReturnsNotDfsAndOriginalPath()
        {
            // Per MS-DFSC Step 1: If path has only one component, it's not DFS
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\server");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(@"\\server", result.ResolvedPath);
            Assert.IsFalse(result.IsDfsPath);
        }

        [TestMethod]
        public void Resolve_IpcPath_ReturnsNotDfsAndOriginalPath()
        {
            // Per MS-DFSC Step 1: IPC$ paths are not DFS
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\server\IPC$");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(@"\\server\IPC$", result.ResolvedPath);
            Assert.IsFalse(result.IsDfsPath);
        }

        [TestMethod]
        public void Resolve_IpcPathCaseInsensitive_ReturnsNotDfs()
        {
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\server\ipc$\pipe");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.IsFalse(result.IsDfsPath);
        }

        #endregion

        #region DFS Disabled

        [TestMethod]
        public void Resolve_WhenDfsDisabled_ReturnsNotApplicable()
        {
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = false };

            // Act
            var result = resolver.Resolve(options, @"\\domain\share\folder");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(@"\\domain\share\folder", result.ResolvedPath);
        }

        [TestMethod]
        public void Resolve_WhenOptionsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var resolver = new DfsPathResolver();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                resolver.Resolve(null, @"\\server\share"));
        }

        #endregion

        #region Step 2: Cache Lookup

        [TestMethod]
        public void Resolve_CachedRootEntry_ReturnsResolvedPathFromCache()
        {
            // Per MS-DFSC Step 2: Check ReferralCache for matching entry
            // Arrange
            var cache = new ReferralCache();
            var entry = new ReferralCacheEntry(@"\\domain\dfsroot")
            {
                RootOrLink = ReferralCacheEntryType.Root,
                ExpiresUtc = DateTime.UtcNow.AddHours(1) // Not expired
            };
            entry.TargetList.Add(new TargetSetEntry(@"\\server1\share"));
            cache.Add(entry);

            var resolver = new DfsPathResolver(cache, null, null);
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\domain\dfsroot\folder\file.txt");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.Success, result.Status);
            Assert.AreEqual(@"\\server1\share\folder\file.txt", result.ResolvedPath);
            Assert.IsTrue(result.IsDfsPath);
        }

        [TestMethod]
        public void Resolve_CachedLinkEntry_ReturnsResolvedPathFromCache()
        {
            // Per MS-DFSC Step 4: LINK cache hit
            // Arrange
            var cache = new ReferralCache();
            var entry = new ReferralCacheEntry(@"\\domain\dfsroot\link1")
            {
                RootOrLink = ReferralCacheEntryType.Link,
                ExpiresUtc = DateTime.UtcNow.AddHours(1) // Not expired
            };
            entry.TargetList.Add(new TargetSetEntry(@"\\server2\targetshare"));
            cache.Add(entry);

            var resolver = new DfsPathResolver(cache, null, null);
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\domain\dfsroot\link1\subfolder");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.Success, result.Status);
            Assert.AreEqual(@"\\server2\targetshare\subfolder", result.ResolvedPath);
            Assert.IsTrue(result.IsDfsPath);
        }

        [TestMethod]
        public void Resolve_NoMatchingCacheEntry_ProceedsToReferralRequest()
        {
            // Arrange
            var cache = new ReferralCache(); // empty cache
            var fakeTransport = new FakeDfsReferralTransport();
            fakeTransport.SetResponse(NTStatus.STATUS_FS_DRIVER_REQUIRED, null, 0); // server not DFS capable

            var resolver = new DfsPathResolver(cache, null, fakeTransport);
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\server\share\folder");

            // Assert - should have attempted transport call
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.IsTrue(fakeTransport.WasCalled);
        }

        #endregion

        #region Step 12: Not DFS Path

        [TestMethod]
        public void Resolve_ServerNotDfsCapable_ReturnsNotApplicable()
        {
            // Per MS-DFSC Step 12: Server returns STATUS_FS_DRIVER_REQUIRED
            // Arrange
            var fakeTransport = new FakeDfsReferralTransport();
            fakeTransport.SetResponse(NTStatus.STATUS_FS_DRIVER_REQUIRED, null, 0);

            var resolver = new DfsPathResolver(null, null, fakeTransport);
            var options = new DfsClientOptions { Enabled = true };

            // Act
            var result = resolver.Resolve(options, @"\\server\share\folder");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(@"\\server\share\folder", result.ResolvedPath);
            Assert.IsFalse(result.IsDfsPath);
        }

        #endregion

        #region Event Raising

        [TestMethod]
        public void Resolve_RaisesResolutionStartedEvent()
        {
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = true };
            bool eventRaised = false;
            string eventPath = null;

            resolver.ResolutionStarted += (sender, e) =>
            {
                eventRaised = true;
                eventPath = e.Path;
            };

            // Act
            resolver.Resolve(options, @"\\server\share");

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(@"\\server\share", eventPath);
        }

        [TestMethod]
        public void Resolve_RaisesResolutionCompletedEvent()
        {
            // Arrange
            var resolver = new DfsPathResolver();
            var options = new DfsClientOptions { Enabled = true };
            bool eventRaised = false;
            DfsResolutionCompletedEventArgs capturedArgs = null;

            resolver.ResolutionCompleted += (sender, e) =>
            {
                eventRaised = true;
                capturedArgs = e;
            };

            // Act
            resolver.Resolve(options, @"\\server");

            // Assert
            Assert.IsTrue(eventRaised);
            Assert.AreEqual(@"\\server", capturedArgs.OriginalPath);
        }

        #endregion

        #region Helper: Fake Transport

        private class FakeDfsReferralTransport : IDfsReferralTransport
        {
            private NTStatus _status;
            private byte[] _buffer;
            private uint _outputCount;

            public bool WasCalled { get; private set; }
            public string LastPath { get; private set; }

            public void SetResponse(NTStatus status, byte[] buffer, uint outputCount)
            {
                _status = status;
                _buffer = buffer;
                _outputCount = outputCount;
            }

            public NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount)
            {
                WasCalled = true;
                LastPath = dfsPath;
                buffer = _buffer;
                outputCount = _outputCount;
                return _status;
            }
        }

        #endregion
    }
}
