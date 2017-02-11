/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    public class QueryInfoHelper
    {
        internal static SMB2Command GetQueryInfoResponse(QueryInfoRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            if (request.InfoType == InfoType.File)
            {
                OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
                if (openFile == null)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
                }

                if (share is FileSystemShare)
                {
                    if (!((FileSystemShare)share).HasReadAccess(session.UserName, openFile.Path, state.ClientEndPoint))
                    {
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }
                }

                FileInformation fileInformation;
                NTStatus queryStatus = share.FileStore.GetFileInformation(out fileInformation, openFile.Handle, request.FileInformationClass);
                if (queryStatus != NTStatus.STATUS_SUCCESS)
                {
                    state.LogToServer(Severity.Verbose, "GetFileInformation on '{0}' failed. Information class: {1}, NTStatus: {2}", openFile.Path, request.FileInformationClass, queryStatus);
                    return new ErrorResponse(request.CommandName, queryStatus);
                }

                QueryInfoResponse response = new QueryInfoResponse();
                response.SetFileInformation(fileInformation);
                return response;
            }
            else if (request.InfoType == InfoType.FileSystem)
            {
                if (share is FileSystemShare)
                {
                    if (!((FileSystemShare)share).HasReadAccess(session.UserName, @"\", state.ClientEndPoint))
                    {
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }

                    FileSystemInformation fileSystemInformation;
                    NTStatus queryStatus = share.FileStore.GetFileSystemInformation(out fileSystemInformation, request.FileSystemInformationClass);
                    if (queryStatus != NTStatus.STATUS_SUCCESS)
                    {
                        state.LogToServer(Severity.Verbose, "GetFileSystemInformation failed. Information class: {0}, NTStatus: {1}", request.FileSystemInformationClass, queryStatus);
                        return new ErrorResponse(request.CommandName, queryStatus);
                    }
                    QueryInfoResponse response = new QueryInfoResponse();
                    response.SetFileSystemInformation(fileSystemInformation);
                    return response;
                }
            }
            return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
        }
    }
}
