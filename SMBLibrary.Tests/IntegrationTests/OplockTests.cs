/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Client;
using SMBLibrary.Server;
using SMBLibrary.SMB2;
using System;
using System.Collections.Generic;
using System.Net;

namespace SMBLibrary.Tests.IntegrationTests
{
    [TestClass]
    public class OplockTests
    {
        private static Random s_seedGenerator = new Random();

        private int m_serverPort;
        private SMBServer m_server;

        [TestInitialize]
        public void Initialize()
        {
            m_serverPort = 1000 + new Random(s_seedGenerator.Next()).Next(50000);

            SMBShareCollection shares = new SMBShareCollection();
            shares.Add(new FileSystemShare("Shared", new DummyStore()));
            
            IGSSMechanism gssMechanism = new IndependentNTLMAuthenticationProvider((username) => "password");
            GSSProvider gssProvider = new GSSProvider(gssMechanism);
            // Grant Oplocks
            SMBServerOptions options = new SMBServerOptions();
            options.AlwaysGrantReadOplock = true;
            m_server = new SMBServer(shares, gssProvider, options);
            m_server.Start(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, false, true, false, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_server.Stop();
        }

        [TestMethod]
        public void When_OplockRequestedAndAlwaysGrantReadOplockIsEnabled_OplockIsGranted()
        {
            // Arrange
            SMB2Client client = new SMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
            client.Login("", "John", "password");
            
            NTStatus status;
            SMB2FileStore fileStore = (SMB2FileStore)client.TreeConnect("Shared", out status);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

            // Act
            
            // We use a manual CreateRequest to verify the OplockLevel in the response
            CreateRequest request = new CreateRequest();
            request.Name = "test.txt";
            request.DesiredAccess = AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE;
            request.FileAttributes = FileAttributes.Normal;
            request.ShareAccess = ShareAccess.Read;
            request.CreateDisposition = CreateDisposition.FILE_OPEN;
            request.CreateOptions = CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT;
            request.RequestedOplockLevel = OplockLevel.Level2;
            request.Header.TreeID = ((uint)typeof(SMB2FileStore).GetField("m_treeID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(fileStore));

            client.TrySendCommand(request);
            CreateResponse response = (CreateResponse)client.WaitForCommand(request.MessageID);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, response.Header.Status);
            Assert.AreEqual(OplockLevel.Level2, response.OplockLevel);
            
            client.Logoff();
            client.Disconnect();
        }

        [TestMethod]
        public void When_OplockNotRequested_OplockIsNotGranted()
        {
            // Arrange
            SMB2Client client = new SMB2Client();
            client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort);
            client.Login("", "John", "password");
            
            NTStatus status;
            SMB2FileStore fileStore = (SMB2FileStore)client.TreeConnect("Shared", out status);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

            // Act
            
            CreateRequest request = new CreateRequest();
            request.Name = "test.txt";
            request.DesiredAccess = AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE;
            request.FileAttributes = FileAttributes.Normal;
            request.ShareAccess = ShareAccess.Read;
            request.CreateDisposition = CreateDisposition.FILE_OPEN;
            request.CreateOptions = CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT;
            request.RequestedOplockLevel = OplockLevel.None;
            request.Header.TreeID = ((uint)typeof(SMB2FileStore).GetField("m_treeID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(fileStore));

            client.TrySendCommand(request);
            CreateResponse response = (CreateResponse)client.WaitForCommand(request.MessageID);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, response.Header.Status);
            Assert.AreEqual(OplockLevel.None, response.OplockLevel);
            
            client.Logoff();
            client.Disconnect();
        }
    }

    public class DummyStore : INTFileStore
    {
        public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
        {
            handle = "dummy-handle";
            fileStatus = FileStatus.FILE_OPENED;
            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus CloseFile(object handle) => NTStatus.STATUS_SUCCESS;
        public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount) { data = new byte[0]; return NTStatus.STATUS_SUCCESS; }
        public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data) { numberOfBytesWritten = data.Length; return NTStatus.STATUS_SUCCESS; }
        public NTStatus FlushFileBuffers(object handle) => NTStatus.STATUS_SUCCESS;
        public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock) => NTStatus.STATUS_SUCCESS;
        public NTStatus UnlockFile(object handle, long byteOffset, long length) => NTStatus.STATUS_SUCCESS;
        public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass) { result = new List<QueryDirectoryFileInformation>(); return NTStatus.STATUS_SUCCESS; }
        public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass)
        {
            if (informationClass == FileInformationClass.FileNetworkOpenInformation)
            {
                FileNetworkOpenInformation fileInfo = new FileNetworkOpenInformation();
                fileInfo.CreationTime = DateTime.UtcNow;
                fileInfo.LastAccessTime = DateTime.UtcNow;
                fileInfo.LastWriteTime = DateTime.UtcNow;
                fileInfo.ChangeTime = DateTime.UtcNow;
                fileInfo.AllocationSize = 0;
                fileInfo.EndOfFile = 0;
                fileInfo.FileAttributes = FileAttributes.Normal;
                result = fileInfo;
                return NTStatus.STATUS_SUCCESS;
            }
            result = null;
            return NTStatus.STATUS_NOT_SUPPORTED;
        }
        public NTStatus SetFileInformation(object handle, FileInformation information) => NTStatus.STATUS_SUCCESS;
        public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass) { result = null; return NTStatus.STATUS_NOT_SUPPORTED; }
        public NTStatus GetFileSystemInformation(out FileSystemInformation result, object handle, FileSystemInformationClass informationClass) { result = null; return NTStatus.STATUS_NOT_SUPPORTED; }
        public NTStatus SetFileSystemInformation(FileSystemInformation information) => NTStatus.STATUS_SUCCESS;
        public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation) { result = null; return NTStatus.STATUS_NOT_SUPPORTED; }
        public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor) => NTStatus.STATUS_SUCCESS;
        public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context) { ioRequest = null; return NTStatus.STATUS_NOT_SUPPORTED; }
        public NTStatus Cancel(object ioRequest) => NTStatus.STATUS_SUCCESS;
        public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength) { output = new byte[0]; return NTStatus.STATUS_SUCCESS; }
    }
}
