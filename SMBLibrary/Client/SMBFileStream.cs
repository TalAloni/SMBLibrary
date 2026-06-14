using System;
using System.ComponentModel;
using System.IO;

namespace SMBLibrary.Client
{
    public sealed class SMBFileStream : Stream
    {
        private const AccessMask FileReadData = (AccessMask)0x01;
        private const AccessMask FileWriteData = (AccessMask)0x02;

        public override bool CanRead => !m_disposed && (AccessMask & FileReadData) != 0;

        public override bool CanWrite => !m_disposed && (AccessMask & FileWriteData) != 0;

        public override bool CanSeek => !m_disposed;

        public override long Length => GetFileInformation<FileStandardInformation>().EndOfFile;

        public override long Position
        {
            get => GetFileInformation<FilePositionInformation>().CurrentByteOffset;
            set => Seek(value, SeekOrigin.Begin);
        }

        public string Name => GetFileInformation<FileAlternateNameInformation>().FileName;

        private AccessMask AccessMask => GetFileInformation<FileAccessInformation>().AccessFlags;

        private readonly object m_handle;
        private readonly ISMBFileStore m_store;

        private bool m_disposed;

        public override void Flush()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SMBFileStream));

            m_store.FlushFileBuffers(m_handle);
        }

        public override void SetLength(long value)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SMBFileStream));

            if (!CanWrite)
                throw new NotSupportedException();

            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "The length of the file must be 0 or greater.");

            m_store.SetFileInformation(m_handle, new FileEndOfFileInformation { EndOfFile = value });
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var length = Length;
            var target = offset;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    // Do nothing
                    break;

                case SeekOrigin.Current:
                    target += Position;
                    break;

                case SeekOrigin.End:
                    target += length;
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin));
            }

            if (target < 0)
                throw new IOException("Cannot seek before beginning of stream.");

            if (target > length)
                throw new IOException("Cannot seek past end of stream.");

            var result = m_store.SetFileInformation(m_handle, new FilePositionInformation { CurrentByteOffset = target });

            return result == NTStatus.STATUS_SUCCESS
                ? target
                : throw new IOException($"Could not set SMB file position. Error status: {result}");
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        protected override void Dispose(bool disposeManaged)
        {
            if (m_disposed)
                return;

            m_disposed = true;

            if (disposeManaged)
                m_store.CloseFile(m_handle);

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