/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Authentication.GSSAPI;
using SMBLibrary.Authentication.NTLM;
using SMBLibrary.Client;
using SMBLibrary.Server;
using SMBLibrary.Win32;

namespace SMBLibrary.Tests.IntegrationTests
{
    /// <summary>
    /// End-to-end tests against an in-process SMBServer, verifying that a share which is not a DFS
    /// namespace root keeps using share-relative paths and is not treated as a DFS operation.
    /// </summary>
    [TestClass]
    public class SMB2FileStoreIntegrationTests
    {
        private static readonly int s_minPort = 1025;
        private static readonly int s_maxPort = 50000;
        private static int s_nextServerPort = s_minPort + new Random().Next(s_maxPort - s_minPort);
        private static readonly string TestDirectoryPath = Path.Combine(Path.GetTempPath(), "SMBLibraryFileStoreTests");

        private int m_serverPort;
        private SMBServer m_server;

        [TestInitialize]
        public void Initialize()
        {
            if (Directory.Exists(TestDirectoryPath))
            {
                Directory.Delete(TestDirectoryPath, true);
            }
            Directory.CreateDirectory(TestDirectoryPath);

            m_serverPort = Interlocked.Increment(ref s_nextServerPort);
            SMBShareCollection shares = new SMBShareCollection();
            shares.Add(new FileSystemShare("Share", new NTDirectoryFileSystem(TestDirectoryPath)));
            IGSSMechanism gssMechanism = new IndependentNTLMAuthenticationProvider((username) => "password");
            GSSProvider gssProvider = new GSSProvider(gssMechanism);
            m_server = new SMBServer(shares, gssProvider);
            m_server.Start(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort, false, true, false, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            m_server.Stop();
            if (Directory.Exists(TestDirectoryPath))
            {
                Directory.Delete(TestDirectoryPath, true);
            }
        }

        [TestMethod]
        public void When_ShareIsNotDfsRoot_WriteAndReadBackSucceed()
        {
            SMB2Client client = Connect();
            try
            {
                NTStatus status;
                ISMBFileStore fileStore = client.TreeConnect("Share", out status);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
                // A share that is not a DFS namespace root must not be wrapped for referral following
                Assert.IsInstanceOfType(fileStore, typeof(SMB2FileStore));

                byte[] contents = Encoding.ASCII.GetBytes("SMBLibrary");
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, WriteFile(fileStore, "file.txt", contents));
                CollectionAssert.AreEqual(contents, ReadFile(fileStore, "file.txt"));

                fileStore.Disconnect();
            }
            finally
            {
                Logoff(client);
            }
        }

        /// <summary>
        /// A single backslash is the idiom used in ClientExamples.md to open the share root.
        /// </summary>
        [TestMethod]
        public void When_ShareIsNotDfsRoot_OpeningShareRootByBackslashSucceeds()
        {
            SMB2Client client = Connect();
            try
            {
                NTStatus status;
                ISMBFileStore fileStore = client.TreeConnect("Share", out status);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

                object handle;
                FileStatus fileStatus;
                status = fileStore.CreateFile(out handle, out fileStatus, @"\", AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
                fileStore.CloseFile(handle);

                fileStore.Disconnect();
            }
            finally
            {
                Logoff(client);
            }
        }

        /// <summary>
        /// The share root is also expressed as an empty string, both forms must keep working.
        /// </summary>
        [TestMethod]
        public void When_ShareIsNotDfsRoot_OpeningShareRootByEmptyStringSucceeds()
        {
            SMB2Client client = Connect();
            try
            {
                NTStatus status;
                ISMBFileStore fileStore = client.TreeConnect("Share", out status);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

                object handle;
                FileStatus fileStatus;
                status = fileStore.CreateFile(out handle, out fileStatus, String.Empty, AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
                Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
                fileStore.CloseFile(handle);

                fileStore.Disconnect();
            }
            finally
            {
                Logoff(client);
            }
        }

        private SMB2Client Connect()
        {
            SMB2Client client = new SMB2Client();
            Assert.IsTrue(client.Connect(IPAddress.Loopback, SMBTransportType.DirectTCPTransport, m_serverPort));
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, client.Login(String.Empty, "John", "password"));
            return client;
        }

        private static void Logoff(SMB2Client client)
        {
            client.Logoff();
            client.Disconnect();
        }

        private static NTStatus WriteFile(ISMBFileStore fileStore, string path, byte[] data)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus status = fileStore.CreateFile(out handle, out fileStatus, path, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }

            int numberOfBytesWritten;
            status = fileStore.WriteFile(out numberOfBytesWritten, handle, 0, data);
            fileStore.CloseFile(handle);
            return status;
        }

        private static byte[] ReadFile(ISMBFileStore fileStore, string path)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus status = fileStore.CreateFile(out handle, out fileStatus, path, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, SMBLibrary.FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);

            byte[] data;
            status = fileStore.ReadFile(out data, handle, 0, 1024);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            fileStore.CloseFile(handle);
            return data;
        }
    }
}
