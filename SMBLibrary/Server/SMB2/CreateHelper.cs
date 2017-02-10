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

            FileAccess createAccess = NTFileStoreHelper.ToCreateFileAccess(request.DesiredAccess, request.CreateDisposition);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasAccess(session.UserName, path, createAccess, state.ClientEndPoint))
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                }
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
                    return CreateResponseForNamedPipe(persistentFileID.Value, FileStatus.FILE_OPENED);
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
                Stream stream;
                FileStatus fileStatus;
                NTStatus createStatus = NTFileSystemHelper.CreateFile(out entry, out stream, out fileStatus, fileSystemShare.FileSystem, path, request.DesiredAccess, request.ShareAccess, request.CreateDisposition, request.CreateOptions, state);
                if (createStatus != NTStatus.STATUS_SUCCESS)
                {
                    return new ErrorResponse(request.CommandName, createStatus);
                }

                bool deleteOnClose = (stream != null) && ((request.CreateOptions & CreateOptions.FILE_DELETE_ON_CLOSE) > 0);
                ulong? persistentFileID = session.AddOpenFile(path, stream, deleteOnClose);
                if (!persistentFileID.HasValue)
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_OPENED_FILES);
                }

                CreateResponse response = CreateResponseFromFileSystemEntry(entry, persistentFileID.Value, fileStatus);
                if (request.RequestedOplockLevel == OplockLevel.Batch)
                {
                    response.OplockLevel = OplockLevel.Batch;
                }
                return response;
            }
        }

        private static CreateResponse CreateResponseForNamedPipe(ulong persistentFileID, FileStatus fileStatus)
        {
            CreateResponse response = new CreateResponse();
            response.CreateAction = (CreateAction)fileStatus;
            response.FileAttributes = FileAttributes.Normal;
            response.FileId.Persistent = persistentFileID;
            return response;
        }

        private static CreateResponse CreateResponseFromFileSystemEntry(FileSystemEntry entry, ulong persistentFileID, FileStatus fileStatus)
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
            response.CreateAction = (CreateAction)fileStatus;
            response.CreationTime = entry.CreationTime;
            response.LastWriteTime = entry.LastWriteTime;
            response.ChangeTime = entry.LastWriteTime;
            response.LastAccessTime = entry.LastAccessTime;
            response.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
            response.EndofFile = (long)entry.Size;
            response.FileId.Persistent = persistentFileID;
            return response;
        }
    }
}
