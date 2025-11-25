using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsSessionManagerTests
    {
        #region Constructor Tests

        [TestMethod]
        public void Ctor_Default_CreatesInstance()
        {
            // Act
            var manager = new DfsSessionManager();

            // Assert
            Assert.IsNotNull(manager);
        }

        [TestMethod]
        public void Ctor_WithClientFactory_CreatesInstance()
        {
            // Arrange
            SmbClientFactory factory = delegate(string serverName) { return new FakeSmbClient(); };

            // Act
            var manager = new DfsSessionManager(factory);

            // Assert
            Assert.IsNotNull(manager);
        }

        #endregion

        #region GetOrCreateSession Tests

        [TestMethod]
        public void GetOrCreateSession_NewServer_CreatesNewSession()
        {
            // Arrange
            var helper = new FactoryHelper(true, NTStatus.STATUS_SUCCESS);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status;
            ISMBFileStore store = manager.GetOrCreateSession("server1", "share1", credentials, out status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsNotNull(store);
            Assert.AreEqual(1, helper.ClientsCreated);
        }

        [TestMethod]
        public void GetOrCreateSession_SameServerDifferentShare_ReusesClient()
        {
            // Arrange
            var helper = new FactoryHelper(true, NTStatus.STATUS_SUCCESS);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status1;
            NTStatus status2;
            manager.GetOrCreateSession("server1", "share1", credentials, out status1);
            manager.GetOrCreateSession("server1", "share2", credentials, out status2);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status1);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status2);
            Assert.AreEqual(1, helper.ClientsCreated, "Should reuse client for same server");
        }

        [TestMethod]
        public void GetOrCreateSession_DifferentServers_CreatesSeparateClients()
        {
            // Arrange
            var helper = new FactoryHelper(true, NTStatus.STATUS_SUCCESS);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status1;
            NTStatus status2;
            manager.GetOrCreateSession("server1", "share1", credentials, out status1);
            manager.GetOrCreateSession("server2", "share1", credentials, out status2);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status1);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status2);
            Assert.AreEqual(2, helper.ClientsCreated, "Should create separate clients for different servers");
        }

        [TestMethod]
        public void GetOrCreateSession_ConnectionFails_ReturnsError()
        {
            // Arrange
            var helper = new FactoryHelper(false, NTStatus.STATUS_SUCCESS);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status;
            ISMBFileStore store = manager.GetOrCreateSession("server1", "share1", credentials, out status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_BAD_NETWORK_NAME, status);
            Assert.IsNull(store);
        }

        [TestMethod]
        public void GetOrCreateSession_LoginFails_ReturnsError()
        {
            // Arrange
            var helper = new FactoryHelper(true, NTStatus.STATUS_LOGON_FAILURE);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status;
            ISMBFileStore store = manager.GetOrCreateSession("server1", "share1", credentials, out status);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_LOGON_FAILURE, status);
            Assert.IsNull(store);
        }

        [TestMethod]
        public void GetOrCreateSession_CaseInsensitiveServerName_ReusesClient()
        {
            // Arrange
            var helper = new FactoryHelper(true, NTStatus.STATUS_SUCCESS);
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            // Act
            NTStatus status1;
            NTStatus status2;
            manager.GetOrCreateSession("SERVER1", "share1", credentials, out status1);
            manager.GetOrCreateSession("server1", "share2", credentials, out status2);

            // Assert
            Assert.AreEqual(1, helper.ClientsCreated, "Should reuse client for case-insensitive server name match");
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public void Dispose_DisconnectsAllClients()
        {
            // Arrange
            var helper = new MultiClientFactoryHelper();
            var manager = new DfsSessionManager(helper.CreateClient);
            var credentials = new DfsCredentials("DOMAIN", "user", "pass");

            NTStatus ignored;
            manager.GetOrCreateSession("server1", "share1", credentials, out ignored);
            manager.GetOrCreateSession("server2", "share1", credentials, out ignored);

            // Act
            manager.Dispose();

            // Assert
            Assert.IsTrue(helper.Client1.DisconnectCalled, "Client1 should be disconnected");
            Assert.IsTrue(helper.Client2.DisconnectCalled, "Client2 should be disconnected");
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Helper for creating fake SMB clients with configurable behavior.
        /// </summary>
        private class FactoryHelper
        {
            private readonly bool _connectResult;
            private readonly NTStatus _loginResult;

            public int ClientsCreated { get; private set; }

            public FactoryHelper(bool connectResult, NTStatus loginResult)
            {
                _connectResult = connectResult;
                _loginResult = loginResult;
            }

            public ISMBClient CreateClient(string serverName)
            {
                ClientsCreated++;
                FakeSmbClient client = new FakeSmbClient();
                client.ConnectResult = _connectResult;
                client.LoginResult = _loginResult;
                return client;
            }
        }

        /// <summary>
        /// Helper for tracking multiple clients across different servers.
        /// </summary>
        private class MultiClientFactoryHelper
        {
            private int _factoryCalls;

            public FakeSmbClient Client1 { get; private set; }
            public FakeSmbClient Client2 { get; private set; }

            public MultiClientFactoryHelper()
            {
                Client1 = new FakeSmbClient();
                Client1.ConnectResult = true;
                Client1.LoginResult = NTStatus.STATUS_SUCCESS;
                Client2 = new FakeSmbClient();
                Client2.ConnectResult = true;
                Client2.LoginResult = NTStatus.STATUS_SUCCESS;
            }

            public ISMBClient CreateClient(string serverName)
            {
                _factoryCalls++;
                return _factoryCalls == 1 ? Client1 : Client2;
            }
        }

        /// <summary>
        /// Fake ISMBClient for testing without network.
        /// </summary>
        private class FakeSmbClient : ISMBClient
        {
            public bool ConnectResult { get; set; }
            public NTStatus LoginResult { get; set; } = NTStatus.STATUS_SUCCESS;
            public NTStatus TreeConnectResult { get; set; } = NTStatus.STATUS_SUCCESS;
            public bool DisconnectCalled { get; private set; }

            public bool Connect(string serverName, SMBTransportType transport)
            {
                return ConnectResult;
            }

            public bool Connect(IPAddress serverAddress, SMBTransportType transport)
            {
                return ConnectResult;
            }

            public void Disconnect()
            {
                DisconnectCalled = true;
            }

            public NTStatus Login(string domainName, string userName, string password)
            {
                return LoginResult;
            }

            public NTStatus Login(string domainName, string userName, string password, AuthenticationMethod authenticationMethod)
            {
                return LoginResult;
            }

            public NTStatus Logoff()
            {
                return NTStatus.STATUS_SUCCESS;
            }

            public System.Collections.Generic.List<string> ListShares(out NTStatus status)
            {
                status = NTStatus.STATUS_SUCCESS;
                return new System.Collections.Generic.List<string>();
            }

            public ISMBFileStore TreeConnect(string shareName, out NTStatus status)
            {
                status = TreeConnectResult;
                if (status == NTStatus.STATUS_SUCCESS)
                {
                    return new FakeSmbFileStore();
                }
                return null;
            }

            public NTStatus Echo()
            {
                return NTStatus.STATUS_SUCCESS;
            }

            public uint MaxReadSize => 65536;

            public uint MaxWriteSize => 65536;

            public bool IsConnected => ConnectResult;
        }

        /// <summary>
        /// Minimal fake ISMBFileStore for testing.
        /// </summary>
        private class FakeSmbFileStore : ISMBFileStore
        {
            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                handle = new object();
                fileStatus = FileStatus.FILE_CREATED;
                return NTStatus.STATUS_SUCCESS;
            }

            public NTStatus CloseFile(object handle) => NTStatus.STATUS_SUCCESS;
            public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount) { data = new byte[0]; return NTStatus.STATUS_SUCCESS; }
            public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data) { numberOfBytesWritten = 0; return NTStatus.STATUS_SUCCESS; }
            public NTStatus FlushFileBuffers(object handle) => NTStatus.STATUS_SUCCESS;
            public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock) => NTStatus.STATUS_SUCCESS;
            public NTStatus UnlockFile(object handle, long byteOffset, long length) => NTStatus.STATUS_SUCCESS;
            public NTStatus QueryDirectory(out System.Collections.Generic.List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass) { result = new System.Collections.Generic.List<QueryDirectoryFileInformation>(); return NTStatus.STATUS_SUCCESS; }
            public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetFileInformation(object handle, FileInformation information) => NTStatus.STATUS_SUCCESS;
            public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetFileSystemInformation(FileSystemInformation information) => NTStatus.STATUS_SUCCESS;
            public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation) { result = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor) => NTStatus.STATUS_SUCCESS;
            public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context) { ioRequest = null; return NTStatus.STATUS_SUCCESS; }
            public NTStatus Cancel(object ioRequest) => NTStatus.STATUS_SUCCESS;
            public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength) { output = new byte[0]; return NTStatus.STATUS_SUCCESS; }
            public NTStatus Disconnect() => NTStatus.STATUS_SUCCESS;
            public uint MaxReadSize => 65536;
            public uint MaxWriteSize => 65536;
        }

        #endregion
    }
}
