using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Integration tests for DFS client functionality.
    /// These tests validate end-to-end DFS resolution flows using fakes.
    /// </summary>
    [TestClass]
    public class DfsIntegrationTests
    {
        #region DfsClientFactory Integration Tests

        [TestMethod]
        public void DfsClientFactory_WhenDfsEnabled_ReturnsWrappedStore()
        {
            // Arrange
            FakeFileStore innerStore = new FakeFileStore();
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            // Act
            ISMBFileStore result = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(innerStore, result, "Should return a wrapped store when DFS is enabled");
        }

        [TestMethod]
        public void DfsClientFactory_WhenDfsDisabled_ReturnsOriginalStore()
        {
            // Arrange
            FakeFileStore innerStore = new FakeFileStore();
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = false;

            // Act
            ISMBFileStore result = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            // Assert
            Assert.AreSame(innerStore, result, "Should return original store when DFS is disabled");
        }

        [TestMethod]
        public void DfsClientFactory_WhenOptionsNull_ReturnsOriginalStore()
        {
            // Arrange
            FakeFileStore innerStore = new FakeFileStore();

            // Act
            ISMBFileStore result = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, null);

            // Assert
            Assert.AreSame(innerStore, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DfsClientFactory_WhenInnerStoreNull_ThrowsArgumentNullException()
        {
            // Act
            DfsClientFactory.CreateDfsAwareFileStore(null, null, new DfsClientOptions());
        }

        #endregion

        #region DfsPathResolver End-to-End Tests

        [TestMethod]
        public void DfsPathResolver_EndToEnd_CachesAndReusesReferrals()
        {
            // Arrange - Pre-populate cache to test cache hit path
            ReferralCache cache = new ReferralCache();
            DomainCache domainCache = new DomainCache();

            // Create a cache entry for the path prefix
            SMBLibrary.ReferralCacheEntry cacheEntry = new SMBLibrary.ReferralCacheEntry(@"\\server\share");
            cacheEntry.TtlSeconds = 300;
            cacheEntry.ExpiresUtc = DateTime.UtcNow.AddSeconds(300);
            cacheEntry.TargetList.Add(new TargetSetEntry(@"\\target\share"));
            cache.Add(cacheEntry);

            int transportCallCount = 0;
            FakeTransport transport = new FakeTransport(delegate(string server, string path, uint flags, out byte[] buffer, out uint outputCount)
            {
                transportCallCount++;
                buffer = null;
                outputCount = 0;
                return NTStatus.STATUS_SUCCESS;
            });

            DfsPathResolver resolver = new DfsPathResolver(cache, domainCache, transport);
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            // Act - Both resolutions should use cached entry (prefix match)
            DfsResolutionResult result1 = resolver.Resolve(options, @"\\server\share\file.txt");
            DfsResolutionResult result2 = resolver.Resolve(options, @"\\server\share\other.txt");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.Success, result1.Status);
            Assert.AreEqual(DfsResolutionStatus.Success, result2.Status);
            Assert.AreEqual(0, transportCallCount, "Should not call transport when cache has matching entry");
        }

        [TestMethod]
        public void DfsPathResolver_EndToEnd_HandlesServerNotDfsCapable()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            DomainCache domainCache = new DomainCache();
            FakeTransport transport = new FakeTransport(delegate(string server, string path, uint flags, out byte[] buffer, out uint outputCount)
            {
                buffer = null;
                outputCount = 0;
                return NTStatus.STATUS_FS_DRIVER_REQUIRED;
            });

            DfsPathResolver resolver = new DfsPathResolver(cache, domainCache, transport);
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            // Act
            DfsResolutionResult result = resolver.Resolve(options, @"\\server\share\file.txt");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(@"\\server\share\file.txt", result.ResolvedPath);
            Assert.IsFalse(result.IsDfsPath);
        }

        [TestMethod]
        public void DfsPathResolver_EndToEnd_SkipsIpcPaths()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            DomainCache domainCache = new DomainCache();
            int transportCallCount = 0;
            FakeTransport transport = new FakeTransport(delegate(string server, string path, uint flags, out byte[] buffer, out uint outputCount)
            {
                transportCallCount++;
                buffer = null;
                outputCount = 0;
                return NTStatus.STATUS_SUCCESS;
            });

            DfsPathResolver resolver = new DfsPathResolver(cache, domainCache, transport);
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            // Act
            DfsResolutionResult result = resolver.Resolve(options, @"\\server\IPC$");

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(0, transportCallCount, "Should not call transport for IPC$ paths");
        }

        [TestMethod]
        public void DfsPathResolver_EndToEnd_RaisesAllEvents()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            DomainCache domainCache = new DomainCache();
            FakeTransport transport = new FakeTransport(delegate(string server, string path, uint flags, out byte[] buffer, out uint outputCount)
            {
                buffer = BuildV1ReferralResponse(@"\\server\share", @"\\target\share", 300);
                outputCount = (uint)buffer.Length;
                return NTStatus.STATUS_SUCCESS;
            });

            DfsPathResolver resolver = new DfsPathResolver(cache, domainCache, transport);
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            bool startedRaised = false;
            bool requestedRaised = false;
            bool receivedRaised = false;
            bool completedRaised = false;

            resolver.ResolutionStarted += (s, e) => startedRaised = true;
            resolver.ReferralRequested += (s, e) => requestedRaised = true;
            resolver.ReferralReceived += (s, e) => receivedRaised = true;
            resolver.ResolutionCompleted += (s, e) => completedRaised = true;

            // Act
            resolver.Resolve(options, @"\\server\share\file.txt");

            // Assert
            Assert.IsTrue(startedRaised, "ResolutionStarted should be raised");
            Assert.IsTrue(requestedRaised, "ReferralRequested should be raised");
            Assert.IsTrue(receivedRaised, "ReferralReceived should be raised");
            Assert.IsTrue(completedRaised, "ResolutionCompleted should be raised");
        }

        #endregion

        #region DfsSessionManager Integration Tests

        [TestMethod]
        public void DfsSessionManager_EndToEnd_ManagesMultipleServers()
        {
            // Arrange
            List<string> connectedServers = new List<string>();
            SmbClientFactory factory = delegate(string serverName)
            {
                connectedServers.Add(serverName);
                FakeSmbClient client = new FakeSmbClient();
                client.ConnectResult = true;
                client.LoginResult = NTStatus.STATUS_SUCCESS;
                return client;
            };

            DfsSessionManager manager = new DfsSessionManager(factory);
            DfsCredentials credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status1;
            NTStatus status2;
            NTStatus status3;
            manager.GetOrCreateSession("server1", "share1", credentials, out status1);
            manager.GetOrCreateSession("server2", "share1", credentials, out status2);
            manager.GetOrCreateSession("server1", "share2", credentials, out status3);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status1);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status2);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status3);
            Assert.AreEqual(2, connectedServers.Count, "Should only connect to 2 unique servers");
            Assert.IsTrue(connectedServers.Contains("server1"));
            Assert.IsTrue(connectedServers.Contains("server2"));
        }

        #endregion

        #region Helper Methods

        private static byte[] BuildV1ReferralResponse(string dfsPath, string networkAddress, uint ttl)
        {
            // Build a minimal V1 referral response
            // Header: PathConsumed(2) + NumberOfReferrals(2) + HeaderFlags(4) = 8 bytes
            // V1 Entry: VersionNumber(2) + Size(2) + ServerType(2) + ReferralEntryFlags(2) + 
            //           Reserved(4) + DfsPathOffset(2) + NetworkAddressOffset(2) + TimeToLive(4) = 20 bytes
            // Strings follow after entry

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int headerSize = 8;
            int entrySize = 20;
            int stringsOffset = headerSize + entrySize;
            int totalSize = stringsOffset + dfsPathBytes.Length + networkAddressBytes.Length;

            byte[] buffer = new byte[totalSize];

            // Header
            ushort pathConsumed = (ushort)(dfsPath.Length * 2);
            buffer[0] = (byte)(pathConsumed & 0xFF);
            buffer[1] = (byte)((pathConsumed >> 8) & 0xFF);
            buffer[2] = 1; // NumberOfReferrals = 1
            buffer[3] = 0;
            // HeaderFlags = 0
            buffer[4] = 0;
            buffer[5] = 0;
            buffer[6] = 0;
            buffer[7] = 0;

            // V1 Entry
            int entryOffset = headerSize;
            buffer[entryOffset + 0] = 1; // VersionNumber = 1
            buffer[entryOffset + 1] = 0;
            buffer[entryOffset + 2] = (byte)(entrySize & 0xFF); // Size
            buffer[entryOffset + 3] = (byte)((entrySize >> 8) & 0xFF);
            buffer[entryOffset + 4] = 0; // ServerType = 0 (NonRoot)
            buffer[entryOffset + 5] = 0;
            buffer[entryOffset + 6] = 0; // ReferralEntryFlags = 0
            buffer[entryOffset + 7] = 0;
            // Reserved (4 bytes) = 0

            // DfsPathOffset (relative to entry start)
            ushort dfsPathOffset = (ushort)(entrySize);
            buffer[entryOffset + 12] = (byte)(dfsPathOffset & 0xFF);
            buffer[entryOffset + 13] = (byte)((dfsPathOffset >> 8) & 0xFF);

            // NetworkAddressOffset (relative to entry start)
            ushort networkAddressOffset = (ushort)(entrySize + dfsPathBytes.Length);
            buffer[entryOffset + 14] = (byte)(networkAddressOffset & 0xFF);
            buffer[entryOffset + 15] = (byte)((networkAddressOffset >> 8) & 0xFF);

            // TimeToLive
            buffer[entryOffset + 16] = (byte)(ttl & 0xFF);
            buffer[entryOffset + 17] = (byte)((ttl >> 8) & 0xFF);
            buffer[entryOffset + 18] = (byte)((ttl >> 16) & 0xFF);
            buffer[entryOffset + 19] = (byte)((ttl >> 24) & 0xFF);

            // Strings
            Array.Copy(dfsPathBytes, 0, buffer, stringsOffset, dfsPathBytes.Length);
            Array.Copy(networkAddressBytes, 0, buffer, stringsOffset + dfsPathBytes.Length, networkAddressBytes.Length);

            return buffer;
        }

        #endregion

        #region Helper Classes

        private delegate NTStatus TransportDelegate(string serverName, string path, uint flags, out byte[] buffer, out uint outputCount);

        private class FakeTransport : IDfsReferralTransport
        {
            private readonly TransportDelegate _handler;

            public FakeTransport(TransportDelegate handler)
            {
                _handler = handler;
            }

            public NTStatus TryGetReferrals(string serverName, string path, uint flags, out byte[] buffer, out uint outputCount)
            {
                return _handler(serverName, path, flags, out buffer, out outputCount);
            }
        }

        private class FakeFileStore : ISMBFileStore
        {
            public NTStatus DeviceIoControlStatus = NTStatus.STATUS_FS_DRIVER_REQUIRED;
            public byte[] DeviceIoControlOutput = null;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                handle = new object();
                fileStatus = FileStatus.FILE_OPENED;
                return NTStatus.STATUS_SUCCESS;
            }

            public NTStatus CloseFile(object handle) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount) { data = new byte[0]; return NTStatus.STATUS_SUCCESS; }
            public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data) { numberOfBytesWritten = 0; return NTStatus.STATUS_SUCCESS; }
            public NTStatus FlushFileBuffers(object handle) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus UnlockFile(object handle, long byteOffset, long length) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass) { result = new List<QueryDirectoryFileInformation>(); return NTStatus.STATUS_SUCCESS; }
            public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetFileInformation(object handle, FileInformation information) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetFileSystemInformation(FileSystemInformation information) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context) { ioRequest = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus Cancel(object ioRequest) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
            {
                output = DeviceIoControlOutput;
                return DeviceIoControlStatus;
            }
            public NTStatus Disconnect() { return NTStatus.STATUS_SUCCESS; }
            public uint MaxReadSize { get { return 65536; } }
            public uint MaxWriteSize { get { return 65536; } }
        }

        private class FakeSmbClient : ISMBClient
        {
            public bool ConnectResult { get; set; }
            public NTStatus LoginResult { get; set; }

            public bool Connect(string serverName, SMBTransportType transport) { return ConnectResult; }
            public bool Connect(System.Net.IPAddress serverAddress, SMBTransportType transport) { return ConnectResult; }
            public void Disconnect() { }
            public NTStatus Login(string domainName, string userName, string password) { return LoginResult; }
            public NTStatus Login(string domainName, string userName, string password, AuthenticationMethod authenticationMethod) { return LoginResult; }
            public NTStatus Logoff() { return NTStatus.STATUS_SUCCESS; }
            public List<string> ListShares(out NTStatus status) { status = NTStatus.STATUS_SUCCESS; return new List<string>(); }
            public ISMBFileStore TreeConnect(string shareName, out NTStatus status)
            {
                status = NTStatus.STATUS_SUCCESS;
                return new FakeFileStore();
            }
            public NTStatus Echo() { return NTStatus.STATUS_SUCCESS; }
            public uint MaxReadSize { get { return 65536; } }
            public uint MaxWriteSize { get { return 65536; } }
            public bool IsConnected { get { return ConnectResult; } }
        }

        #endregion
    }
}
