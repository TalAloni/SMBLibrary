/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Server
{
    /// <summary>
    /// Helper class to access the FileSystemShare / NamedPipeShare in an NT-like manner dictated by the SMB protocol
    /// </summary>
    public partial class NTFileSystemHelper
    {
        public const int BytesPerSector = 512;
        public const int ClusterSize = 4096;

        public static NTStatus CreateFile(out FileSystemEntry entry, IFileSystem fileSystem, string path, AccessMask desiredAccess, CreateDisposition createDisposition, CreateOptions createOptions, ConnectionState state)
        {
            FileAccess createAccess = ToCreateFileAccess(desiredAccess, createDisposition);
            bool requestedWriteAccess = (createAccess & FileAccess.Write) > 0;

            bool forceDirectory = (createOptions & CreateOptions.FILE_DIRECTORY_FILE) > 0;
            bool forceFile = (createOptions & CreateOptions.FILE_NON_DIRECTORY_FILE) > 0;

            if (forceDirectory & (createDisposition != CreateDisposition.FILE_CREATE &&
                                  createDisposition != CreateDisposition.FILE_OPEN &&
                                  createDisposition != CreateDisposition.FILE_OPEN_IF &&
                                  createDisposition != CreateDisposition.FILE_SUPERSEDE))
            {
                entry = null;
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            // Windows will try to access named streams (alternate data streams) regardless of the FILE_NAMED_STREAMS flag, we need to prevent this behaviour.
            if (path.Contains(":"))
            {
                // Windows Server 2003 will return STATUS_OBJECT_NAME_NOT_FOUND
                entry = null;
                return NTStatus.STATUS_NO_SUCH_FILE;
            }

            try
            {
                entry = fileSystem.GetEntry(path);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                state.LogToServer(Severity.Debug, "CreateFile: Error retrieving '{0}'. {1}.", path, status);
                entry = null;
                return status;
            }
            
            if (createDisposition == CreateDisposition.FILE_OPEN)
            {
                if (entry == null)
                {
                    return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                }

                if (entry.IsDirectory && forceFile)
                {
                    return NTStatus.STATUS_FILE_IS_A_DIRECTORY;
                }

                if (!entry.IsDirectory && forceDirectory)
                {
                    // Not sure if that's the correct response
                    return NTStatus.STATUS_OBJECT_NAME_COLLISION;
                }
            }
            else if (createDisposition == CreateDisposition.FILE_CREATE)
            {
                if (entry != null)
                {
                    // File already exists, fail the request
                    state.LogToServer(Severity.Debug, "CreateFile: File '{0}' already exist", path);
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
                        state.LogToServer(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                        entry = fileSystem.CreateDirectory(path);
                    }
                    else
                    {
                        state.LogToServer(Severity.Information, "CreateFile: Creating file '{0}'", path);
                        entry = fileSystem.CreateFile(path);
                    }
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                    return status;
                }
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
                            state.LogToServer(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                            entry = fileSystem.CreateDirectory(path);
                        }
                        else
                        {
                            state.LogToServer(Severity.Information, "CreateFile: Creating file '{0}'", path);
                            entry = fileSystem.CreateFile(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        NTStatus status = ToNTStatus(ex);
                        state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                        return status;
                    }
                }
                else
                {
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
                            Stream temp = fileSystem.OpenFile(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                            temp.Close();
                        }
                        catch (Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            state.LogToServer(Severity.Debug, "CreateFile: Error truncating '{0}'. {1}.", path, status);
                            return status;
                        }
                    }
                    else if (createDisposition == CreateDisposition.FILE_SUPERSEDE)
                    {
                        // Delete the old file
                        try
                        {
                            fileSystem.Delete(path);
                        }
                        catch(Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            state.LogToServer(Severity.Debug, "CreateFile: Error deleting '{0}'. {1}.", path, status);
                            return status;
                        }

                        try
                        {
                            if (forceDirectory)
                            {
                                state.LogToServer(Severity.Information, "CreateFile: Creating directory '{0}'", path);
                                entry = fileSystem.CreateDirectory(path);
                            }
                            else
                            {
                                state.LogToServer(Severity.Information, "CreateFile: Creating file '{0}'", path);
                                entry = fileSystem.CreateFile(path);
                            }
                        }
                        catch (Exception ex)
                        {
                            NTStatus status = ToNTStatus(ex);
                            state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. {1}.", path, status);
                            return status;
                        }
                    }
                }
            }
            else
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            return NTStatus.STATUS_SUCCESS;
        }

        public static NTStatus OpenFile(out Stream stream, IFileSystem fileSystem, string path, FileAccess fileAccess, ShareAccess shareAccess, bool buffered, ConnectionState state)
        {
            stream = null;
            FileShare fileShare = NTFileSystemHelper.ToFileShare(shareAccess);
            state.LogToServer(Severity.Verbose, "OpenFile: Opening '{0}', Access={1}, Share={2}, Buffered={3}", path, fileAccess, fileShare, buffered);
            try
            {
                stream = fileSystem.OpenFile(path, FileMode.Open, fileAccess, fileShare);
            }
            catch (Exception ex)
            {
                NTStatus status = ToNTStatus(ex);
                state.LogToServer(Severity.Debug, "OpenFile: Cannot open '{0}'. {1}.", path, status);
                return status;
            }

            if (buffered)
            {
                stream = new PrefetchedStream(stream);
            }

            return NTStatus.STATUS_SUCCESS;
        }

        public static NTStatus ReadFile(out byte[] data, OpenFileObject openFile, long offset, int maxCount, ConnectionState state)
        {
            data = null;
            string openFilePath = openFile.Path;
            Stream stream = openFile.Stream;
            if (stream is RPCPipeStream)
            {
                data = new byte[maxCount];
                int bytesRead = stream.Read(data, 0, maxCount);
                if (bytesRead < maxCount)
                {
                    // EOF, we must trim the response data array
                    data = ByteReader.ReadBytes(data, 0, bytesRead);
                }
                return NTStatus.STATUS_SUCCESS;
            }
            else // File
            {
                if (stream == null || !stream.CanRead)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}', Invalid Operation.", openFilePath);
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
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. {1}.", openFilePath, status);
                    return status;
                }

                if (bytesRead < maxCount)
                {
                    // EOF, we must trim the response data array
                    data = ByteReader.ReadBytes(data, 0, bytesRead);
                }
                return NTStatus.STATUS_SUCCESS;
            }
        }

        public static NTStatus WriteFile(out int numberOfBytesWritten, OpenFileObject openFile, long offset, byte[] data, ConnectionState state)
        {
            numberOfBytesWritten = 0;
            string openFilePath = openFile.Path;
            Stream stream = openFile.Stream;
            if (stream is RPCPipeStream)
            {
                stream.Write(data, 0, data.Length);
                numberOfBytesWritten = data.Length;
                return NTStatus.STATUS_SUCCESS;
            }
            else // File
            {
                if (stream == null || !stream.CanWrite)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Invalid Operation.", openFilePath);
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
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. {1}.", openFilePath, status);
                    return status;
                }
                numberOfBytesWritten = data.Length;
                return NTStatus.STATUS_SUCCESS;
            }
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
                else if (errorCode == (ushort)Win32Error.ERROR_ALREADY_EXISTS)
                {
                    return NTStatus.STATUS_OBJECT_NAME_EXISTS;
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

        public static FileAccess ToCreateFileAccess(AccessMask desiredAccess, CreateDisposition createDisposition)
        {
            FileAccess result = 0;

            if ((desiredAccess.File & FileAccessMask.FILE_READ_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_READ_EA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_READ_ATTRIBUTES) > 0 ||
                (desiredAccess.File & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_READ) > 0)
            {
                result |= FileAccess.Read;
            }

            if ((desiredAccess.File & FileAccessMask.FILE_WRITE_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_APPEND_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_WRITE_EA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_WRITE_ATTRIBUTES) > 0 ||
                (desiredAccess.File & FileAccessMask.DELETE) > 0 ||
                (desiredAccess.File & FileAccessMask.WRITE_DAC) > 0 ||
                (desiredAccess.File & FileAccessMask.WRITE_OWNER) > 0 ||
                (desiredAccess.File & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_WRITE) > 0)
            {
                result |= FileAccess.Write;
            }

            if ((desiredAccess.Directory & DirectoryAccessMask.FILE_DELETE_CHILD) > 0)
            {
                result |= FileAccess.Write;
            }

            if (createDisposition == CreateDisposition.FILE_CREATE ||
                createDisposition == CreateDisposition.FILE_SUPERSEDE)
            {
                result |= FileAccess.Write;
            }

            return result;
        }

        public static FileAccess ToFileAccess(FileAccessMask desiredAccess)
        {
            FileAccess result = 0;
            if ((desiredAccess & FileAccessMask.FILE_READ_DATA) > 0 ||
                (desiredAccess & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_READ) > 0)
            {
                result |= FileAccess.Read;
            }

            if ((desiredAccess & FileAccessMask.FILE_WRITE_DATA) > 0 ||
                (desiredAccess & FileAccessMask.FILE_APPEND_DATA) > 0 ||
                (desiredAccess & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_WRITE) > 0)
            {
                result |= FileAccess.Write;
            }

            return result;
        }

        public static FileShare ToFileShare(ShareAccess shareAccess)
        {
            FileShare result = FileShare.None;
            if ((shareAccess & ShareAccess.FILE_SHARE_READ) > 0)
            {
                result |= FileShare.Read;
            }

            if ((shareAccess & ShareAccess.FILE_SHARE_WRITE) > 0)
            {
                result |= FileShare.Write;
            }

            if ((shareAccess & ShareAccess.FILE_SHARE_DELETE) > 0)
            {
                result |= FileShare.Delete;
            }

            return result;
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
