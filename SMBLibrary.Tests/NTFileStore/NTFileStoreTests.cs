using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Win32;

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

            object handle;
            FileStatus fileStatus;
            NTStatus status = m_fileStore.CreateFile(out handle, out fileStatus, TestDirName, AccessMask.GENERIC_ALL, FileAttributes.Directory, ShareAccess.Read, CreateDisposition.FILE_OPEN_IF, CreateOptions.FILE_DIRECTORY_FILE, null);
            Assert.IsTrue(status == NTStatus.STATUS_SUCCESS);
            status = m_fileStore.CloseFile(handle);
            Assert.IsTrue(status == NTStatus.STATUS_SUCCESS);
        }

        [TestMethod]
        public void TestCancel()
        {
            object handle;
            FileStatus fileStatus;
            m_fileStore.CreateFile(out handle, out fileStatus, TestDirName, AccessMask.GENERIC_ALL, FileAttributes.Directory, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);

            object ioRequest = null;
            NTStatus status = m_fileStore.NotifyChange(out ioRequest, handle, NotifyChangeFilter.FileName | NotifyChangeFilter.LastWrite | NotifyChangeFilter.DirName, false, 8192, OnNotifyChangeCompleted, null);
            Assert.IsTrue(status == NTStatus.STATUS_PENDING);

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

        public void TestAll()
        {
            TestCancel();
        }
    }
}
