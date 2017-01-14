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
    public class FileSystemResponseHelper
    {
        internal static SMB1Command GetCreateDirectoryResponse(SMB1Header header, CreateDirectoryRequest request, FileSystemShare share, SMB1ConnectionState state)
        {
            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_CREATE_DIRECTORY);
            }
            IFileSystem fileSystem = share.FileSystem;

            try
            {
                fileSystem.CreateDirectory(request.DirectoryName);
            }
            catch (IOException)
            {
                state.LogToServer(Severity.Debug, "CreateDirectory: Cannot create '{0}'", request.DirectoryName);
                header.Status = NTStatus.STATUS_OBJECT_NAME_INVALID;
                return new ErrorResponse(CommandName.SMB_COM_CREATE_DIRECTORY);
            }
            catch (UnauthorizedAccessException)
            {
                state.LogToServer(Severity.Debug, "CreateDirectory: Cannot create '{0}', Access Denied", request.DirectoryName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_CREATE_DIRECTORY);
            }

            return new CreateDirectoryResponse();
        }

        internal static SMB1Command GetDeleteDirectoryResponse(SMB1Header header, DeleteDirectoryRequest request, FileSystemShare share, SMB1ConnectionState state)
        {
            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }
            IFileSystem fileSystem = share.FileSystem;

            FileSystemEntry entry = fileSystem.GetEntry(request.DirectoryName);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }

            if (!entry.IsDirectory)
            {
                header.Status = NTStatus.STATUS_OBJECT_PATH_INVALID;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }

            try
            {
                fileSystem.Delete(request.DirectoryName);
                return new DeleteDirectoryResponse();
            }
            catch (IOException)
            {
                state.LogToServer(Severity.Debug, "DeleteDirectory: Cannot delete '{0}'", request.DirectoryName);
                header.Status = NTStatus.STATUS_CANNOT_DELETE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }
            catch (UnauthorizedAccessException)
            {
                state.LogToServer(Severity.Debug, "DeleteDirectory: Cannot delete '{0}', Access Denied", request.DirectoryName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }
        }

        internal static SMB1Command GetCheckDirectoryResponse(SMB1Header header, CheckDirectoryRequest request, FileSystemShare share)
        {
            IFileSystem fileSystem = share.FileSystem;
            FileSystemEntry entry = fileSystem.GetEntry(request.DirectoryName);
            if (entry == null || !entry.IsDirectory)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return new ErrorResponse(CommandName.SMB_COM_CHECK_DIRECTORY);
            }

            return new CheckDirectoryResponse();
        }

        internal static SMB1Command GetDeleteResponse(SMB1Header header, DeleteRequest request, FileSystemShare share, SMB1ConnectionState state)
        {
            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }
            IFileSystem fileSystem = share.FileSystem;

            FileSystemEntry entry = fileSystem.GetEntry(request.FileName);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }

            if (!entry.IsDirectory && (request.SearchAttributes & SMBFileAttributes.Directory) > 0
                || entry.IsDirectory && (request.SearchAttributes & SMBFileAttributes.Directory) == 0)
            {
                header.Status = NTStatus.STATUS_OBJECT_PATH_INVALID;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }

            try
            {
                fileSystem.Delete(request.FileName);
                return new DeleteResponse();
            }
            catch (IOException)
            {
                state.LogToServer(Severity.Debug, "Delete: Cannot delete '{0}'", request.FileName);
                header.Status = NTStatus.STATUS_CANNOT_DELETE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }
            catch (UnauthorizedAccessException)
            {
                state.LogToServer(Severity.Debug, "DeleteDirectory: Cannot delete '{0}', Access Denied", request.FileName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }
        }

        internal static SMB1Command GetRenameResponse(SMB1Header header, RenameRequest request, FileSystemShare share, SMB1ConnectionState state)
        {
            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }
            IFileSystem fileSystem = share.FileSystem;

            FileSystemEntry sourceEntry = fileSystem.GetEntry(request.OldFileName);
            if (sourceEntry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }

            // The file must not already exist unless we just want to upcase / downcase a filename letter
            FileSystemEntry destinationEntry = fileSystem.GetEntry(request.NewFileName);
            if (destinationEntry != null &&
                !String.Equals(request.OldFileName, request.NewFileName, StringComparison.InvariantCultureIgnoreCase))
            {
                // The new file already exists.
                header.Status = NTStatus.STATUS_OBJECT_NAME_COLLISION;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }

            try
            {
                fileSystem.Move(request.OldFileName, request.NewFileName);
                return new RenameResponse();
            }
            catch (IOException)
            {
                state.LogToServer(Severity.Debug, "Rename: Sharing violation renaming '{0}'", request.OldFileName);
                header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }
            catch (UnauthorizedAccessException)
            {
                state.LogToServer(Severity.Debug, "Rename: Cannot rename '{0}', Access Denied", request.OldFileName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }
        }

        internal static SMB1Command GetQueryInformationResponse(SMB1Header header, QueryInformationRequest request, FileSystemShare share)
        {
            IFileSystem fileSystem = share.FileSystem;
            FileSystemEntry entry = fileSystem.GetEntry(request.FileName);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_OBJECT_PATH_INVALID;
                return new ErrorResponse(CommandName.SMB_COM_QUERY_INFORMATION);
            }

            QueryInformationResponse response = new QueryInformationResponse();
            response.FileAttributes = InfoHelper.GetFileAttributes(entry);
            response.LastWriteTime = entry.LastWriteTime;
            response.FileSize = (uint)Math.Min(UInt32.MaxValue, entry.Size);

            return response;
        }

        internal static SMB1Command GetSetInformationResponse(SMB1Header header, SetInformationRequest request, FileSystemShare share, SMB1ConnectionState state)
        {
            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_SET_INFORMATION2);
            }
            IFileSystem fileSystem = share.FileSystem;

            FileSystemEntry entry = fileSystem.GetEntry(request.FileName);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return new ErrorResponse(CommandName.SMB_COM_SET_INFORMATION);
            }

            bool? isHidden = null;
            bool? isReadOnly = null;
            bool? isArchived = null;
            if ((request.FileAttributes & SMBFileAttributes.Hidden) > 0)
            {
                isHidden = true;
            }
            if ((request.FileAttributes & SMBFileAttributes.ReadOnly) > 0)
            {
                isReadOnly = true;
            }
            if ((request.FileAttributes & SMBFileAttributes.Archive) > 0)
            {
                isArchived = true;
            }
            fileSystem.SetAttributes(request.FileName, isHidden, isReadOnly, isArchived);

            if (request.LastWriteTime.HasValue)
            {
                fileSystem.SetDates(request.FileName, null, request.LastWriteTime, null);
            }

            return new SetInformationResponse();
        }

        internal static SMB1Command GetSetInformation2Response(SMB1Header header, SetInformation2Request request, FileSystemShare share, SMB1ConnectionState state)
        {
            string openedFilePath = state.GetOpenedFilePath(request.FID);
            if (openedFilePath == null)
            {
                header.Status = NTStatus.STATUS_SMB_BAD_FID;
                return new ErrorResponse(CommandName.SMB_COM_SET_INFORMATION2);
            }

            string userName = state.GetConnectedUserName(header.UID);
            if (!share.HasWriteAccess(userName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_SET_INFORMATION2);
            }
            IFileSystem fileSystem = share.FileSystem;

            fileSystem.SetDates(openedFilePath, request.CreationDateTime, request.LastWriteDateTime, request.LastAccessDateTime);
            return new SetInformation2Response();
        }
    }
}
