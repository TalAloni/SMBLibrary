/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SMBLibrary.Services;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class NTCreateHelper
    {
        internal static SMB1Command GetNTCreateResponse(SMB1Header header, NTCreateAndXRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            bool isExtended = (request.Flags & NTCreateFlags.NT_CREATE_REQUEST_EXTENDED_RESPONSE) > 0;
            string path = request.FileName;
            if (share is NamedPipeShare)
            {
                Stream pipeStream = ((NamedPipeShare)share).OpenPipe(path);
                if (pipeStream != null)
                {
                    ushort? fileID = session.AddOpenFile(path, pipeStream);
                    if (!fileID.HasValue)
                    {
                        header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                        return new ErrorResponse(request.CommandName);
                    }
                    if (isExtended)
                    {
                        return CreateResponseExtendedForNamedPipe(fileID.Value);
                    }
                    else
                    {
                        return CreateResponseForNamedPipe(fileID.Value);
                    }
                }

                header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                return new ErrorResponse(request.CommandName);
            }
            else // FileSystemShare
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;
                FileAccess createAccess = NTFileSystemHelper.ToCreateFileAccess(request.DesiredAccess, request.CreateDisposition);
                if (!fileSystemShare.HasAccess(session.UserName, path, createAccess, state.ClientEndPoint))
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }

                FileSystemEntry entry;
                NTStatus createStatus = NTFileSystemHelper.CreateFile(out entry, fileSystemShare.FileSystem, path, request.DesiredAccess, request.CreateDisposition, request.CreateOptions, state);
                if (createStatus != NTStatus.STATUS_SUCCESS)
                {
                    header.Status = createStatus;
                    return new ErrorResponse(request.CommandName);
                }

                FileAccess fileAccess = NTFileSystemHelper.ToFileAccess(request.DesiredAccess);

                Stream stream;
                bool deleteOnClose = false;
                if (fileAccess == (FileAccess)0 || entry.IsDirectory)
                {
                    stream = null;
                }
                else
                {
                    IFileSystem fileSystem = fileSystemShare.FileSystem;
                    // When FILE_OPEN_REPARSE_POINT is specified, the operation should continue normally if the file is not a reparse point.
                    // FILE_OPEN_REPARSE_POINT is a hint that the caller does not intend to actually read the file, with the exception
                    // of a file copy operation (where the caller will attempt to simply copy the reparse point).
                    deleteOnClose = (request.CreateOptions & CreateOptions.FILE_DELETE_ON_CLOSE) > 0;
                    bool openReparsePoint = (request.CreateOptions & CreateOptions.FILE_OPEN_REPARSE_POINT) > 0;
                    bool disableBuffering = (request.CreateOptions & CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING) > 0;
                    bool buffered = (request.CreateOptions & CreateOptions.FILE_SEQUENTIAL_ONLY) > 0 && !disableBuffering && !openReparsePoint;
                    NTStatus openStatus = NTFileSystemHelper.OpenFile(out stream, fileSystem, path, fileAccess, request.ShareAccess, buffered, state);
                    if (openStatus != NTStatus.STATUS_SUCCESS)
                    {
                        header.Status = openStatus;
                        return new ErrorResponse(request.CommandName);
                    }
                }

                ushort? fileID = session.AddOpenFile(path, stream, deleteOnClose);
                if (!fileID.HasValue)
                {
                    header.Status = NTStatus.STATUS_TOO_MANY_OPENED_FILES;
                    return new ErrorResponse(request.CommandName);
                }
                if (isExtended)
                {
                    NTCreateAndXResponseExtended response = CreateResponseExtendedFromFileSystemEntry(entry, fileID.Value);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
                else
                {
                    NTCreateAndXResponse response = CreateResponseFromFileSystemEntry(entry, fileID.Value);
                    if ((request.Flags & NTCreateFlags.NT_CREATE_REQUEST_OPBATCH) > 0)
                    {
                        response.OpLockLevel = OpLockLevel.BatchOpLockGranted;
                    }
                    return response;
                }
            }
        }

        private static NTCreateAndXResponse CreateResponseForNamedPipe(ushort fileID)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            response.NMPipeStatus.ICount = 255;
            response.NMPipeStatus.ReadMode = ReadMode.MessageMode;
            response.NMPipeStatus.NamedPipeType = NamedPipeType.MessageNodePipe;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedForNamedPipe(ushort fileID)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            response.ResourceType = ResourceType.FileTypeMessageModePipe;
            NamedPipeStatus status = new NamedPipeStatus();
            status.ICount = 255;
            status.ReadMode = ReadMode.MessageMode;
            status.NamedPipeType = NamedPipeType.MessageNodePipe;
            response.NMPipeStatus = status;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }

        private static NTCreateAndXResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ushort fileID)
        {
            NTCreateAndXResponse response = new NTCreateAndXResponse();
            if (entry.IsDirectory)
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Directory;
                response.Directory = true;
            }
            else
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            }
            response.FID = fileID;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
            response.EndOfFile = (long)entry.Size;
            response.CreateTime = entry.CreationTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.LastChangeTime = entry.LastWriteTime;
            response.ResourceType = ResourceType.FileTypeDisk;
            return response;
        }

        private static NTCreateAndXResponseExtended CreateResponseExtendedFromFileSystemEntry(FileSystemEntry entry, ushort fileID)
        {
            NTCreateAndXResponseExtended response = new NTCreateAndXResponseExtended();
            if (entry.IsDirectory)
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Directory;
                response.Directory = true;
            }
            else
            {
                response.ExtFileAttributes = ExtendedFileAttributes.Normal;
            }
            response.FID = fileID;
            response.CreateTime = entry.CreationTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.LastChangeTime = entry.LastWriteTime;
            response.CreateDisposition = CreateDisposition.FILE_OPEN;
            response.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
            response.EndOfFile = (long)entry.Size;
            response.ResourceType = ResourceType.FileTypeDisk;
            response.FileStatus = FileStatus.NO_EAS | FileStatus.NO_SUBSTREAMS | FileStatus.NO_REPARSETAG;
            response.MaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                FileAccessMask.FILE_EXECUTE |
                                                FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                    FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                    FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                    FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            return response;
        }
    }
}
