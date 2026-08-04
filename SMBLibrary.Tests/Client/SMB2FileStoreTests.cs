/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class SMB2FileStoreTests
    {
        [TestMethod]
        public void CreateFile_WhenShareIsDfsRoot_SetsDfsOperationsFlagAndFullPath()
        {
            CapturingSMB2Client client = new CapturingSMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false, @"SERVER1\DfsRoot");

            object handle;
            FileStatus fileStatus;
            fileStore.CreateFile(out handle, out fileStatus, @"Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            CreateRequest request = (CreateRequest)client.LastRequest;
            // [MS-SMB2] 3.2.4.1.4 - The client MUST set SMB2_FLAGS_DFS_OPERATIONS when sending a DFS operation.
            // Without it the server will not return STATUS_PATH_NOT_COVERED and no referral is ever requested.
            Assert.IsTrue((request.Header.Flags & SMB2PacketHeaderFlags.DfsOperations) > 0);
            // [MS-SMB2] 2.2.13 - When SMB2_FLAGS_DFS_OPERATIONS is set the name is subject to DFS name normalization
            // and must be a full path in the form <server>\<share>\<path>.
            Assert.AreEqual(@"SERVER1\DfsRoot\Link\file.txt", request.Name);
        }

        [TestMethod]
        public void CreateFile_WhenShareIsDfsRootAndPathIsEmpty_SendsSharePath()
        {
            CapturingSMB2Client client = new CapturingSMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false, @"SERVER1\DfsRoot");

            object handle;
            FileStatus fileStatus;
            fileStore.CreateFile(out handle, out fileStatus, String.Empty, (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            CreateRequest request = (CreateRequest)client.LastRequest;
            Assert.IsTrue((request.Header.Flags & SMB2PacketHeaderFlags.DfsOperations) > 0);
            Assert.AreEqual(@"SERVER1\DfsRoot", request.Name);
        }

        [TestMethod]
        public void CreateFile_WhenShareIsDfsRootAndPathIsBackslash_SendsSharePathWithoutTrailingSeparator()
        {
            CapturingSMB2Client client = new CapturingSMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false, @"SERVER1\DfsRoot");

            object handle;
            FileStatus fileStatus;
            // Opening the share root using a single backslash is the idiom used in ClientExamples.md
            fileStore.CreateFile(out handle, out fileStatus, @"\", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            CreateRequest request = (CreateRequest)client.LastRequest;
            Assert.IsTrue((request.Header.Flags & SMB2PacketHeaderFlags.DfsOperations) > 0);
            Assert.AreEqual(@"SERVER1\DfsRoot", request.Name);
        }

        [TestMethod]
        public void CreateFile_WhenShareIsDfsRootAndPathHasLeadingBackslash_DoesNotDoubleSeparator()
        {
            CapturingSMB2Client client = new CapturingSMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false, @"SERVER1\DfsRoot");

            object handle;
            FileStatus fileStatus;
            fileStore.CreateFile(out handle, out fileStatus, @"\Link\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            CreateRequest request = (CreateRequest)client.LastRequest;
            Assert.AreEqual(@"SERVER1\DfsRoot\Link\file.txt", request.Name);
        }

        [TestMethod]
        public void CreateFile_WhenShareIsNotDfsRoot_DoesNotSetDfsOperationsFlagAndKeepsPathShareRelative()
        {
            CapturingSMB2Client client = new CapturingSMB2Client();
            SMB2FileStore fileStore = new SMB2FileStore(client, 1, false);

            object handle;
            FileStatus fileStatus;
            fileStore.CreateFile(out handle, out fileStatus, @"folder\file.txt", (AccessMask)0, (FileAttributes)0, (ShareAccess)0, (CreateDisposition)0, (CreateOptions)0, null);

            CreateRequest request = (CreateRequest)client.LastRequest;
            Assert.IsTrue((request.Header.Flags & SMB2PacketHeaderFlags.DfsOperations) == 0);
            Assert.AreEqual(@"folder\file.txt", request.Name);
        }

        /// <summary>
        /// SMB2Client that captures the request instead of sending it, so that the composition of
        /// outgoing requests can be verified without a server.
        /// </summary>
        private class CapturingSMB2Client : SMB2Client
        {
            public SMB2Command LastRequest;

            public CapturingSMB2Client()
            {
                // SMB2FileStore refuses to send when the client is not connected. The field is set directly
                // to avoid having to widen the public API surface with a virtual IsConnected for the sake of a test.
                FieldInfo isConnectedField = typeof(SMB2Client).GetField("m_isConnected", BindingFlags.NonPublic | BindingFlags.Instance);
                isConnectedField.SetValue(this, true);
            }

            internal override void TrySendCommand(SMB2Command request, bool encryptData)
            {
                LastRequest = request;
            }

            internal override SMB2Command WaitForCommand(ulong messageID, out bool connectionTerminated)
            {
                connectionTerminated = false;
                return null;
            }
        }
    }
}
