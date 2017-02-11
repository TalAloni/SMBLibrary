/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace SMBLibrary
{
    public partial class NTFileSystemAdapter : INTFileStore
    {
        private const int BytesPerSector = 512;
        private const int ClusterSize = 4096;

        private IFileSystem m_fileSystem;

        public event EventHandler<LogEntry> OnLogEntry;

        public NTFileSystemAdapter(IFileSystem fileSystem)
        {
            m_fileSystem = fileSystem;
        }

        public NTStatus CreateFile(out object handle, out FileStatus fileStatus, string path, AccessMask desiredAccess, ShareAccess shareAccess, CreateDisposition createDisposition, CreateOptions createOptions, SecurityContext securityContext)
        {
            handle = null;
            fileStatus = FileStatus.FILE_DOES_NOT_EXIST;
            FileAccess createAccess = NTFileStoreHelper.ToCreateFileAccess(desiredAccess, createDisposition);
            bool requestedWriteAccess = (createAccess & FileAccess.Write) > 0;

            bool forceDirectory = (createOptions & CreateOptions.FILE_DIRECTORY_FILE) > 0;
            bool forceFile = (createOptions & CreateOptions.FILE_NON_DIRECTORY_FILE) > 0;

            if (forceDirectory & (createDisposition != CreateDisposition.FILE_CREATE &&
                                  createDisposition != CreateDisposition.FILE_OPEN &&
                                  createDisposition != CreateDisposition.FILE_OPEN_IF &&
                                  createDisposition != CreateDisposition.FILE_SUPERSEDE))
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            // Windows will try to access named streams (alternate data streams) regardless of the FILE_NAMED_STREAMS flag, we need to prevent this behaviour.
            if (path.Contains(":"))
            {
                // Windows Server 2003 will return STATUS_OBJECT_NAME_NOT_FOUND
                return NTStatus.STATUS_NO_SUCH_FILE;
            }

            FileSystemEntry entry;
            try
            {
                entry = m_fileSystem.GetEntry(path);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                Log(Severity.Debug, "CreateFile: Error retrieving '{0}'. {1}.", path, status);
                return status;
            }

            if (createDisposition == CreateDisposition.FILE_OPEN)
            {
                if (entry == null)
                {
                    return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                }

                fileStatus = FileStatus.FILE_EXISTS;
                if (entry.IsDirectory && forceFile)
                {
                    return NTStatus.STATUS_FILE_IS_A_DIRECTORY;
                }

                if (!entry.IsDirectory && forceDirectory)
                {
                    return NTStatus.STATUS_OBJECT_PATH_INVALID;
                }
            }
            else if (createDisposition == CreateDisposition.FILE_CREATE)
            {
                if (entry != null)
                {
                    // File already exists, fail the request
                    Log(Severity.Debug, "CreateFile: File '{0}' already exist", path);
                    fileStatus = FileStatus.FILE_EXISTS;
                    return NTStatus.STATUS_OBJECT_NAME_COLLISION;
                }

                if (!requestedWriteAccess)
                {
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                try
                {
                    if (forceDirectory)
                    {
                        Log(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                        entry = m_fileSystem.CreateDirectory(path);
                    }
                    else
                    {
                        Log(Severity.Information, "CreateFile: Creating file '{0}'", path);
                        entry = m_fileSystem.CreateFile(path);
                    }
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    Log(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                    return status;
                }
                fileStatus = FileStatus.FILE_CREATED;
            }
            else if (createDisposition == CreateDisposition.FILE_OPEN_IF ||
                     createDisposition == CreateDisposition.FILE_OVERWRITE ||
                     createDisposition == CreateDisposition.FILE_OVERWRITE_IF ||
                     createDisposition == CreateDisposition.FILE_SUPERSEDE)
            {
                if (entry == null)
                {
                    if (createDisposition == CreateDisposition.FILE_OVERWRITE)
                    {
                        return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    }

                    if (!requestedWriteAccess)
                    {
                        return NTStatus.STATUS_ACCESS_DENIED;
                    }

                    try
                    {
                        if (forceDirectory)
                        {
                            Log(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                            entry = m_fileSystem.CreateDirectory(path);
                        }
                        else
                        {
                            Log(Severity.Information, "CreateFile: Creating file '{0}'", path);
                            entry = m_fileSystem.CreateFile(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        NTStatus status = ToNTStatus(ex);
                        Log(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                        return status;
                    }
                    fileStatus = FileStatus.FILE_CREATED;
                }
                else
                {
                    fileStatus = FileStatus.FILE_EXISTS;
                    if (!requestedWriteAccess)
                    {
                        return NTStatus.STATUS_ACCESS_DENIED;
                    }

                    if (createDisposition == CreateDisposition.FILE_OVERWRITE ||
                        createDisposition == CreateDisposition.FILE_OVERWRITE_IF)
                    {
                        // Truncate the file
                        try
                        {
                            Stream temp = m_fileSystem.OpenFile(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                            temp.Close();
                        }
                        catch (Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            Log(Severity.Debug, "CreateFile: Error truncating '{0}'. {1}.", path, status);
                            return status;
                        }
                        fileStatus = FileStatus.FILE_OVERWRITTEN;
                    }
                    else if (createDisposition == CreateDisposition.FILE_SUPERSEDE)
                    {
                        // Delete the old file
                        try
                        {
                            m_fileSystem.Delete(path);
                        }
                        catch(Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            Log(Severity.Debug, "CreateFile: Error deleting '{0}'. {1}.", path, status);
                            return status;
                        }

                        try
                        {
                            if (forceDirectory)
                            {
                                Log(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                                entry = m_fileSystem.CreateDirectory(path);
                            }
                            else
                            {
                                Log(Severity.Information, "CreateFile: Creating file '{0}'", path);
                                entry = m_fileSystem.CreateFile(path);
                            }
                        }
                        catch (Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            Log(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                            return status;
                        }
                        fileStatus = FileStatus.FILE_SUPERSEDED;
                    }
                }
            }
            else
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            FileAccess fileAccess = NTFileStoreHelper.ToFileAccess(desiredAccess.File);
            Stream stream;
            bool deleteOnClose = false;
            if (fileAccess == (FileAccess)0 || entry.IsDirectory)
            {
                stream = null;
            }
            else
            {
                deleteOnClose = (createOptions & CreateOptions.FILE_DELETE_ON_CLOSE) > 0;
                NTStatus openStatus = OpenFileStream(out stream, path, fileAccess, shareAccess, createOptions);
                if (openStatus != NTStatus.STATUS_SUCCESS)
                {
                    return openStatus;
                }
            }

            handle = new FileHandle(path, entry.IsDirectory, stream, deleteOnClose);
            if (fileStatus != FileStatus.FILE_CREATED &&
                fileStatus != FileStatus.FILE_OVERWRITTEN &&
                fileStatus != FileStatus.FILE_SUPERSEDED)
            {
                fileStatus = FileStatus.FILE_OPENED;
            }
            return NTStatus.STATUS_SUCCESS;
        }

        private NTStatus OpenFileStream(out Stream stream, string path, FileAccess fileAccess, ShareAccess shareAccess, CreateOptions openOptions)
        {
            stream = null;
            // When FILE_OPEN_REPARSE_POINT is specified, the operation should continue normally if the file is not a reparse point.
            // FILE_OPEN_REPARSE_POINT is a hint that the caller does not intend to actually read the file, with the exception
            // of a file copy operation (where the caller will attempt to simply copy the reparse point).
            bool openReparsePoint = (openOptions & CreateOptions.FILE_OPEN_REPARSE_POINT) > 0;
            bool disableBuffering = (openOptions & CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING) > 0;
            bool buffered = (openOptions & CreateOptions.FILE_SEQUENTIAL_ONLY) > 0 && !disableBuffering && !openReparsePoint;
            FileShare fileShare = NTFileStoreHelper.ToFileShare(shareAccess);
            Log(Severity.Verbose, "OpenFileStream: Opening '{0}', Access={1}, Share={2}, Buffered={3}", path, fileAccess, fileShare, buffered);
            try
            {
                stream = m_fileSystem.OpenFile(path, FileMode.Open, fileAccess, fileShare);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                Log(Severity.Debug, "OpenFile: Cannot open '{0}'. {1}.", path, status);
                return status;
            }

            if (buffered)
            {
                stream = new PrefetchedStream(stream);
            }

            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus CloseFile(object handle)
        {
            FileHandle fileHandle = (FileHandle)handle;
            if (fileHandle.Stream != null)
            {
                fileHandle.Stream.Close();
            }

            if (fileHandle.DeleteOnClose)
            {
                try
                {
                    m_fileSystem.Delete(fileHandle.Path);
                }
                catch
                {
                }
            }
            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus ReadFile(out byte[] data, object handle, long offset, int maxCount)
        {
            data = null;
            FileHandle fileHandle = (FileHandle)handle;
            string path = fileHandle.Path;
            Stream stream = fileHandle.Stream;
            if (stream == null || !stream.CanRead)
            {
                Log(Severity.Debug, "ReadFile: Cannot read '{0}', Invalid Operation.", path);
                return NTStatus.STATUS_ACCESS_DENIED;
            }

            int bytesRead;
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                data = new byte[maxCount];
                bytesRead = stream.Read(data, 0, maxCount);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                Log(Severity.Debug, "ReadFile: Cannot read '{0}'. {1}.", path, status);
                return status;
            }

            if (bytesRead < maxCount)
            {
                // EOF, we must trim the response data array
                data = ByteReader.ReadBytes(data, 0, bytesRead);
            }
            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus WriteFile(out int numberOfBytesWritten, object handle, long offset, byte[] data)
        {
            numberOfBytesWritten = 0;
            FileHandle fileHandle = (FileHandle)handle;
            string path = fileHandle.Path;
            Stream stream = fileHandle.Stream;
            if (stream == null || !stream.CanWrite)
            {
                Log(Severity.Debug, "WriteFile: Cannot write '{0}'. Invalid Operation.", path);
                return NTStatus.STATUS_ACCESS_DENIED;
            }

            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                Log(Severity.Debug, "WriteFile: Cannot write '{0}'. {1}.", path, status);
                return status;
            }
            numberOfBytesWritten = data.Length;
            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus FlushFileBuffers(object handle)
        {
            FileHandle fileHandle = (FileHandle)handle;
            if (fileHandle.Stream != null)
            {
                fileHandle.Stream.Flush();
            }
            return NTStatus.STATUS_SUCCESS;
        }

        public NTStatus DeviceIOControl(object handle, uint ctlCode, byte[] input, out byte[] output, int maxOutputLength)
        {
            output = null;
            return NTStatus.STATUS_NOT_SUPPORTED;
        }

        public void Log(Severity severity, string message)
        {
            // To be thread-safe we must capture the delegate reference first
            EventHandler<LogEntry> handler = OnLogEntry;
            if (handler != null)
            {
                handler(this, new LogEntry(DateTime.Now, severity, "NT FileSystem", message));
            }
        }

        public void Log(Severity severity, string message, params object[] args)
        {
            Log(severity, String.Format(message, args));
        }

        /// <param name="exception">IFileSystem exception</param>
        private static NTStatus ToNTStatus(Exception exception)
        {
            if (exception is ArgumentException)
            {
                return NTStatus.STATUS_OBJECT_PATH_SYNTAX_BAD;
            }
            else if (exception is DirectoryNotFoundException)
            {
                return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
            }
            else if (exception is FileNotFoundException)
            {
                return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
            }
            else if (exception is IOException)
            {
                ushort errorCode = IOExceptionHelper.GetWin32ErrorCode((IOException)exception);
                if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                {
                    return NTStatus.STATUS_SHARING_VIOLATION;
                }
                else if (errorCode == (ushort)Win32Error.ERROR_DISK_FULL)
                {
                    return NTStatus.STATUS_DISK_FULL;
                }
                else if (errorCode == (ushort)Win32Error.ERROR_DIR_NOT_EMPTY)
                {
                    // If a user tries to rename folder1 to folder2 when folder2 already exists, Windows 7 will offer to merge folder1 into folder2.
                    // In such case, Windows 7 will delete folder 1 and will expect STATUS_DIRECTORY_NOT_EMPTY if there are files to merge.
                    return NTStatus.STATUS_DIRECTORY_NOT_EMPTY;
                }
                else if (errorCode == (ushort)Win32Error.ERROR_ALREADY_EXISTS)
                {
                    // According to [MS-FSCC], FileRenameInformation MUST return STATUS_OBJECT_NAME_COLLISION when the specified name already exists and ReplaceIfExists is zero.
                    return NTStatus.STATUS_OBJECT_NAME_COLLISION;
                }
                else
                {
                    return NTStatus.STATUS_DATA_ERROR;
                }
            }
            else if (exception is UnauthorizedAccessException)
            {
                return NTStatus.STATUS_ACCESS_DENIED;
            }
            else
            {
                return NTStatus.STATUS_DATA_ERROR;
            }
        }

        /// <summary>
        /// Will return a virtual allocation size, assuming 4096 bytes per cluster
        /// </summary>
        public static ulong GetAllocationSize(ulong size)
        {
            return (ulong)Math.Ceiling((double)size / ClusterSize) * ClusterSize;
        }

        public static string GetShortName(string fileName)
        {
            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string extension = System.IO.Path.GetExtension(fileName);
            if (fileNameWithoutExt.Length > 8 || extension.Length > 4)
            {
                if (fileNameWithoutExt.Length > 8)
                {
                    fileNameWithoutExt = fileNameWithoutExt.Substring(0, 8);
                }

                if (extension.Length > 4)
                {
                    extension = extension.Substring(0, 4);
                }

                return fileNameWithoutExt + extension;
            }
            else
            {
                return fileName;
            }
        }
    }
}
