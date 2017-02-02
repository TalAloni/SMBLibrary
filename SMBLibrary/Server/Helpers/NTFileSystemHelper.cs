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

        public static NTStatus CreateFile(out FileSystemEntry entry, FileSystemShare share, string userName, string path, CreateDisposition createDisposition, CreateOptions createOptions, AccessMask desiredAccess, ConnectionState state)
        {
            bool hasWriteAccess = share.HasWriteAccess(userName);
            IFileSystem fileSystem = share.FileSystem;

            bool forceDirectory = (createOptions & CreateOptions.FILE_DIRECTORY_FILE) > 0;
            bool forceFile = (createOptions & CreateOptions.FILE_NON_DIRECTORY_FILE) > 0;

            if (forceDirectory & (createDisposition != CreateDisposition.FILE_CREATE &&
                                  createDisposition != CreateDisposition.FILE_OPEN &&
                                  createDisposition != CreateDisposition.FILE_OPEN_IF))
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

            entry = fileSystem.GetEntry(path);
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

                if (!hasWriteAccess)
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
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. Sharing violation.", path);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. Data Error.", path);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "CreateFile: Error creating '{0}'. Access Denied.", path);
                    return NTStatus.STATUS_ACCESS_DENIED;
                }
            }
            else if (createDisposition == CreateDisposition.FILE_OPEN_IF ||
                     createDisposition == CreateDisposition.FILE_OVERWRITE ||
                     createDisposition == CreateDisposition.FILE_OVERWRITE_IF ||
                     createDisposition == CreateDisposition.FILE_SUPERSEDE)
            {
                entry = fileSystem.GetEntry(path);
                if (entry == null)
                {
                    if (createDisposition == CreateDisposition.FILE_OVERWRITE)
                    {
                        return NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    }

                    if (!hasWriteAccess)
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
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            return NTStatus.STATUS_SHARING_VIOLATION;
                        }
                        else
                        {
                            return NTStatus.STATUS_DATA_ERROR;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return NTStatus.STATUS_ACCESS_DENIED;
                    }
                }
                else
                {
                    if (createDisposition == CreateDisposition.FILE_OVERWRITE ||
                        createDisposition == CreateDisposition.FILE_OVERWRITE_IF ||
                        createDisposition == CreateDisposition.FILE_SUPERSEDE)
                    {
                        if (!hasWriteAccess)
                        {
                            return NTStatus.STATUS_ACCESS_DENIED;
                        }

                        // Truncate the file
                        try
                        {
                            Stream temp = fileSystem.OpenFile(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                            temp.Close();
                        }
                        catch (IOException ex)
                        {
                            ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                            if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                            {
                                return NTStatus.STATUS_SHARING_VIOLATION;
                            }
                            else
                            {
                                return NTStatus.STATUS_DATA_ERROR;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return NTStatus.STATUS_ACCESS_DENIED;
                        }
                    }
                }
            }
            else
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            FileAccess fileAccess = ToFileAccess(desiredAccess.File);
            if (!hasWriteAccess && (fileAccess == FileAccess.Write || fileAccess == FileAccess.ReadWrite))
            {
                return NTStatus.STATUS_ACCESS_DENIED;
            }

            return NTStatus.STATUS_SUCCESS;
        }

        public static FileAccess ToFileAccess(FileAccessMask desiredAccess)
        {
            if ((desiredAccess & FileAccessMask.GENERIC_ALL) > 0 ||
                ((desiredAccess & FileAccessMask.FILE_READ_DATA) > 0 && (desiredAccess & FileAccessMask.FILE_WRITE_DATA) > 0) ||
                ((desiredAccess & FileAccessMask.FILE_READ_DATA) > 0 && (desiredAccess & FileAccessMask.FILE_APPEND_DATA) > 0))
            {
                return FileAccess.ReadWrite;
            }
            else if ((desiredAccess & FileAccessMask.GENERIC_WRITE) > 0 ||
                     (desiredAccess & FileAccessMask.FILE_WRITE_DATA) > 0 ||
                     (desiredAccess & FileAccessMask.FILE_APPEND_DATA) > 0)
            {
                return FileAccess.Write;
            }
            else if ((desiredAccess & FileAccessMask.FILE_READ_DATA) > 0)
            {
                return FileAccess.Read;
            }
            else
            {
                return (FileAccess)0;
            }
        }

        public static FileShare ToFileShare(ShareAccess shareAccess)
        {
            if ((shareAccess & ShareAccess.FILE_SHARE_READ) > 0 && (shareAccess & ShareAccess.FILE_SHARE_WRITE) > 0)
            {
                return FileShare.ReadWrite;
            }
            else if ((shareAccess & ShareAccess.FILE_SHARE_WRITE) > 0)
            {
                return FileShare.Write;
            }
            else if ((shareAccess & ShareAccess.FILE_SHARE_READ) > 0)
            {
                return FileShare.Read;
            }
            else if ((shareAccess & ShareAccess.FILE_SHARE_DELETE) > 0)
            {
                return FileShare.Delete;
            }
            else
            {
                return FileShare.None;
            }
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
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Sharing Violation.", openFilePath);
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Data Error.", openFilePath);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}'. Offset Out Of Range.", openFilePath);
                    return NTStatus.STATUS_DATA_ERROR;
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "ReadFile: Cannot read '{0}', Access Denied.", openFilePath);
                    return NTStatus.STATUS_ACCESS_DENIED;
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
                    numberOfBytesWritten = data.Length;
                    return NTStatus.STATUS_SUCCESS;
                }
                catch (IOException ex)
                {
                    ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                    if (errorCode == (ushort)Win32Error.ERROR_DISK_FULL)
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Disk Full.", openFilePath);
                        return NTStatus.STATUS_DISK_FULL;
                    }
                    else if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Sharing Violation.", openFilePath);
                        // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                        return NTStatus.STATUS_SHARING_VIOLATION;
                    }
                    else
                    {
                        state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Data Error.", openFilePath);
                        return NTStatus.STATUS_DATA_ERROR;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Offset Out Of Range.", openFilePath);
                    return NTStatus.STATUS_DATA_ERROR;
                }
                catch (UnauthorizedAccessException)
                {
                    state.LogToServer(Severity.Debug, "WriteFile: Cannot write '{0}'. Access Denied.", openFilePath);
                    // The user may have tried to write to a readonly file
                    return NTStatus.STATUS_ACCESS_DENIED;
                }
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
