/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public partial class SMB1FileStoreHelper
    {
        public static NTStatus CreateDirectory(INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus createStatus = fileStore.CreateFile(out handle, out fileStatus, path, DirectoryAccessMask.FILE_ADD_SUBDIRECTORY, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_CREATE, CreateOptions.FILE_DIRECTORY_FILE, securityContext);
            if (createStatus != NTStatus.STATUS_SUCCESS)
            {
                return createStatus;
            }
            fileStore.CloseFile(handle);
            return createStatus;
        }

        public static NTStatus DeleteDirectory(INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            return Delete(fileStore, path, CreateOptions.FILE_DIRECTORY_FILE, securityContext);
        }

        public static NTStatus DeleteFile(INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            return Delete(fileStore, path, CreateOptions.FILE_NON_DIRECTORY_FILE, securityContext);
        }

        public static NTStatus Delete(INTFileStore fileStore, string path, CreateOptions createOptions, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, DirectoryAccessMask.DELETE, 0, CreateDisposition.FILE_OPEN, createOptions, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                return openStatus;
            }
            FileDispositionInformation fileDispositionInfo = new FileDispositionInformation();
            fileDispositionInfo.DeletePending = true;
            NTStatus setStatus = fileStore.SetFileInformation(handle, fileDispositionInfo);
            if (setStatus != NTStatus.STATUS_SUCCESS)
            {
                return setStatus;
            }
            NTStatus closeStatus = fileStore.CloseFile(handle);
            return closeStatus;
        }

        public static NTStatus Rename(INTFileStore fileStore, string oldName, string newName, SMBFileAttributes searchAttributes, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            CreateOptions createOptions = 0;
            if (searchAttributes == SMBFileAttributes.Normal)
            {
                createOptions = CreateOptions.FILE_NON_DIRECTORY_FILE;
            }
            else if ((searchAttributes & SMBFileAttributes.Directory) > 0)
            {
                createOptions = CreateOptions.FILE_DIRECTORY_FILE;
            }
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, oldName, DirectoryAccessMask.DELETE, 0, CreateDisposition.FILE_OPEN, createOptions, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                return openStatus;
            }
            FileRenameInformationType2 renameInfo = new FileRenameInformationType2();
            renameInfo.ReplaceIfExists = false;
            renameInfo.FileName = newName;
            NTStatus setStatus = fileStore.SetFileInformation(handle, renameInfo);
            if (setStatus != NTStatus.STATUS_SUCCESS)
            {
                return setStatus;
            }
            NTStatus closeStatus = fileStore.CloseFile(handle);
            return closeStatus;
        }

        public static NTStatus CheckDirectory(INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, (AccessMask)0, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                return openStatus;
            }

            fileStore.CloseFile(handle);
            return NTStatus.STATUS_SUCCESS;
        }

        public static NTStatus QueryInformation(out FileNetworkOpenInformation fileInfo, INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, FileAccessMask.FILE_READ_ATTRIBUTES, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, 0, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                fileInfo = null;
                return openStatus;
            }

            fileInfo = NTFileStoreHelper.GetNetworkOpenInformation(fileStore, handle);
            return NTStatus.STATUS_SUCCESS;
        }

        public static NTStatus SetInformation(INTFileStore fileStore, string path, SMBFileAttributes fileAttributes, DateTime? lastWriteTime, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, FileAccessMask.FILE_WRITE_ATTRIBUTES, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, 0, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                return openStatus;
            }

            FileBasicInformation basicInfo = new FileBasicInformation();
            basicInfo.LastWriteTime = lastWriteTime;

            if ((fileAttributes & SMBFileAttributes.Hidden) > 0)
            {
                basicInfo.FileAttributes |= FileAttributes.Hidden;
            }

            if ((fileAttributes & SMBFileAttributes.ReadOnly) > 0)
            {
                basicInfo.FileAttributes |= FileAttributes.ReadOnly;
            }

            if ((fileAttributes & SMBFileAttributes.Archive) > 0)
            {
                basicInfo.FileAttributes |= FileAttributes.Archive;
            }

            return fileStore.SetFileInformation(handle, basicInfo);
        }

        public static NTStatus SetInformation2(INTFileStore fileStore, object handle, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime)
        {
            FileNetworkOpenInformation fileInfo = NTFileStoreHelper.GetNetworkOpenInformation(fileStore, handle);
            FileBasicInformation basicInfo = new FileBasicInformation();
            basicInfo.FileAttributes = fileInfo.FileAttributes;
            basicInfo.CreationTime = creationTime;
            basicInfo.LastAccessTime = lastAccessTime;
            basicInfo.LastWriteTime = lastWriteTime;
            return fileStore.SetFileInformation(handle, basicInfo);
        }

        public static SMBFileAttributes GetFileAttributes(FileAttributes attributes)
        {
            SMBFileAttributes result = SMBFileAttributes.Normal;
            if ((attributes & FileAttributes.Hidden) > 0)
            {
                result |= SMBFileAttributes.Hidden;
            }
            if ((attributes & FileAttributes.ReadOnly) > 0)
            {
                result |= SMBFileAttributes.ReadOnly;
            }
            if ((attributes & FileAttributes.Archive) > 0)
            {
                result |= SMBFileAttributes.Archive;
            }
            if ((attributes & FileAttributes.Directory) > 0)
            {
                result |= SMBFileAttributes.Directory;
            }

            return result;
        }
    }
}
