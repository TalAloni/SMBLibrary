using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace SMBLibrary.Client
{
    /// <summary>
    /// Provides a <see cref="Stream"/> for a single SMB-shared file.
    /// This class cannot be inherited.
    /// </summary>
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

        private static readonly Dictionary<FileOptions, CreateOptions> FileOptionsToCreateOptionsMap =
            new Dictionary<FileOptions, CreateOptions>(4)
            {
                { FileOptions.WriteThrough, CreateOptions.FILE_WRITE_THROUGH },
                { FileOptions.DeleteOnClose, CreateOptions.FILE_DELETE_ON_CLOSE },
                { FileOptions.SequentialScan, CreateOptions.FILE_SEQUENTIAL_ONLY },
                { FileOptions.RandomAccess, CreateOptions.FILE_RANDOM_ACCESS },
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

        /// <summary>
        /// TODO
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The <see cref="ISMBFileStore"/> with which this stream was created.
        /// </summary>
        public ISMBFileStore Store { get; }

        private readonly object m_handle;
        private readonly bool m_ownsStore;
        private readonly long m_earliestSeekablePosition;
        private readonly AccessMask m_accessMask;

        private bool m_disposed;
        private long m_length;
        private long m_position;

        /// <summary>
        /// Initializes a new instance of the <see cref="SMBFileStream"/> class.
        /// </summary>
        /// <param name="store">The store to use in this stream.</param>
        /// <param name="path">A relative path to the file that the stream will encapsulate.</param>
        /// <param name="fileMode">One of the enumeration values that determines how to open or create the file.</param>
        /// <param name="fileAccess">A bitwise combination of the enumeration values that determines how the file can be accessed by the stream. Default: <see cref="FileAccess.ReadWrite"/></param>
        /// <param name="fileShare">A bitwise combination of the enumeration values that determines how the file will be shared by processes. Default: <see cref="FileShare.Read"/></param>
        /// <param name="fileOptions">A bitwise combination of the enumeration values that specifies additional file options. Default: <see cref="FileOptions.None"/></param>
        /// <param name="ownsStore">True to disconnect <paramref name="store"/> when the stream is disposed; otherwise, false. Default: false</param>
        /// <exception cref="ArgumentNullException"><paramref name="store"/> is null, or <paramref name="path"/> is null or an empty string.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an absolute path, or <paramref name="fileMode"/> is <see cref="FileMode.Append"/> or <see cref="FileMode.Truncate"/> while <paramref name="fileAccess"/> is not <see cref="FileAccess.Write"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="fileMode"/> contains an invalid value.</exception>
        /// <exception cref="IOException">An error occured while attempting to either create the file handle or read its information.</exception>
        public SMBFileStream(ISMBFileStore store, string path, FileMode fileMode,
            FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.Read,
            FileOptions fileOptions = FileOptions.None, bool ownsStore = false)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

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

            //Possibly replace this with commented code below
            const FileAttributes fileAttributes = FileAttributes.Normal;

            /*
            var fileAttributes = (fileOptions & FileOptions.Encrypted) != FileOptions.None
                ? FileAttributes.Encrypted
                : FileAttributes.Normal;
            //*/

            var shareAccess = (ShareAccess)fileShare & SupportedShareAccessFlags;

            CreateDisposition createDisposition;

            switch (fileMode)
            {
                case FileMode.CreateNew:
                    createDisposition = CreateDisposition.FILE_CREATE;
                    break;

                case FileMode.Create:
                    createDisposition = CreateDisposition.FILE_SUPERSEDE;
                    break;

                case FileMode.Open:
                    createDisposition = CreateDisposition.FILE_OPEN;
                    break;

                case FileMode.OpenOrCreate:
                case FileMode.Append:
                    createDisposition = CreateDisposition.FILE_OPEN_IF;
                    break;

                case FileMode.Truncate:
                    createDisposition = CreateDisposition.FILE_OVERWRITE;
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(fileMode), (int)fileMode, typeof(FileMode));
            }

            var createOptions = RequiredCreateFlags;

            foreach (var entry in FileOptionsToCreateOptionsMap)
                if ((fileOptions & entry.Key) != FileOptions.None)
                    createOptions |= entry.Value;

            var status = store.CreateFile(out var handle, out var fileStatus, path, accessMask, fileAttributes,
                shareAccess, createDisposition, createOptions, null);

            try
            {
                if (status != NTStatus.STATUS_SUCCESS)
                    throw new IOException($"Could not create SMB handle. Error status: {status}. File status: {fileStatus}");

                status = store.GetFileInformation(out var info, handle, FileInformationClass.FileAllInformation);

                if (status != NTStatus.STATUS_SUCCESS)
                    throw new IOException($"Could not get file information from SMB file. Error status: {status}");

                var fileInfo = (FileAllInformation)info;

                Name = fileInfo.NameInformation.FileName;
                Store = store;
                m_handle = handle;
                m_ownsStore = ownsStore;
                m_length = fileInfo.StandardInformation.EndOfFile;
                m_earliestSeekablePosition = append ? m_length : 0;
                m_accessMask = fileInfo.AccessInformation.AccessFlags;
                m_position = m_earliestSeekablePosition;
                m_disposed = false;
            }
            catch
            {
                store.CloseFile(handle);
                throw;
            }
        }

        public override void Flush()
        {
            if (m_disposed)
                return;

            var status = Store.FlushFileBuffers(m_handle);

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

            var status = Store.SetFileInformation(m_handle, new FileEndOfFileInformation { EndOfFile = value });

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
                    target += m_position;
                    break;

                case SeekOrigin.End:
                    target += m_length;
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin));
            }

            if (target < m_earliestSeekablePosition)
                throw new IOException("Cannot seek before beginning of stream.");

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

            var maxBytes = Math.Min((int)Math.Min(int.MaxValue, Store.MaxReadSize), count);
            var read = 0;

            while (read < maxBytes)
            {
                var status = Store.ReadFile(out var bytes, m_handle, m_position + read, maxBytes - read);

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
                var bytes = new byte[Math.Min((int)Math.Min(int.MaxValue, Store.MaxWriteSize), count - written)];
                Array.Copy(source, offset + written, bytes, 0, bytes.Length);
                var status = Store.WriteFile(out var writtenThisRound, m_handle, m_position + written, bytes);

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
                    Store.CloseFile(m_handle);
                }
                finally
                {
                    if (m_ownsStore)
                        Store.Disconnect();
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

            var status = Store.GetFileInformation(out var result, m_handle, fileInformationClass);

            return status == NTStatus.STATUS_SUCCESS
                ? (T)result
                : throw new IOException($"Could not get file information from SMB file. Error status: {status}");
        }
        //*/
    }
}