using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace SMBLibrary.Client
{
    public sealed class SMBFileStream : Stream
    {
        private const AccessMask FileReadData = (AccessMask)0x01;
        private const AccessMask FileWriteData = (AccessMask)0x02;

        private const ShareAccess SupportedShareAccessFlags = ShareAccess.Read | ShareAccess.Write | ShareAccess.Delete;

        private const CreateOptions RequiredCreateFlags = CreateOptions.FILE_NON_DIRECTORY_FILE;

        private static readonly Dictionary<FileMode, CreateDisposition> _fileModeToCreateDispositionMap =
            new Dictionary<FileMode, CreateDisposition>(6)
            {
                { FileMode.CreateNew, CreateDisposition.FILE_CREATE },
                { FileMode.Create, CreateDisposition.FILE_SUPERSEDE },
                { FileMode.Open, CreateDisposition.FILE_OPEN },
                { FileMode.OpenOrCreate, CreateDisposition.FILE_OPEN_IF },
                { FileMode.Truncate, CreateDisposition.FILE_SUPERSEDE },
                { FileMode.Append, CreateDisposition.FILE_OPEN_IF },
            };

        private static readonly Dictionary<FileOptions, CreateOptions> _fileOptionsToCreateOptionsMap =
            new Dictionary<FileOptions, CreateOptions>(5)
            {
                { FileOptions.WriteThrough, CreateOptions.FILE_WRITE_THROUGH },
                { FileOptions.DeleteOnClose, CreateOptions.FILE_DELETE_ON_CLOSE },
                //{ FileOptions.SequentialScan, CreateOptions.FILE_SEQUENTIAL_ONLY },
                //{ FileOptions.RandomAccess, CreateOptions.FILE_RANDOM_ACCESS },
                //{ FileOptions.Asynchronous, CreateOptions. },
            };

        public override bool CanRead => !m_disposed && (AccessMask & FileReadData) != 0;

        public override bool CanWrite => !m_disposed && (AccessMask & FileWriteData) != 0;

        public override bool CanSeek => !m_disposed;

        public override long Length => GetFileInformation<FileStandardInformation>().EndOfFile;

        public override long Position
        {
            get => GetFileInformation<FilePositionInformation>().CurrentByteOffset;
            set => Seek(value, SeekOrigin.Begin);
        }

        public int MaxReadSize => (int)Math.Min(int.MaxValue, m_store.MaxReadSize);

        public int MaxWriteSize => (int)Math.Min(int.MaxValue, m_store.MaxWriteSize);

        public string Name => GetFileInformation<FileAlternateNameInformation>().FileName;

        private AccessMask AccessMask => GetFileInformation<FileAccessInformation>().AccessFlags;

        private readonly object m_handle;
        private readonly ISMBFileStore m_store;

        private bool m_disposed;

        public SMBFileStream(ISMBFileStore store, string path, FileMode fileMode,
            FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read,
            FileOptions fileOptions = FileOptions.None)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (Path.IsPathRooted(path))
                throw new ArgumentException("Path must be relative", nameof(path));

            if (fileMode == FileMode.Truncate)
            {
                //TODO: throw FileNotFoundException if file does not exist
            }

            var accessMask = (AccessMask)0; //TODO

            var fileAttributes = (FileAttributes)0; //TODO

            var shareAccess = (ShareAccess)fileShare & SupportedShareAccessFlags;

            var createDisposition = _fileModeToCreateDispositionMap[fileMode];

            var createOptions = RequiredCreateFlags;

            foreach (var entry in _fileOptionsToCreateOptionsMap)
                if ((fileOptions & entry.Key) != FileOptions.None)
                    createOptions |= entry.Value;

            var status = store.CreateFile(out var handle, out var fileStatus, path, accessMask, fileAttributes,
                shareAccess, createDisposition, createOptions, null);

            if (status != NTStatus.STATUS_SUCCESS)
                throw new IOException($"Could not create SMB handle. Error status: {status}. File status: {fileStatus}");

            m_handle = handle;
            m_store = store;
            m_disposed = false;
        }

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

        public override int Read(byte[] destination, int offset, int count)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SMBFileStream));

            if (count == 0)
                return 0;

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (destination.Length - offset < count)
                throw new ArgumentException("Offset and count exceed destination bounds.");

            if (!CanRead)
                throw new NotSupportedException();

            var startingPosition = GetFileInformation<FilePositionInformation>().CurrentByteOffset;

            var maxBytes = Math.Min(MaxReadSize, destination.Length);
            var read = 0;

            while (read < maxBytes)
            {
                var status = m_store.ReadFile(out var bytes, m_handle, startingPosition + read, maxBytes - read);

                if ((status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE) || bytes.Length == 0)
                    break;

                Array.Copy(bytes, 0, destination, offset + read, bytes.Length);
                read += bytes.Length;

                if (status == NTStatus.STATUS_END_OF_FILE)
                    break;
            }

            return read;
        }

        public override void Write(byte[] source, int offset, int count)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(SMBFileStream));

            if (count == 0)
                return;

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (source.Length - offset < count)
                throw new ArgumentException("Offset and count exceed source bounds.");

            if (!CanWrite)
                throw new NotSupportedException();

            var maxWriteSize = MaxWriteSize;

            if (source.Length > MaxWriteSize)
                throw new IOException($"Write size exceeds maximum write size of {maxWriteSize} bytes.");

            throw new NotImplementedException();
        }

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