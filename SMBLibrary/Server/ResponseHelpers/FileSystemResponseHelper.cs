/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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

namespace SMBLibrary.Server
{
    public class FileSystemResponseHelper
    {
        internal static SMBCommand GetCreateDirectoryResponse(SMBHeader header, CreateDirectoryRequest request, FileSystemShare share, StateObject state)
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
                System.Diagnostics.Debug.Print("[{0}] CreateDirectory: Cannot create '{1}'", DateTime.Now.ToString("HH:mm:ss:ffff"), request.DirectoryName);
                header.Status = NTStatus.STATUS_OBJECT_NAME_INVALID;
                return new ErrorResponse(CommandName.SMB_COM_CREATE_DIRECTORY);
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.Print("[{0}] CreateDirectory: Cannot create '{1}', Access Denied", DateTime.Now.ToString("HH:mm:ss:ffff"), request.DirectoryName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_CREATE_DIRECTORY);
            }

            return new CreateDirectoryResponse();
        }

        internal static SMBCommand GetDeleteDirectoryResponse(SMBHeader header, DeleteDirectoryRequest request, FileSystemShare share, StateObject state)
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
                System.Diagnostics.Debug.Print("[{0}] DeleteDirectory: Cannot delete '{1}'", DateTime.Now.ToString("HH:mm:ss:ffff"), request.DirectoryName);
                header.Status = NTStatus.STATUS_CANNOT_DELETE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.Print("[{0}] DeleteDirectory: Cannot delete '{1}', Access Denied", DateTime.Now.ToString("HH:mm:ss:ffff"), request.DirectoryName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE_DIRECTORY);
            }
        }

        internal static SMBCommand GetCheckDirectoryResponse(SMBHeader header, CheckDirectoryRequest request, FileSystemShare share)
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

        internal static SMBCommand GetDeleteResponse(SMBHeader header, DeleteRequest request, FileSystemShare share, StateObject state)
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

            if (!entry.IsDirectory && (request.SearchAttributes & SMBLibrary.SMB1.FileAttributes.Directory) > 0
                || entry.IsDirectory && (request.SearchAttributes & SMBLibrary.SMB1.FileAttributes.Directory) == 0)
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
                System.Diagnostics.Debug.Print("[{0}] Delete: Cannot delete '{1}'", DateTime.Now.ToString("HH:mm:ss:ffff"), request.FileName);
                header.Status = NTStatus.STATUS_CANNOT_DELETE;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.Print("[{0}] DeleteDirectory: Cannot delete '{1}', Access Denied", DateTime.Now.ToString("HH:mm:ss:ffff"), request.FileName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_DELETE);
            }
        }

        internal static SMBCommand GetRenameResponse(SMBHeader header, RenameRequest request, FileSystemShare share, StateObject state)
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
                System.Diagnostics.Debug.Print("[{0}] Rename: Sharing violation renaming '{1}'", DateTime.Now.ToString("HH:mm:ss:ffff"), request.OldFileName);
                header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.Print("[{0}] Rename: Cannot rename '{1}', Access Denied", DateTime.Now.ToString("HH:mm:ss:ffff"), request.OldFileName);
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return new ErrorResponse(CommandName.SMB_COM_RENAME);
            }
        }

        internal static SMBCommand GetQueryInformationResponse(SMBHeader header, QueryInformationRequest request, FileSystemShare share)
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

        internal static SMBCommand GetSetInformationResponse(SMBHeader header, SetInformationRequest request, FileSystemShare share, StateObject state)
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
            if ((request.FileAttributes & SMBLibrary.SMB1.FileAttributes.Hidden) > 0)
            {
                isHidden = true;
            }
            if ((request.FileAttributes & SMBLibrary.SMB1.FileAttributes.ReadOnly) > 0)
            {
                isReadOnly = true;
            }
            if ((request.FileAttributes & SMBLibrary.SMB1.FileAttributes.Archive) > 0)
            {
                isArchived = true;
            }
            fileSystem.SetAttributes(request.FileName, isHidden, isReadOnly, isArchived);

            if (request.LastWriteTime != SMBHelper.UTimeNotSpecified)
            {
                fileSystem.SetDates(request.FileName, null, request.LastWriteTime, null);
            }

            return new SetInformationResponse();
        }

        internal static SMBCommand GetSetInformation2Response(SMBHeader header, SetInformation2Request request, FileSystemShare share, StateObject state)
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
