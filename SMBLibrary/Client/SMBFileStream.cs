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
        private const AccessMask FileAppendData = (AccessMask)0x04;

        private const ShareAccess SupportedShareAccessFlags = ShareAccess.Read | ShareAccess.Write | ShareAccess.Delete;

        private const CreateOptions RequiredCreateFlags = CreateOptions.FILE_NON_DIRECTORY_FILE;

        private static readonly Dictionary<FileAccess, AccessMask> FileAccessToAccessMaskMap =
            new Dictionary<FileAccess, AccessMask>(2)
            {
                { FileAccess.Read, FileReadData },
                { FileAccess.Write, FileWriteData },
            };

        private static readonly Dictionary<FileMode, CreateDisposition> FileModeToCreateDispositionMap =
            new Dictionary<FileMode, CreateDisposition>(6)
            {
                { FileMode.CreateNew, CreateDisposition.FILE_CREATE },
                { FileMode.Create, CreateDisposition.FILE_SUPERSEDE },
                { FileMode.Open, CreateDisposition.FILE_OPEN },
                { FileMode.OpenOrCreate, CreateDisposition.FILE_OPEN_IF },
                { FileMode.Truncate, CreateDisposition.FILE_OVERWRITE },
                { FileMode.Append, CreateDisposition.FILE_OPEN_IF },
            };

        private static readonly Dictionary<FileOptions, CreateOptions> FileOptionsToCreateOptionsMap =
            new Dictionary<FileOptions, CreateOptions>(5)
            {
                { FileOptions.WriteThrough, CreateOptions.FILE_WRITE_THROUGH },
                { FileOptions.DeleteOnClose, CreateOptions.FILE_DELETE_ON_CLOSE },
                //{ FileOptions.SequentialScan, CreateOptions.FILE_SEQUENTIAL_ONLY },
                { FileOptions.RandomAccess, CreateOptions.FILE_RANDOM_ACCESS },
                //{ FileOptions.Asynchronous, CreateOptions. },
            };

        public override bool CanRead => !m_disposed && (m_accessMask & FileReadData) != 0;

        public override bool CanWrite => !m_disposed && (m_accessMask & FileWriteData) != 0;

        public override bool CanSeek => !m_disposed;

        public override long Length => m_disposed ? throw new ObjectDisposedException(GetType().FullName) : m_length;

        public override long Position
        {
            get => m_disposed ? throw new ObjectDisposedException(GetType().FullName) : m_position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override bool CanTimeout => false;

        public override int ReadTimeout
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public override int WriteTimeout
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public int MaxReadSize => (int)Math.Min(int.MaxValue, m_store.MaxReadSize);

        public int MaxWriteSize => (int)Math.Min(int.MaxValue, m_store.MaxWriteSize);

        public string Name { get; }

        private readonly object m_handle;
        private readonly bool m_ownsStore;
        private readonly long m_earliestSeekablePosition;
        private readonly AccessMask m_accessMask;
        private readonly ISMBFileStore m_store;

        private bool m_disposed;
        private long m_length;
        private long m_position;

        public SMBFileStream(ISMBFileStore store, string path, FileMode fileMode,
            FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read,
            FileOptions fileOptions = FileOptions.None, bool ownsStore = false)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (Path.IsPathRooted(path))
                throw new ArgumentException("Path must be relative", nameof(path));

            var append = fileMode == FileMode.Append;

            if (append || fileMode == FileMode.Truncate)
            {
                if ((fileAccess & FileAccess.Read) != 0)
                    throw new ArgumentException($"{fileMode} cannot be combined with FileAccess.Read");

                if ((fileAccess & FileAccess.Write) == 0)
                    throw new ArgumentException($"{fileMode} must be combined with FileAccess.Write");
            }

            var accessMask = append ? FileAppendData : 0;

            foreach (var entry in FileAccessToAccessMaskMap)
                if ((fileAccess & entry.Key) != 0)
                    accessMask |= entry.Value;

            var fileAttributes = (fileOptions & FileOptions.Encrypted) != 0
                ? FileAttributes.Encrypted
                : FileAttributes.Normal;

            var shareAccess = (ShareAccess)fileShare & SupportedShareAccessFlags;

            var createDisposition = FileModeToCreateDispositionMap[fileMode];

            var createOptions = RequiredCreateFlags;

            foreach (var entry in FileOptionsToCreateOptionsMap)
                if ((fileOptions & entry.Key) != FileOptions.None)
                    createOptions |= entry.Value;

            var status = store.CreateFile(out var handle, out var fileStatus, path, accessMask, fileAttributes,
                shareAccess, createDisposition, createOptions, null);

            if (status != NTStatus.STATUS_SUCCESS)
                throw new IOException($"Could not create SMB handle. Error status: {status}. File status: {fileStatus}");

            m_handle = handle;
            m_ownsStore = ownsStore;
            m_store = store;

            m_position = m_earliestSeekablePosition;
            m_disposed = false;

            var result = m_store.GetFileInformation(out var info, m_handle,
                FileInformationClass.FileAllInformation);

            if (result != NTStatus.STATUS_SUCCESS)
                throw new IOException($"Could not get file information from SMB file. Error status: {status}");

            var fileInfo = (FileAllInformation)info;

            Name = fileInfo.NameInformation.FileName;
            m_length = fileInfo.StandardInformation.EndOfFile;
            m_earliestSeekablePosition = append ? m_length : 0;
            m_accessMask = fileInfo.AccessInformation.AccessFlags;
        }

        public override void Flush()
        {
            if (m_disposed)
                return;

            var status = m_store.FlushFileBuffers(m_handle);

            if (status != NTStatus.STATUS_SUCCESS)
                throw new IOException($"Could not flush SMB stream. Error status: {status}");
        }

        public override void SetLength(long value)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!CanWrite)
                throw new NotSupportedException();

            if (value < m_earliestSeekablePosition)
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"The length of the file must be {m_earliestSeekablePosition} or greater.");

            var status = m_store.SetFileInformation(m_handle, new FileEndOfFileInformation { EndOfFile = value });

            if (status != NTStatus.STATUS_SUCCESS)
                throw new IOException($"Could not set file length. Error status: {status}");

            m_length = value;

            if (m_position > value)
                m_position = value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

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
                    target += m_length;
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin));
            }

            if (target < m_earliestSeekablePosition)
                throw new IOException("Cannot seek before beginning of stream.");

            if (target > m_length)
                throw new IOException("Cannot seek past end of stream.");

            m_position = target;
            return m_position;
        }

        public override int Read(byte[] destination, int offset, int count)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

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

            var maxBytes = Math.Min(MaxReadSize, count);
            var read = 0;

            while (read < maxBytes)
            {
                var status = m_store.ReadFile(out var bytes, m_handle, m_position + read, maxBytes - read);

                if ((status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE) || bytes.Length == 0)
                    break;

                Array.Copy(bytes, 0, destination, offset + read, bytes.Length);
                read += bytes.Length;

                if (status == NTStatus.STATUS_END_OF_FILE)
                    break;
            }

            m_position += read;
            return read;
        }

        public override void Write(byte[] source, int offset, int count)
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().FullName);

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

            var written = 0;

            while (written < count)
            {
                var bytes = new byte[Math.Min(MaxWriteSize, count - written)];
                Array.Copy(source, offset + written, bytes, 0, bytes.Length);
                var status = m_store.WriteFile(out var writtenThisRound, m_handle, m_position + written, bytes);

                if (status != NTStatus.STATUS_SUCCESS)
                    throw new IOException(
                        $"Could not write to SMB file. Bytes successfully written: {written}. Error status: {status}");

                if (writtenThisRound == 0)
                    throw new IOException(
                        $"SMB server reported a successful write of zero bytes. Bytes successfully written: {written}");

                written += writtenThisRound;
                m_length = Math.Max(m_length, m_position + written);
            }

            m_position += count;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (m_disposed)
                return;

            m_disposed = true;

            if (disposeManaged)
            {
                try
                {
                    m_store.CloseFile(m_handle);
                }
                finally
                {
                    if (m_ownsStore)
                        m_store.Disconnect();
                }
            }

            base.Dispose(disposeManaged);
        }
        /*
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
        //*/
    }
}