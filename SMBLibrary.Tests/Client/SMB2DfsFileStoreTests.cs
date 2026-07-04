/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2DfsFileStoreTests
    {
        [TestMethod]
        public void CreateFile_WhenNotCovered_FollowsReferralToTargetAndRoutesHandle()
        {
            // Arrange: a link referral \SERVER1\DfsRoot\Link -> \SERVER2\Share
            byte[] referralBytes = BuildReferral(@"\SERVER1\DfsRoot\Link", @"\SERVER2\Share");
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_PATH_NOT_COVERED, ReferralResponseBytes = referralBytes };
            FakeFileStore targetStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            Dictionary<string, ISMBFileStore> targets = new Dictionary<string, ISMBFileStore>(StringComparer.OrdinalIgnoreCase)
            {
                { @"SERVER2\Share", targetStore }
            };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, targets);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            // Assert: resolved to the target, remainder path preserved
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsNotNull(handle);
            CollectionAssert.Contains(dfsFileStore.ConnectRequests, @"SERVER2\Share");
            Assert.AreEqual("file.txt", targetStore.LastCreateFilePath);

            // The returned handle must route subsequent operations to the target it was opened against.
            byte[] data;
            dfsFileStore.ReadFile(out data, handle, 0, 3);
            Assert.AreEqual(1, targetStore.ReadFileCount);
            Assert.AreEqual(0, rootStore.ReadFileCount);
        }

        [TestMethod]
        public void CreateFile_WhenCovered_UsesDfsRootStoreWithoutReferral()
        {
            // Arrange
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, new Dictionary<string, ISMBFileStore>());

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"folder\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(0, dfsFileStore.ConnectRequests.Count);
            Assert.AreEqual(@"folder\file.txt", rootStore.LastCreateFilePath);

            byte[] data;
            dfsFileStore.ReadFile(out data, handle, 0, 3);
            Assert.AreEqual(1, rootStore.ReadFileCount);
        }

        [TestMethod]
        public void CreateFile_WhenReferralTargetUnreachable_ReturnsPathNotCovered()
        {
            // Arrange: no target registered => ConnectToTarget returns null.
            byte[] referralBytes = BuildReferral(@"\SERVER1\DfsRoot\Link", @"\SERVER2\Share");
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_PATH_NOT_COVERED, ReferralResponseBytes = referralBytes };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, new Dictionary<string, ISMBFileStore>());

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, status);
            Assert.IsNull(handle);
        }

        [TestMethod]
        public void CreateFile_WhenFirstReferralTargetUnreachable_FailsOverToNextTarget()
        {
            // Arrange: two referral targets, only the second is reachable
            byte[] referralBytes = BuildReferral(@"\SERVER1\DfsRoot\Link", @"\SERVER2\Share", @"\SERVER3\Share");
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_PATH_NOT_COVERED, ReferralResponseBytes = referralBytes };
            FakeFileStore targetStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            Dictionary<string, ISMBFileStore> targets = new Dictionary<string, ISMBFileStore>(StringComparer.OrdinalIgnoreCase)
            {
                { @"SERVER3\Share", targetStore }
            };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, targets);

            // Act
            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            CollectionAssert.Contains(dfsFileStore.ConnectRequests, @"SERVER2\Share");
            CollectionAssert.Contains(dfsFileStore.ConnectRequests, @"SERVER3\Share");
            Assert.AreEqual("file.txt", targetStore.LastCreateFilePath);
        }

        private static byte[] BuildReferral(string dfsPath, params string[] networkAddresses)
        {
            ResponseGetDfsReferral referral = new ResponseGetDfsReferral();
            referral.PathConsumed = (ushort)(dfsPath.Length * 2);
            referral.ReferralHeaderFlags = DfsReferralHeaderFlags.StorageServers;
            foreach (string networkAddress in networkAddresses)
            {
                referral.ReferralEntries.Add(new DfsReferralEntryV4()
                {
                    TimeToLive = 300,
                    ReferralEntryFlags = DfsReferralEntryFlags.None,
                    DfsPath = dfsPath,
                    DfsAlternatePath = dfsPath,
                    NetworkAddress = networkAddress,
                    ServiceSiteGuid = Guid.Empty
                });
            }
            return referral.GetBytes();
        }

        /// <summary>
        /// SMB2DfsFileStore subclass that intercepts target connections so the referral-following logic
        /// can be exercised without a live server.
        /// </summary>
        private class TestableDfsFileStore : SMB2DfsFileStore
        {
            private Dictionary<string, ISMBFileStore> m_targets;
            public List<string> ConnectRequests = new List<string>();

            public TestableDfsFileStore(SMB2Client client, string serverName, string shareName, ISMBFileStore dfsFileStore, Dictionary<string, ISMBFileStore> targets)
                : base(client, serverName, shareName, dfsFileStore)
            {
                m_targets = targets;
            }

            protected override ISMBFileStore ConnectToTarget(string serverName, string shareName)
            {
                string key = serverName + @"\" + shareName;
                ConnectRequests.Add(key);
                ISMBFileStore fileStore;
                if (m_targets.TryGetValue(key, out fileStore))
                {
                    return fileStore;
                }
                return null;
            }
        }

        private class FakeFileStore : ISMBFileStore
        {
            public NTStatus CreateFileStatus = NTStatus.STATUS_SUCCESS;
            public byte[] ReferralResponseBytes;
            public string LastCreateFilePath;
            public int ReadFileCount;

            public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, FileAttributes fileAttributes, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
            {
                LastCreateFilePath = path;
                if (CreateFileStatus == NTStatus.STATUS_SUCCESS)
                {
                    handle = new object();
                    fileStatus = FileStatus.FILE_OPENED;
                    return NTStatus.STATUS_SUCCESS;
                }
                handle = null;
                fileStatus = FileStatus.FILE_DOES_NOT_EXIST;
                return CreateFileStatus;
            }

            public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
            {
                output = ReferralResponseBytes;
                return (ReferralResponseBytes != null) ? NTStatus.STATUS_SUCCESS : NTStatus.STATUS_NOT_SUPPORTED;
            }

            public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
            {
                ReadFileCount++;
                data = new byte[maxCount];
                return NTStatus.STATUS_SUCCESS;
            }

            public NTStatus CloseFile(object handle) { return NTStatus.STATUS_SUCCESS; }
            public NTStatus Disconnect() { return NTStatus.STATUS_SUCCESS; }
            public uint MaxReadSize { get { return 65536; } }
            public uint MaxWriteSize { get { return 65536; } }

            public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data) { throw new NotImplementedException(); }
            public NTStatus FlushFileBuffers(object handle) { throw new NotImplementedException(); }
            public NTStatus LockFile(object handle, long byteOffset, long length, bool exclusiveLock) { throw new NotImplementedException(); }
            public NTStatus UnlockFile(object handle, long byteOffset, long length) { throw new NotImplementedException(); }
            public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass) { throw new NotImplementedException(); }
            public NTStatus GetFileInformation(out FileInformation result, object handle, FileInformationClass informationClass) { throw new NotImplementedException(); }
            public NTStatus SetFileInformation(object handle, FileInformation information) { throw new NotImplementedException(); }
            public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass) { throw new NotImplementedException(); }
            public NTStatus SetFileSystemInformation(FileSystemInformation information) { throw new NotImplementedException(); }
            public NTStatus GetSecurityInformation(out SecurityDescriptor result, object handle, SecurityInformation securityInformation) { throw new NotImplementedException(); }
            public NTStatus SetSecurityInformation(object handle, SecurityInformation securityInformation, SecurityDescriptor securityDescriptor) { throw new NotImplementedException(); }
            public NTStatus NotifyChange(out object ioRequest, object handle, NotifyChangeFilter completionFilter, bool watchTree, int outputBufferSize, OnNotifyChangeCompleted onNotifyChangeCompleted, object context) { throw new NotImplementedException(); }
            public NTStatus Cancel(object ioRequest) { throw new NotImplementedException(); }
        }
    }
}
