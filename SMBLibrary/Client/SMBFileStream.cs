using System;
using System.IO;

namespace SMBLibrary.Client
{
    public sealed class SMBFileStream : Stream
    {
        public override bool CanRead => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override long Length => GetFileInformation<FileStandardInformation>().EndOfFile;

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        private readonly object m_handle;
        private readonly ISMBFileStore m_store;

        private bool m_disposed;

        public override void Flush()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SMBFileStream));

            m_store.FlushFileBuffers(m_handle);
        }

        public override void SetLength(long value) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        protected override void Dispose(bool disposeManaged)
        {
            if (m_disposed)
                return;

            m_disposed = true;

            base.Dispose(disposeManaged);
        }

        private T GetFileInformation<T>() where T : FileInformation
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            FileInformationClass fileInformationClass;

            try
            {
                fileInformationClass = (FileInformationClass)Enum.Parse(typeof(FileInformationClass), typeof(T).Name);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid file information class: {typeof(T)}", e);
            }

            var status = m_store.GetFileInformation(out var result, m_handle, fileInformationClass);

            return status == NTStatus.STATUS_SUCCESS
                ? (T)result
                : throw new IOException($"Could not get file information from SMB file. Error status: {status}");
        }
    }
}