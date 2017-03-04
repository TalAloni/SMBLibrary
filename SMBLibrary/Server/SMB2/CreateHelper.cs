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
                if (!((FileSystemShare)share).HasAccess(session.SecurityContext, path, createAccess))
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                }
            }

            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = share.FileStore.CreateFile(out handle, out fileStatus, path, request.DesiredAccess, request.ShareAccess, request.CreateDisposition, request.CreateOptions, session.SecurityContext);
            if (createStatus != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName, createStatus);
            }

            ulong? persistentFileID = session.AddOpenFile(request.Header.TreeID, path, handle);
            if (!persistentFileID.HasValue)
            {
                share.FileStore.CloseFile(handle);
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_TOO_MANY_OPENED_FILES);
            }

            if (share is NamedPipeShare)
            {
                return CreateResponseForNamedPipe(persistentFileID.Value, FileStatus.FILE_OPENED);
            }
            else
            {
                FileNetworkOpenInformation fileInfo = NTFileStoreHelper.GetNetworkOpenInformation(share.FileStore, handle);
                CreateResponse response = CreateResponseFromFileSystemEntry(fileInfo, persistentFileID.Value, fileStatus);
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

        private static CreateResponse CreateResponseFromFileSystemEntry(FileNetworkOpenInformation fileInfo, ulong persistentFileID, FileStatus fileStatus)
        {
            CreateResponse response = new CreateResponse();
            response.CreateAction = (CreateAction)fileStatus;
            response.CreationTime = fileInfo.CreationTime;
            response.LastWriteTime = fileInfo.LastWriteTime;
            response.ChangeTime = fileInfo.LastWriteTime;
            response.LastAccessTime = fileInfo.LastAccessTime;
            response.AllocationSize = fileInfo.AllocationSize;
            response.EndofFile = fileInfo.EndOfFile;
            response.FileAttributes = fileInfo.FileAttributes;
            response.FileId.Persistent = persistentFileID;
            return response;
        }
    }
}
