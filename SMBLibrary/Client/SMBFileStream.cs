using System;
using System.IO;

namespace SMBLibrary.Client
{
    public sealed class SMBFileStream : Stream
    {
        public override bool CanRead => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        private readonly object m_handle;
        private readonly ISMBFileStore m_store;

        private bool m_disposed;

        public override void Flush() => throw new NotImplementedException();

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
    }
}