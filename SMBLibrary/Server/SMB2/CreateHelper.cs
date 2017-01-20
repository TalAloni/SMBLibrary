/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    public class CreateHelper
    {
        internal static SMB2Command GetCreateResponse(CreateRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            string path = request.Name;
            if (!path.StartsWith(@"\"))
            {
                path = @"\" + path;
            }
            if (share is NamedPipeShare)
            {
                Stream pipeStream = ((NamedPipeShare)share).OpenPipe(path);
                if (pipeStream != null)
                {
                    ulong? persistentFileID = session.AddOpenFile(path, pipeStream);
                    if (!persistentFileID.HasValue)
                    {
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_OPENED_FILES);
                    }
                    return CreateResponseForNamedPipe(persistentFileID.Value);
                }
                else
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_OBJECT_PATH_NOT_FOUND);
                }
            }
            else
            {
                FileSystemShare fileSystemShare = (FileSystemShare)share;

                FileSystemEntry entry;
                NTStatus createStatus = NTFileSystemHelper.CreateFile(out entry, (FileSystemShare)share, session.UserName, path, request.CreateDisposition, request.CreateOptions, request.DesiredAccess, state);
                if (createStatus != NTStatus.STATUS_SUCCESS)
                {
                    return new ErrorResponse(request.CommandName, createStatus);
                }

                IFileSystem fileSystem = fileSystemShare.FileSystem;
                FileAccess fileAccess = NTFileSystemHelper.ToFileAccess(request.DesiredAccess.File);
                FileShare fileShare = NTFileSystemHelper.ToFileShare(request.ShareAccess);

                Stream stream;
                bool deleteOnClose = false;
                if (fileAccess == (FileAccess)0 || entry.IsDirectory)
                {
                    stream = null;
                }
                else
                {
                    // When FILE_OPEN_REPARSE_POINT is specified, the operation should continue normally if the file is not a reparse point.
                    // FILE_OPEN_REPARSE_POINT is a hint that the caller does not intend to actually read the file, with the exception
                    // of a file copy operation (where the caller will attempt to simply copy the reparse point).
                    deleteOnClose = (request.CreateOptions & CreateOptions.FILE_DELETE_ON_CLOSE) > 0;
                    bool openReparsePoint = (request.CreateOptions & CreateOptions.FILE_OPEN_REPARSE_POINT) > 0;
                    bool disableBuffering = (request.CreateOptions & CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING) > 0;
                    bool buffered = (request.CreateOptions & CreateOptions.FILE_SEQUENTIAL_ONLY) > 0 && !disableBuffering && !openReparsePoint;
                    state.LogToServer(Severity.Verbose, "Create: Opening '{0}', Access={1}, Share={2}, Buffered={3}", path, fileAccess, fileShare, buffered);
                    try
                    {
                        stream = fileSystem.OpenFile(path, FileMode.Open, fileAccess, fileShare);
                    }
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            state.LogToServer(Severity.Debug, "NTCreate: Sharing violation opening '{0}'", path);
                            return new ErrorResponse(request.CommandName, NTStatus.STATUS_SHARING_VIOLATION);
                        }
                        else
                        {
                            state.LogToServer(Severity.Debug, "NTCreate: Sharing violation opening '{0}', Data Error", path);
                            return new ErrorResponse(request.CommandName, NTStatus.STATUS_DATA_ERROR);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Debug, "NTCreate: Sharing violation opening '{0}', Access Denied", path);
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }
        
                    if (buffered)
                    {
                        stream = new PrefetchedStream(stream);
                    }
                }

                ulong? persistentFileID = session.AddOpenFile(path, stream, deleteOnClose);
                if (!persistentFileID.HasValue)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_OPENED_FILES);
                }

                CreateResponse response = CreateResponseFromFileSystemEntry(entry, persistentFileID.Value);
                if (request.RequestedOplockLevel == OplockLevel.Batch)
                {
                    response.OplockLevel = OplockLevel.Batch;
                }
                return response;
            }
        }

        private static CreateResponse CreateResponseForNamedPipe(ulong persistentFileID)
        {
            CreateResponse response = new CreateResponse();
            response.FileId.Persistent = persistentFileID;
            response.CreateAction = CreateAction.FILE_OPENED;
            response.FileAttributes = FileAttributes.Normal;
            return response;
        }

        private static CreateResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ulong persistentFileID)
        {
            CreateResponse response = new CreateResponse();
            if (entry.IsDirectory)
            {
                response.FileAttributes = FileAttributes.Directory;
            }
            else
            {
                response.FileAttributes = FileAttributes.Normal;
            }
            response.FileId.Persistent = persistentFileID;
            response.CreateAction = CreateAction.FILE_OPENED;
            response.CreationTime = entry.CreationTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.ChangeTime = entry.LastWriteTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
            response.EndofFile = entry.Size;
            return response;
        }
    }
}
