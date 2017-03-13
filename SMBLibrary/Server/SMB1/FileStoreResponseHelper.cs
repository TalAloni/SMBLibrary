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
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    internal class FileStoreResponseHelper
    {
        internal static SMB1Command GetCreateDirectoryResponse(SMB1Header header, CreateDirectoryRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.DirectoryName))
                {
                    state.LogToServer(Severity.Verbose, "Create Directory '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.DirectoryName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.CreateDirectory(share.FileStore, request.DirectoryName, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            return new CreateDirectoryResponse();
        }

        internal static SMB1Command GetDeleteDirectoryResponse(SMB1Header header, DeleteDirectoryRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.DirectoryName))
                {
                    state.LogToServer(Severity.Verbose, "Delete Directory '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.DirectoryName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.DeleteDirectory(share.FileStore, request.DirectoryName, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }
            return new DeleteDirectoryResponse();
        }

        internal static SMB1Command GetDeleteResponse(SMB1Header header, DeleteRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.FileName))
                {
                    state.LogToServer(Severity.Verbose, "Delete '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.FileName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            // [MS-CIFS] This command cannot delete directories or volumes.
            header.Status = SMB1FileStoreHelper.DeleteFile(share.FileStore, request.FileName, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }
            return new DeleteResponse();
        }

        internal static SMB1Command GetRenameResponse(SMB1Header header, RenameRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.OldFileName))
                {
                    state.LogToServer(Severity.Verbose, "Rename '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.OldFileName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.NewFileName))
                {
                    state.LogToServer(Severity.Verbose, "Rename '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.OldFileName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.Rename(share.FileStore, request.OldFileName, request.NewFileName, request.SearchAttributes, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }
            return new RenameResponse();
        }

        internal static SMB1Command GetCheckDirectoryResponse(SMB1Header header, CheckDirectoryRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, request.DirectoryName))
                {
                    state.LogToServer(Severity.Verbose, "Check Directory '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.DirectoryName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.CheckDirectory(share.FileStore, request.DirectoryName, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            return new CheckDirectoryResponse();
        }

        internal static SMB1Command GetQueryInformationResponse(SMB1Header header, QueryInformationRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, request.FileName))
                {
                    state.LogToServer(Severity.Verbose, "Query Information on '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.FileName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            FileNetworkOpenInformation fileInfo;
            header.Status = SMB1FileStoreHelper.QueryInformation(out fileInfo, share.FileStore, request.FileName, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            QueryInformationResponse response = new QueryInformationResponse();
            response.FileAttributes = SMB1FileStoreHelper.GetFileAttributes(fileInfo.FileAttributes);
            response.LastWriteTime = fileInfo.LastWriteTime;
            response.FileSize = (uint)Math.Min(UInt32.MaxValue, fileInfo.EndOfFile);
            return response;
        }

        internal static SMB1Command GetSetInformationResponse(SMB1Header header, SetInformationRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, request.FileName))
                {
                    state.LogToServer(Severity.Verbose, "Set Information on '{0}{1}' failed. User '{2}' was denied access.", share.Name, request.FileName, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.SetInformation(share.FileStore, request.FileName, request.FileAttributes, request.LastWriteTime, session.SecurityContext);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            return new SetInformationResponse();
        }

        internal static SMB1Command GetSetInformation2Response(SMB1Header header, SetInformation2Request request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_SMB_BAD_FID;
                return new ErrorResponse(request.CommandName);
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "Set Information 2 on '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }

            header.Status = SMB1FileStoreHelper.SetInformation2(share.FileStore, openFile.Handle, request.CreationDateTime, request.LastAccessDateTime, request.LastWriteDateTime);
            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName);
            }

            return new SetInformation2Response();
        }
    }
}
