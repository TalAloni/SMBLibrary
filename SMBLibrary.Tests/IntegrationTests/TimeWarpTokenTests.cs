/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.IntegrationTests
{
    /// <summary>
    /// Exercises SMB2_CREATE_TIMEWARP_TOKEN against a server that actually serves shadow copies,
    /// which the in-process SMBServer does not. Point these at a Samba share configured with
    /// shadow_copy2 and two snapshots holding a file named report.txt.
    /// </summary>
    [TestClass]
    public class TimeWarpTokenTests
    {
        // Set to the address of a shadow-copy enabled share to run these.
        private const string ServerAddress = null;
        private const string ShareName = "testshare_vss";
        private const string UserName = "testuser";
        private const string Password = "testpass";

        private SMB2Client m_client;
        private ISMBFileStore m_fileStore;

        [TestInitialize]
        public void Initialize()
        {
            if (ServerAddress == null)
            {
                Assert.Inconclusive("No shadow-copy enabled server configured.");
            }

            m_client = new SMB2Client();
            Assert.IsTrue(m_client.Connect(IPAddress.Parse(ServerAddress), SMBTransportType.DirectTCPTransport), "connect");
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, m_client.Login(String.Empty, UserName, Password), "login");
            m_fileStore = m_client.TreeConnect(ShareName, out NTStatus status);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, "tree connect");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (m_fileStore != null)
            {
                m_fileStore.Disconnect();
            }
            if (m_client != null)
            {
                m_client.Logoff();
                m_client.Disconnect();
            }
        }

        [TestMethod]
        public void EnumerateSnapshotsReturnsTheServersTokens()
        {
            object handle;
            FileStatus fileStatus;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, m_fileStore.CreateFile(out handle, out fileStatus, String.Empty, AccessMask.GENERIC_READ, 0, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null));

            byte[] output;
            NTStatus status = m_fileStore.DeviceIOControl(handle, (uint)IoControlCode.FSCTL_SRV_ENUMERATE_SNAPSHOTS, new byte[0], out output, 64 * 1024);
            m_fileStore.CloseFile(handle);

            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsTrue(output.Length > 12, "snapshot array should carry at least one token");
        }

        [TestMethod]
        public void TimeWarpTokenOpensTheFileHeldByThatSnapshot()
        {
            DateTime snapshot = new DateTime(2026, 9, 17, 6, 30, 0, DateTimeKind.Utc);
            List<CreateContext> createContexts = new List<CreateContext>();
            createContexts.Add(CreateContextHelper.CreateTimeWarpToken(snapshot));

            object handle;
            FileStatus fileStatus;
            List<CreateContext> responseCreateContexts;
            NTStatus status = ((SMB2FileStore)m_fileStore).CreateFile(out handle, out fileStatus, out responseCreateContexts, "report.txt", AccessMask.GENERIC_READ, 0, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, 0, null, createContexts);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status, "open with TimeWarp token");
            Assert.IsNotNull(responseCreateContexts, "an open that succeeded reports the server's contexts, empty or not");

            byte[] data;
            m_fileStore.ReadFile(out data, handle, 0, 64);
            m_fileStore.CloseFile(handle);

            Assert.AreEqual("oldest", Encoding.ASCII.GetString(data).Trim());
        }

        [TestMethod]
        public void OpeningWithoutATimeWarpTokenReadsTheLiveFile()
        {
            object handle;
            FileStatus fileStatus;
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, m_fileStore.CreateFile(out handle, out fileStatus, "report.txt", AccessMask.GENERIC_READ, 0, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, 0, null));

            byte[] data;
            m_fileStore.ReadFile(out data, handle, 0, 64);
            m_fileStore.CloseFile(handle);

            Assert.AreEqual("live", Encoding.ASCII.GetString(data).Trim());
        }
    }
}
