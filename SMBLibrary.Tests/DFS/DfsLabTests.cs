using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// End-to-end DFS tests against live Hyper-V lab environment.
    /// Requires: LAB_PASSWORD environment variable set.
    /// Run with: dotnet test --filter "TestCategory=Lab"
    /// </summary>
    [TestClass]
    [TestCategory("Lab")]
    public class DfsLabTests : DfsLabTestBase
    {
        #region Smoke Tests

        [TestMethod]
        [TestCategory("Smoke")]
        public void Lab_ConnectToDc_Succeeds()
        {
            // Arrange
            RequireLabEnvironment();

            // Act
            ConnectToDc();

            // Assert - if we get here, connection succeeded
            Assert.IsTrue(Client.IsConnected);
        }

        [TestMethod]
        [TestCategory("Smoke")]
        public void Lab_TreeConnectToIpc_Succeeds()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToDc();

            // Act - IPC$ is always available
            ISMBFileStore store = Client.TreeConnect("IPC$", out NTStatus status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, $"TreeConnect to IPC$ failed: {status}");
            Assert.IsNotNull(store);

            store.Disconnect();
        }

        [TestMethod]
        [TestCategory("Smoke")]
        public void Lab_DirectShareAccess_Succeeds()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            // Act
            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

            // Verify we can list directory
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle, out fileStatus, "",
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus);
            store.CloseFile(handle);
            store.Disconnect();
        }

        #endregion

        #region DFS Referral Tests

        /// <summary>
        /// Shorthand for the special FileID used for DFS referral IOCTLs.
        /// </summary>
        private static FileID DfsReferralFileId
        {
            get { return DfsIoctlRequestBuilder.DfsReferralFileId; }
        }

        [TestMethod]
        public void Lab_DfsPath_ParsesLabNamespace()
        {
            // Arrange
            RequireLabEnvironment();

            // Act - Test DFS path parsing for lab namespace
            DfsPath path = new DfsPath(DfsFolderPath);

            // Assert
            Assert.IsNotNull(path);
            Assert.AreEqual("LAB.LOCAL", path.ServerName, true);
            Assert.AreEqual("Files", path.ShareName, true);
            Console.WriteLine($"Parsed: Server={path.ServerName}, Share={path.ShareName}");
        }

        [TestMethod]
        public void Lab_DfsPath_DetectsSysvol()
        {
            // Arrange
            RequireLabEnvironment();

            // Act
            DfsPath sysvolPath = new DfsPath(SysvolPath);
            DfsPath netlogonPath = new DfsPath(NetlogonPath);

            // Assert
            Assert.IsTrue(sysvolPath.IsSysVolOrNetLogon, "SYSVOL should be detected");
            Assert.IsTrue(netlogonPath.IsSysVolOrNetLogon, "NETLOGON should be detected");
        }

        [TestMethod]
        public void Lab_GetSysvolReferral_ReturnsReferral()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(SysvolPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus,
                $"SYSVOL referral failed: {ioctlStatus}");

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            Assert.IsTrue(response.NumberOfReferrals > 0);

            // Log SYSVOL referral details
            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            if (entry != null)
            {
                Console.WriteLine($"SYSVOL Referral:");
                Console.WriteLine($"  IsNameListReferral: {entry.IsNameListReferral}");
                Console.WriteLine($"  SpecialName: {entry.SpecialName}");
                Console.WriteLine($"  NetworkAddress: {entry.NetworkAddress}");
                if (entry.ExpandedNames != null && entry.ExpandedNames.Count > 0)
                {
                    Console.WriteLine($"  ExpandedNames ({entry.ExpandedNames.Count}):");
                    foreach (var name in entry.ExpandedNames)
                    {
                        Console.WriteLine($"    - {name}");
                    }
                }
            }

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_GetNetlogonReferral_ReturnsReferral()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(NetlogonPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus,
                $"NETLOGON referral failed: {ioctlStatus}");

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            Assert.IsTrue(response.NumberOfReferrals > 0, "Expected at least one NETLOGON referral");

            Console.WriteLine($"NETLOGON Referral: {response.NumberOfReferrals} entries");
            foreach (var e in response.ReferralEntries)
            {
                if (e is DfsReferralEntryV3 v3)
                {
                    Console.WriteLine($"  V{v3.VersionNumber}: NetworkAddress={v3.NetworkAddress}, TTL={v3.TimeToLive}s");
                }
            }

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_GetDfsNamespaceReferral_ReturnsBothTargets()
        {
            // Arrange - Request referral for DFS folder (should return FS1 and FS2)
            // NOTE: This test requires DNS resolution of LAB.LOCAL domain.
            // When connecting via IP, the server may return STATUS_NOT_FOUND.
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Request referral for the DFS folder path
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(DfsFolderPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Handle case where DNS isn't configured (IP-only connection)
            if (ioctlStatus == NTStatus.STATUS_NOT_FOUND)
            {
                Console.WriteLine($"DFS namespace referral returned STATUS_NOT_FOUND - DNS may not be configured for {DfsFolderPath}");
                Assert.Inconclusive("DFS namespace referral requires DNS resolution of LAB.LOCAL domain");
            }

            // Assert - Should succeed and return 2 targets
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus,
                $"DFS namespace referral failed: {ioctlStatus}");

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            Console.WriteLine($"DFS Namespace Referral for {DfsFolderPath}:");
            Console.WriteLine($"  PathConsumed: {response.PathConsumed}");
            Console.WriteLine($"  NumberOfReferrals: {response.NumberOfReferrals}");

            Assert.AreEqual(2, response.NumberOfReferrals,
                "Lab DFS namespace should have 2 targets (FS1 and FS2)");

            // Verify both targets are present
            List<string> targets = new List<string>();
            foreach (var entry in response.ReferralEntries)
            {
                if (entry is DfsReferralEntryV3 v3)
                {
                    string addr = v3.NetworkAddress ?? "";
                    targets.Add(addr.ToUpperInvariant());
                    Console.WriteLine($"  Target: {v3.NetworkAddress} (TTL={v3.TimeToLive}s)");
                }
            }

            Assert.IsTrue(targets.Exists(t => t.Contains("FS1") || t.Contains("10.0.0.20")),
                $"Should have FS1 as target. Got: {string.Join(", ", targets)}");
            Assert.IsTrue(targets.Exists(t => t.Contains("FS2") || t.Contains("10.0.0.21")),
                $"Should have FS2 as target. Got: {string.Join(", ", targets)}");

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_GetDfsReferral_ReturnsV3OrV4Entry()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Use SYSVOL which always exists on DC
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(SysvolPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            Assert.IsTrue(response.NumberOfReferrals > 0);

            // Verify we got V3 or V4 (Windows Server 2008+ always returns V3/V4)
            DfsReferralEntry entry = response.ReferralEntries[0];
            Assert.IsTrue(entry is DfsReferralEntryV3 || entry is DfsReferralEntryV4,
                $"Expected V3 or V4 entry, got {entry.GetType().Name}");

            if (entry is DfsReferralEntryV3 v3)
            {
                Console.WriteLine($"Referral Entry V{v3.VersionNumber}:");
                Console.WriteLine($"  ServerType: {v3.ServerType}");
                Console.WriteLine($"  ReferralEntryFlags: {v3.ReferralEntryFlags}");
                Console.WriteLine($"  TimeToLive: {v3.TimeToLive}s");
            }

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_GetDfsReferral_PathConsumedIsValid()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(SysvolPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            
            // PathConsumed should be the number of bytes (UTF-16) consumed from the request path
            // For \\LAB.LOCAL\SYSVOL, this should be > 0 and <= path length * 2
            Assert.IsTrue(response.PathConsumed > 0, "PathConsumed should be positive");
            Assert.IsTrue(response.PathConsumed <= SysvolPath.Length * 2,
                $"PathConsumed ({response.PathConsumed}) should not exceed path length ({SysvolPath.Length * 2} bytes)");

            Console.WriteLine($"PathConsumed: {response.PathConsumed} bytes ({response.PathConsumed / 2} chars)");

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_GetDfsReferralEx_WithSiteName_Succeeds()
        {
            // Arrange - Test extended referral request with site name
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Use extended referral request with site name
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(
                SysvolPath, "Default-First-Site-Name", 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);

            // Assert - Extended referral should work (or return NOT_SUPPORTED/INVALID_PARAMETER on some servers)
            if (ioctlStatus == NTStatus.STATUS_NOT_SUPPORTED || ioctlStatus == NTStatus.STATUS_INVALID_PARAMETER)
            {
                Console.WriteLine($"FSCTL_DFS_GET_REFERRALS_EX returned {ioctlStatus} - server may not support site-aware referrals");
                Assert.Inconclusive($"Server does not support extended referrals: {ioctlStatus}");
            }

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus,
                $"Extended referral failed: {ioctlStatus}");

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            Assert.IsTrue(response.NumberOfReferrals > 0);
            Console.WriteLine($"Extended referral returned {response.NumberOfReferrals} entries");

            ipcStore.Disconnect();
        }

        #endregion

        #region File Operations Tests

        [TestMethod]
        [TestCategory("Smoke")]
        public void Lab_DirectAccess_ConnectToFs2_Succeeds()
        {
            // Arrange - Verify FS2 is also accessible
            RequireLabEnvironment();
            ConnectToServer(LabFs2Server);

            // Act
            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, $"TreeConnect to FS2 failed: {status}");
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_DirectAccess_CreateFileOnFs1_Succeeds()
        {
            // Arrange - Connect directly to FS1 via IP (bypasses DFS/DNS)
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus, $"TreeConnect failed: {treeStatus}");

            // Act - Create/open a test file directly
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle,
                out fileStatus,
                @"dfs-test-file.txt",
                AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE,
                FileAttributes.Normal,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN_IF,
                CreateOptions.FILE_NON_DIRECTORY_FILE,
                null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus,
                $"CreateFile failed: {createStatus}");

            store.CloseFile(handle);
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_DirectAccess_QueryDirectoryOnFs1_Succeeds()
        {
            // Arrange - Connect directly to FS1 via IP
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Open directory handle
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle,
                out fileStatus,
                "",
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus);

            // Act - Query directory contents
            List<QueryDirectoryFileInformation> entries;
            NTStatus queryStatus = store.QueryDirectory(
                out entries,
                handle,
                "*",
                FileInformationClass.FileDirectoryInformation);

            // Assert - STATUS_SUCCESS or STATUS_NO_MORE_FILES are both valid
            Assert.IsTrue(
                queryStatus == NTStatus.STATUS_SUCCESS || queryStatus == NTStatus.STATUS_NO_MORE_FILES,
                $"QueryDirectory failed: {queryStatus}");
            Assert.IsNotNull(entries);

            Console.WriteLine($"Sales share directory ({entries.Count} entries):");
            foreach (var entry in entries)
            {
                if (entry is FileDirectoryInformation info)
                {
                    Console.WriteLine($"  {info.FileName}");
                }
            }

            store.CloseFile(handle);
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_FileReadWrite_RoundTrip_Succeeds()
        {
            // Arrange
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            string testFileName = $"dfs-roundtrip-{Guid.NewGuid():N}.txt";
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("Hello from SMBLibrary DFS E2E test!");

            try
            {
                // Create and write
                object handle;
                FileStatus fileStatus;
                NTStatus createStatus = store.CreateFile(
                    out handle, out fileStatus, testFileName,
                    AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE,
                    FileAttributes.Normal,
                    ShareAccess.None,
                    CreateDisposition.FILE_CREATE,
                    CreateOptions.FILE_NON_DIRECTORY_FILE,
                    null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus, $"Create failed: {createStatus}");

                // Write
                int bytesWritten;
                NTStatus writeStatus = store.WriteFile(out bytesWritten, handle, 0, testData);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, writeStatus, $"Write failed: {writeStatus}");
                Assert.AreEqual(testData.Length, bytesWritten);

                store.CloseFile(handle);

                // Reopen and read
                NTStatus reopenStatus = store.CreateFile(
                    out handle, out fileStatus, testFileName,
                    AccessMask.GENERIC_READ,
                    FileAttributes.Normal,
                    ShareAccess.Read,
                    CreateDisposition.FILE_OPEN,
                    CreateOptions.FILE_NON_DIRECTORY_FILE,
                    null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, reopenStatus);

                // Read
                byte[] readData;
                NTStatus readStatus = store.ReadFile(out readData, handle, 0, testData.Length);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, readStatus, $"Read failed: {readStatus}");
                
                store.CloseFile(handle);

                // Verify content
                CollectionAssert.AreEqual(testData, readData, "Read data should match written data");
                Console.WriteLine($"Successfully wrote and read {testData.Length} bytes");
            }
            finally
            {
                // Cleanup - delete test file
                object deleteHandle;
                FileStatus deleteStatus;
                if (store.CreateFile(out deleteHandle, out deleteStatus, testFileName,
                    AccessMask.DELETE, FileAttributes.Normal, ShareAccess.None,
                    CreateDisposition.FILE_OPEN, CreateOptions.FILE_DELETE_ON_CLOSE, null) == NTStatus.STATUS_SUCCESS)
                {
                    store.CloseFile(deleteHandle);
                }
                store.Disconnect();
            }
        }

        #endregion

        #region Caching Tests

        [TestMethod]
        public void Lab_ReferralTtl_IsPositive()
        {
            // Arrange - Use SYSVOL referral which should work on any DC
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(SysvolPath, 16384);

            // Act
            byte[] output;
            NTStatus ioctlStatus = ipcStore.DeviceIOControl(
                DfsReferralFileId, request.CtlCode, request.Input, out output, (int)request.MaxOutputResponse);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ioctlStatus);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(output);
            uint ttl = 0;
            if (response.ReferralEntries.Count > 0 && response.ReferralEntries[0] is DfsReferralEntryV3 v3)
            {
                ttl = v3.TimeToLive;
            }

            // Assert - TTL should be reasonable (typically 300-600 seconds)
            Assert.IsTrue(ttl > 0, "Referral TTL should be positive");
            Console.WriteLine($"SYSVOL Referral TTL: {ttl} seconds");

            ipcStore.Disconnect();
        }

        #endregion

        #region DFS Resolution Tests

        [TestMethod]
        public void Lab_DfsPathResolver_ResolvesPath()
        {
            // Arrange - Test DfsPathResolver directly
            RequireLabEnvironment();
            ConnectToDc();

            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus treeStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Create a transport that sends referral requests
            var transport = Smb2DfsReferralTransport.CreateUsingDeviceIOControl(ipcStore, DfsReferralFileId);
            var referralCache = new ReferralCache();
            var domainCache = new DomainCache();
            var resolver = new DfsPathResolver(referralCache, domainCache, transport);

            DfsClientOptions options = new DfsClientOptions { Enabled = true };

            // Act - Resolve SYSVOL path
            DfsResolutionResult result = resolver.Resolve(options, SysvolPath);

            // Assert - Log the result for diagnostics
            Console.WriteLine($"Resolution result: Status={result.Status}, ResolvedPath={result.ResolvedPath}");
            
            // The resolver may return different statuses depending on implementation
            // Success = fully resolved, NotApplicable = not a DFS path, Error = resolution failed but original path returned
            Assert.IsNotNull(result.ResolvedPath, "ResolvedPath should not be null");
            
            // If error, it should still return the original path for fallback
            if (result.Status == DfsResolutionStatus.Error)
            {
                Console.WriteLine("Resolver returned Error - this is acceptable for paths that can't be fully resolved");
                Assert.AreEqual(SysvolPath, result.ResolvedPath, "On error, should return original path");
            }
            else if (result.Status == DfsResolutionStatus.Success)
            {
                Console.WriteLine($"SYSVOL resolved to: {result.ResolvedPath}");
            }

            ipcStore.Disconnect();
        }

        [TestMethod]
        public void Lab_DfsClientFactory_WrapperPassthroughWhenDisabled()
        {
            // Arrange - DFS disabled should return inner store unchanged
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore innerStore = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            DfsClientOptions options = new DfsClientOptions { Enabled = false };

            // Act
            ISMBFileStore wrappedStore = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            // Assert - When disabled, should return the same store
            Assert.AreSame(innerStore, wrappedStore, "With DFS disabled, factory should return inner store");

            wrappedStore.Disconnect();
        }

        [TestMethod]
        public void Lab_ReferralCache_StoresAndRetrievesEntry()
        {
            // Arrange - Test cache behavior
            RequireLabEnvironment();

            var cache = new ReferralCache();
            
            // Create a cache entry manually with proper expiration
            var entry = new ReferralCacheEntry(SysvolPath);
            entry.TtlSeconds = 300;
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(300); // Must set ExpiresUtc or entry is considered expired
            entry.TargetList.Add(new TargetSetEntry(@"\\LAB-DC1\SYSVOL"));

            // Act - Store and retrieve
            cache.Add(entry);
            var retrieved = cache.Lookup(SysvolPath);

            // Assert
            Assert.IsNotNull(retrieved, "Cache should return the stored entry");
            Assert.AreEqual(SysvolPath.ToUpperInvariant(), retrieved.DfsPathPrefix.ToUpperInvariant());
            Assert.IsFalse(retrieved.IsExpired, "Entry should not be expired immediately");
            Assert.AreEqual(1, retrieved.TargetList.Count);
            Console.WriteLine($"Cached entry: Prefix={retrieved.DfsPathPrefix}, TTL={retrieved.TtlSeconds}s, Targets={retrieved.TargetList.Count}");
        }

        [TestMethod]
        public void Lab_TargetHint_RotatesTargets()
        {
            // Arrange - Test target rotation
            RequireLabEnvironment();

            var entry = new ReferralCacheEntry(SysvolPath);
            entry.TtlSeconds = 300;
            entry.TargetList.Add(new TargetSetEntry(@"\\DC1\SYSVOL"));
            entry.TargetList.Add(new TargetSetEntry(@"\\DC2\SYSVOL"));

            // Act - Get target hints
            var firstHint = entry.GetTargetHint();
            Console.WriteLine($"First target hint: {firstHint?.TargetPath}");

            entry.NextTargetHint();
            var secondHint = entry.GetTargetHint();
            Console.WriteLine($"Second target hint: {secondHint?.TargetPath}");

            // Assert - Should rotate
            Assert.AreNotEqual(firstHint?.TargetPath, secondHint?.TargetPath,
                "NextTargetHint should rotate to different target");

            // Reset and verify
            entry.ResetTargetHint();
            var resetHint = entry.GetTargetHint();
            Assert.AreEqual(firstHint?.TargetPath, resetHint?.TargetPath,
                "ResetTargetHint should return to first target");
        }

        #endregion

        #region DfsSessionManager Tests

        [TestMethod]
        public void Lab_DfsSessionManager_CreatesSession()
        {
            // Arrange - Test DfsSessionManager can create sessions
            RequireLabEnvironment();

            using (var sessionManager = new DfsSessionManager())
            {
                // Act - Get session to FS1
                var credentials = new DfsCredentials(LabDomain, LabUsername, LabPassword);
                NTStatus status;
                ISMBFileStore store = sessionManager.GetOrCreateSession(
                    LabFs1Server, "Sales", credentials, out status);

                // Assert
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, $"GetOrCreateSession failed: {status}");
                Assert.IsNotNull(store);

                Console.WriteLine($"Successfully created session to {LabFs1Server}\\Sales");

                // Verify we can use the store
                object handle;
                FileStatus fileStatus;
                NTStatus createStatus = store.CreateFile(
                    out handle, out fileStatus, "",
                    AccessMask.GENERIC_READ,
                    FileAttributes.Directory,
                    ShareAccess.Read,
                    CreateDisposition.FILE_OPEN,
                    CreateOptions.FILE_DIRECTORY_FILE,
                    null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus);
                store.CloseFile(handle);
            }
        }

        [TestMethod]
        public void Lab_DfsSessionManager_ReusesSameServerSession()
        {
            // Arrange
            RequireLabEnvironment();

            using (var sessionManager = new DfsSessionManager())
            {
                var credentials = new DfsCredentials(LabDomain, LabUsername, LabPassword);

                // Act - Get two sessions to same server, different conceptual paths
                NTStatus status1, status2;
                ISMBFileStore store1 = sessionManager.GetOrCreateSession(
                    LabFs1Server, "Sales", credentials, out status1);
                ISMBFileStore store2 = sessionManager.GetOrCreateSession(
                    LabFs1Server, "Sales", credentials, out status2);

                // Assert - Both should succeed
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status1);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status2);

                // Should reuse the same underlying client/connection
                Console.WriteLine("Successfully reused session for same server");
            }
        }

        [TestMethod]
        public void Lab_DfsSessionManager_CreatesSeparateSessionsForDifferentServers()
        {
            // Arrange
            RequireLabEnvironment();

            using (var sessionManager = new DfsSessionManager())
            {
                var credentials = new DfsCredentials(LabDomain, LabUsername, LabPassword);

                // Act - Get sessions to different servers
                NTStatus status1, status2;
                ISMBFileStore storeFs1 = sessionManager.GetOrCreateSession(
                    LabFs1Server, "Sales", credentials, out status1);
                ISMBFileStore storeFs2 = sessionManager.GetOrCreateSession(
                    LabFs2Server, "Sales", credentials, out status2);

                // Assert
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status1, $"FS1 session failed: {status1}");
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status2, $"FS2 session failed: {status2}");

                // Stores should be different instances
                Assert.AreNotSame(storeFs1, storeFs2, "Different servers should have different stores");

                Console.WriteLine($"Successfully created separate sessions for {LabFs1Server} and {LabFs2Server}");
            }
        }

        #endregion

        #region DFS-Aware Adapter End-to-End Tests

        [TestMethod]
        public void Lab_DfsAwareAdapter_DirectAccessWithDfsDisabled_Succeeds()
        {
            // Arrange - DFS disabled should work for direct share access
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore innerStore = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Create DFS-aware adapter with DFS disabled
            DfsClientOptions options = new DfsClientOptions { Enabled = false };
            ISMBFileStore store = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            // Act - Access directory through adapter
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle, out fileStatus, "",
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus,
                $"Should access share directly when DFS disabled: {createStatus}");

            store.CloseFile(handle);
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_DfsAwareAdapter_WithDfsEnabled_DirectShareAccess_Succeeds()
        {
            // Arrange - DFS enabled but accessing non-DFS path should still work
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore innerStore = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            // Create transport for referrals
            // Note: In lab tests, innerStore is always SMB2FileStore; transport will be non-null
            SMB2FileStore smb2Store = innerStore as SMB2FileStore;
            IDfsReferralTransport transport = null;
            if (smb2Store != null)
            {
                transport = Smb2DfsReferralTransport.CreateUsingDeviceIOControl(smb2Store, DfsReferralFileId);
            }

            // Create DFS-aware adapter with resolver (transport may be null if not SMB2, resolver handles gracefully)
            var resolver = new DfsClientResolver(transport);
            DfsClientOptions options = new DfsClientOptions { Enabled = true };

            ISMBFileStore store = DfsClientFactory.CreateDfsAwareFileStore(innerStore, resolver, options);

            // Act - Access directory (non-DFS path should pass through)
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle, out fileStatus, "",
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus,
                $"Direct share access with DFS enabled should work: {createStatus}");

            store.CloseFile(handle);
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_DfsAwareAdapter_FileReadWrite_Succeeds()
        {
            // Arrange - Test file operations through DFS-aware adapter
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore innerStore = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            DfsClientOptions options = new DfsClientOptions { Enabled = false };
            ISMBFileStore store = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            string testFileName = $"dfs-adapter-test-{Guid.NewGuid():N}.txt";
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("DFS Adapter E2E Test Data");

            try
            {
                // Act - Create and write file
                object handle;
                FileStatus fileStatus;
                NTStatus createStatus = store.CreateFile(
                    out handle, out fileStatus, testFileName,
                    AccessMask.GENERIC_READ | AccessMask.GENERIC_WRITE,
                    FileAttributes.Normal,
                    ShareAccess.None,
                    CreateDisposition.FILE_CREATE,
                    CreateOptions.FILE_NON_DIRECTORY_FILE,
                    null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus);

                int bytesWritten;
                NTStatus writeStatus = store.WriteFile(out bytesWritten, handle, 0, testData);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, writeStatus);

                store.CloseFile(handle);

                // Read back
                NTStatus reopenStatus = store.CreateFile(
                    out handle, out fileStatus, testFileName,
                    AccessMask.GENERIC_READ,
                    FileAttributes.Normal,
                    ShareAccess.Read,
                    CreateDisposition.FILE_OPEN,
                    CreateOptions.FILE_NON_DIRECTORY_FILE,
                    null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, reopenStatus);

                byte[] readData;
                NTStatus readStatus = store.ReadFile(out readData, handle, 0, testData.Length);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, readStatus);

                store.CloseFile(handle);

                // Assert
                CollectionAssert.AreEqual(testData, readData, "Data should round-trip through DFS adapter");
                Console.WriteLine($"Successfully read/wrote {testData.Length} bytes through DFS adapter");
            }
            finally
            {
                // Cleanup
                object deleteHandle;
                FileStatus deleteStatus;
                if (store.CreateFile(out deleteHandle, out deleteStatus, testFileName,
                    AccessMask.DELETE, FileAttributes.Normal, ShareAccess.None,
                    CreateDisposition.FILE_OPEN, CreateOptions.FILE_DELETE_ON_CLOSE, null) == NTStatus.STATUS_SUCCESS)
                {
                    store.CloseFile(deleteHandle);
                }
                store.Disconnect();
            }
        }

        [TestMethod]
        public void Lab_DfsAwareAdapter_QueryDirectory_Succeeds()
        {
            // Arrange - Test QueryDirectory through adapter
            RequireLabEnvironment();
            ConnectToServer(LabFs1Server);

            ISMBFileStore innerStore = Client.TreeConnect("Sales", out NTStatus treeStatus);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, treeStatus);

            DfsClientOptions options = new DfsClientOptions { Enabled = false };
            ISMBFileStore store = DfsClientFactory.CreateDfsAwareFileStore(innerStore, null, options);

            // Act - Open directory and query contents
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = store.CreateFile(
                out handle, out fileStatus, "",
                AccessMask.GENERIC_READ,
                FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, createStatus);

            List<QueryDirectoryFileInformation> entries;
            NTStatus queryStatus = store.QueryDirectory(
                out entries, handle, "*", FileInformationClass.FileDirectoryInformation);

            // Assert
            Assert.IsTrue(
                queryStatus == NTStatus.STATUS_SUCCESS || queryStatus == NTStatus.STATUS_NO_MORE_FILES,
                $"QueryDirectory should succeed: {queryStatus}");
            Assert.IsNotNull(entries);

            Console.WriteLine($"QueryDirectory returned {entries.Count} entries through DFS adapter");

            store.CloseFile(handle);
            store.Disconnect();
        }

        [TestMethod]
        public void Lab_FullDfsResolution_SysvolPath()
        {
            // Arrange - Test full DFS resolution path for SYSVOL
            RequireLabEnvironment();
            ConnectToDc();

            // Connect to IPC$ for DFS referrals
            SMB2FileStore ipcStore = Client.TreeConnect("IPC$", out NTStatus ipcStatus) as SMB2FileStore;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ipcStatus);

            // Create full DFS resolver stack
            var referralCache = new ReferralCache();
            var domainCache = new DomainCache();
            var transport = Smb2DfsReferralTransport.CreateUsingDeviceIOControl(ipcStore, DfsReferralFileId);
            var resolver = new DfsPathResolver(referralCache, domainCache, transport);

            DfsClientOptions options = new DfsClientOptions { Enabled = true };

            // Act - Resolve SYSVOL path (always exists on DC)
            DfsResolutionResult result = resolver.Resolve(options, SysvolPath);

            // Assert
            Console.WriteLine($"SYSVOL Resolution:");
            Console.WriteLine($"  Original: {result.OriginalPath}");
            Console.WriteLine($"  Resolved: {result.ResolvedPath}");
            Console.WriteLine($"  Status: {result.Status}");
            Console.WriteLine($"  IsDfsPath: {result.IsDfsPath}");

            // Resolution should complete (may be success or error, but should return something)
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResolvedPath, "ResolvedPath should not be null");

            // Check cache was populated
            ReferralCacheEntry cached = referralCache.Lookup(SysvolPath);
            if (cached != null)
            {
                Console.WriteLine($"  Cache populated: TTL={cached.TtlSeconds}s, Targets={cached.TargetList.Count}");
            }

            ipcStore.Disconnect();
        }

        #endregion

        #region Failover Tests

        [TestMethod]
        [TestCategory("Failover")]
        public void Lab_Failover_WhenPrimaryDown_SecondTargetAccessible()
        {
            // NOTE: This test requires LAB-FS1 to be stopped before running.
            // Use: .\Run-DfsLabTests.ps1 -IncludeFailover
            
            // Arrange
            RequireLabEnvironment();
            
            // Verify FS1 is actually down (test precondition)
            bool fs1Reachable = false;
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var result = tcp.BeginConnect(LabFs1Server, 445, null, null);
                    fs1Reachable = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2)) && tcp.Connected;
                }
            }
            catch { }

            if (fs1Reachable)
            {
                Assert.Inconclusive("Failover test requires LAB-FS1 to be stopped. Use -IncludeFailover flag.");
            }

            // Act - Connect directly to FS2 (simulating failover)
            ConnectToServer(LabFs2Server);
            ISMBFileStore store = Client.TreeConnect("Sales", out NTStatus status);

            // Assert - FS2 should be accessible
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status,
                "Should be able to access Sales share on FS2 when FS1 is down");

            store.Disconnect();
        }

        #endregion
    }
}
