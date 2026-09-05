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
using SMBLibrary.SMB2;

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
            // [MS-SMB2] 2.2.13 - a request against the namespace root is a DFS operation, so the name is subject to
            // DFS name normalization and must be a full path rather than a share-relative one.
            Assert.AreEqual(@"SERVER1\DfsRoot\folder\file.txt", rootStore.LastCreateFilePath);

            byte[] data;
            dfsFileStore.ReadFile(out data, handle, 0, 3);
            Assert.AreEqual(1, rootStore.ReadFileCount);
        }

        [TestMethod]
        public void CreateFile_WhenPathIsShareRoot_SendsSharePathWithoutTrailingSeparator()
        {
            // Opening the share root using a single backslash is the idiom used in ClientExamples.md;
            // String.Empty is the other spelling. Neither may produce a trailing separator.
            foreach (string shareRoot in new string[] { @"\", String.Empty })
            {
                string spelling = (shareRoot.Length == 0) ? "String.Empty" : "a single backslash";
                FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
                TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, new Dictionary<string, ISMBFileStore>());

                object handle;
                FileStatus fileStatus;
                NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, shareRoot, (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, "Share root expressed as " + spelling);
                Assert.AreEqual(@"SERVER1\DfsRoot", rootStore.LastCreateFilePath, "Share root expressed as " + spelling);
            }
        }

        [TestMethod]
        public void CreateFile_WhenPathHasLeadingBackslash_RequestsReferralForTheNormalizedPath()
        {
            // The CREATE name and the referral path are built from the same caller-supplied value. If only the
            // former is normalized the server is asked to resolve \\SERVER1\DfsRoot\\Link\file.txt and the
            // referral fails, so the link is never followed.
            byte[] referralBytes = BuildReferral(@"\SERVER1\DfsRoot\Link", @"\SERVER2\Share");
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_PATH_NOT_COVERED, ReferralResponseBytes = referralBytes };
            FakeFileStore targetStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            Dictionary<string, ISMBFileStore> targets = new Dictionary<string, ISMBFileStore>(StringComparer.OrdinalIgnoreCase)
            {
                { @"SERVER2\Share", targetStore }
            };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, targets);

            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"\Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(@"\\SERVER1\DfsRoot\Link\file.txt", rootStore.LastReferralRequestPath);
            Assert.AreEqual("file.txt", targetStore.LastCreateFilePath);
        }

        [TestMethod]
        public void CreateFile_WhenReferralTargetIsItselfADfsRoot_SendsDfsPathToTheTarget()
        {
            // An interlink: the referral target is a namespace root of its own, so requests against it are DFS
            // operations too and must carry a full path rather than a share-relative one.
            byte[] referralBytes = BuildReferral(@"\SERVER1\DfsRoot\Link", @"\SERVER2\Nested");
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_PATH_NOT_COVERED, ReferralResponseBytes = referralBytes };
            FakeFileStore nestedRootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            // TreeConnect returns an SMB2DfsFileStore when the target share is a namespace root.
            SMB2DfsFileStore nestedDfsStore = new SMB2DfsFileStore(new SMB2Client(), "SERVER2", "Nested", nestedRootStore);
            Dictionary<string, ISMBFileStore> targets = new Dictionary<string, ISMBFileStore>(StringComparer.OrdinalIgnoreCase)
            {
                { @"SERVER2\Nested", nestedDfsStore }
            };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, targets);

            object handle;
            FileStatus fileStatus;
            NTStatus status = dfsFileStore.CreateFile(out handle, out fileStatus, @"Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.AreEqual(@"SERVER2\Nested\file.txt", nestedRootStore.LastCreateFilePath);
        }

        [TestMethod]
        public void SMB2FileStore_ByDefault_IsNotADfsOperation()
        {
            // A share that is not a DFS namespace root is never wrapped, so its store must leave the flag clear
            // and keep sending share-relative names.
            SMB2FileStore fileStore = new SMB2FileStore(new SMB2Client(), 1, false);

            Assert.IsFalse(fileStore.IsDfsOperation);
        }

        [TestMethod]
        public void CreateFile_WhenPathHasLeadingBackslash_DoesNotDoubleSeparator()
        {
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "SERVER1", "DfsRoot", rootStore, new Dictionary<string, ISMBFileStore>());

            object handle;
            FileStatus fileStatus;
            dfsFileStore.CreateFile(out handle, out fileStatus, @"\folder\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            Assert.AreEqual(@"SERVER1\DfsRoot\folder\file.txt", rootStore.LastCreateFilePath);
        }

        [TestMethod]
        public void CreateFile_WhenConnectedByIPAddress_KeepsPathShareRelative()
        {
            // [MS-DFSC] The server normalizes a DFS path against the namespace name, which an IP address can never
            // match. Sending a DFS path here would break callers that connect by address to a share that happens to
            // carry SMB2_SHAREFLAG_DFS_ROOT and work today.
            FakeFileStore rootStore = new FakeFileStore() { CreateFileStatus = NTStatus.STATUS_SUCCESS };
            TestableDfsFileStore dfsFileStore = new TestableDfsFileStore(new SMB2Client(), "10.0.0.5", "Namespace", rootStore, new Dictionary<string, ISMBFileStore>());

            object handle;
            FileStatus fileStatus;
            dfsFileStore.CreateFile(out handle, out fileStatus, @"folder\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            Assert.AreEqual(@"folder\file.txt", rootStore.LastCreateFilePath);
        }

        [TestMethod]
        public void Constructor_WhenServerIsName_MarksUnderlyingStoreAsDfsOperation()
        {
            SMB2Client client = new SMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false);

            new SMB2DfsFileStore(client, "SERVER1", "DfsRoot", fileStore);

            Assert.IsTrue(fileStore.IsDfsOperation, "Requests against a DFS namespace root must be marked as DFS operations.");
        }

        [TestMethod]
        public void Constructor_WhenServerIsIPAddress_DoesNotMarkUnderlyingStoreAsDfsOperation()
        {
            SMB2Client client = new SMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false);

            new SMB2DfsFileStore(client, "10.0.0.5", "Namespace", fileStore);

            Assert.IsFalse(fileStore.IsDfsOperation);
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

        [TestMethod]
        public void GetNamespaceTargets_ReturnsTargetsInReferralOrder()
        {
            // MS-DFSC 3.1.5.4.3: referral targets are listed in order of preference, so the order
            // the server gave them in has to be preserved.
            ResponseGetDfsReferral referral = new ResponseGetDfsReferral();
            referral.ReferralEntries.Add(new DfsReferralEntryV4() { ServerType = DfsServerType.Root, DfsPath = @"\lab.local\Namespace", NetworkAddress = @"\NS1.lab.local\Namespace" });
            referral.ReferralEntries.Add(new DfsReferralEntryV4() { ServerType = DfsServerType.Root, DfsPath = @"\lab.local\Namespace", NetworkAddress = @"\NS2.lab.local\Namespace" });

            List<DfsPath> targets = DfsNamespaceResolver.GetNamespaceTargets(referral);

            Assert.AreEqual(2, targets.Count);
            // A root referral names the namespace server by FQDN, not by short name.
            Assert.AreEqual("NS1.lab.local", targets[0].ServerName);
            Assert.AreEqual("Namespace", targets[0].ShareName);
            Assert.AreEqual("NS2.lab.local", targets[1].ServerName);
        }

        [TestMethod]
        public void GetNamespaceTargets_SkipsEntriesThatAreNotRootTargets()
        {
            // MS-DFSC 2.2.4.3: only a root target names a namespace server. Resolution runs ahead
            // of every tree connect, so treating a link target as a namespace server would divert
            // an ordinary tree connect to a server it was never asked to reach.
            ResponseGetDfsReferral referral = new ResponseGetDfsReferral();
            referral.ReferralEntries.Add(new DfsReferralEntryV4() { ServerType = DfsServerType.NonRoot, NetworkAddress = @"\LINK-TARGET\Share" });
            referral.ReferralEntries.Add(new DfsReferralEntryV4() { ServerType = DfsServerType.Root, NetworkAddress = @"\NS1\Namespace" });

            List<DfsPath> targets = DfsNamespaceResolver.GetNamespaceTargets(referral);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual("NS1", targets[0].ServerName);
        }

        [TestMethod]
        public void IsSameServer_MatchesOnlyTheSameHost()
        {
            // A root referral names its target by FQDN while the caller will often have connected
            // by short name, so both spellings have to name one host. Treating distinct hosts as
            // one would skip a target the referral named, which is how a namespace becomes
            // unreachable, so anything less certain than that has to be treated as a second host.
            Assert.IsTrue(DfsNamespaceResolver.IsSameServer("NS1", "NS1.lab.local"));
            Assert.IsTrue(DfsNamespaceResolver.IsSameServer("NS1.lab.local", "NS1"));
            Assert.IsTrue(DfsNamespaceResolver.IsSameServer("ns1", "NS1"));
            Assert.IsTrue(DfsNamespaceResolver.IsSameServer("10.0.0.5", "10.0.0.5"));

            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("10.0.0.5", "10.0.0.9"));
            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("192.168.1.10", "192.168.1.20"));
            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("NS1.a.local", "NS1.b.local"));
            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("NS1", "NS2.lab.local"));
            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("NS1", "NS10.lab.local"));
            Assert.IsFalse(DfsNamespaceResolver.IsSameServer("NS1", null));
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
            public string LastReferralRequestPath;
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
                if (ctlCode == (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS && input != null)
                {
                    LastReferralRequestPath = new RequestGetDfsReferral(input).RequestFileName;
                }
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
