/* Copyright (C) 2019-2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SMBLibrary.Tests
{
    [TestClass]
    public abstract class NTFileStoreTests
    {
        private INTFileStore m_fileStore;
        private readonly string TestDirName = "Dir";

        private NTStatus? m_notifyChangeStatus;

        public NTFileStoreTests(INTFileStore fileStore)
        {
            m_fileStore = fileStore;
        }

        [TestMethod]
        public virtual void TestCancel()
        {
            CreateTestDirectory();

            object handle;
            FileStatus fileStatus;
            m_fileStore.CreateFile(out handle, out fileStatus, TestDirName, AccessMask.GENERIC_ALL, FileAttributes.Directory, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);

            object ioRequest = null;
            NTStatus status = m_fileStore.NotifyChange(out ioRequest, handle, NotifyChangeFilter.FileName | NotifyChangeFilter.LastWrite | NotifyChangeFilter.DirName, false, 8192, OnNotifyChangeCompleted, null);
            Assert.IsTrue(status == NTStatus.STATUS_PENDING);

            Thread.Sleep(1);
            m_fileStore.Cancel(ioRequest);
            m_fileStore.CloseFile(handle);
            while (m_notifyChangeStatus == null)
            {
                Thread.Sleep(1);
            }
            Assert.IsTrue(m_notifyChangeStatus.Value == NTStatus.STATUS_CANCELLED);
        }

        private void OnNotifyChangeCompleted(NTStatus status, byte[] buffer, object context)
        {
            m_notifyChangeStatus = status;
        }

        private void CreateTestDirectory()
        {
            object handle;
            FileStatus fileStatus;
            NTStatus status = m_fileStore.CreateFile(out handle, out fileStatus, TestDirName, AccessMask.GENERIC_ALL, FileAttributes.Directory, ShareAccess.Read, CreateDisposition.FILE_OPEN_IF, CreateOptions.FILE_DIRECTORY_FILE, null);
            Assert.IsTrue(status == NTStatus.STATUS_SUCCESS);
            status = m_fileStore.CloseFile(handle);
            Assert.IsTrue(status == NTStatus.STATUS_SUCCESS);
        }
    }
}
