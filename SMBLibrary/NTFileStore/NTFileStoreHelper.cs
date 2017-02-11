/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace SMBLibrary
{
    public partial class NTFileStoreHelper
    {
        public static FileAccess ToCreateFileAccess(AccessMask desiredAccess, CreateDisposition createDisposition)
        {
            FileAccess result = 0;

            if ((desiredAccess.File & FileAccessMask.FILE_READ_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_READ_EA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_READ_ATTRIBUTES) > 0 ||
                (desiredAccess.File & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_READ) > 0)
            {
                result |= FileAccess.Read;
            }

            if ((desiredAccess.File & FileAccessMask.FILE_WRITE_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_APPEND_DATA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_WRITE_EA) > 0 ||
                (desiredAccess.File & FileAccessMask.FILE_WRITE_ATTRIBUTES) > 0 ||
                (desiredAccess.File & FileAccessMask.DELETE) > 0 ||
                (desiredAccess.File & FileAccessMask.WRITE_DAC) > 0 ||
                (desiredAccess.File & FileAccessMask.WRITE_OWNER) > 0 ||
                (desiredAccess.File & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess.File & FileAccessMask.GENERIC_WRITE) > 0)
            {
                result |= FileAccess.Write;
            }

            if ((desiredAccess.Directory & DirectoryAccessMask.FILE_DELETE_CHILD) > 0)
            {
                result |= FileAccess.Write;
            }

            if (createDisposition == CreateDisposition.FILE_CREATE ||
                createDisposition == CreateDisposition.FILE_SUPERSEDE)
            {
                result |= FileAccess.Write;
            }

            return result;
        }

        public static FileAccess ToFileAccess(FileAccessMask desiredAccess)
        {
            FileAccess result = 0;
            if ((desiredAccess & FileAccessMask.FILE_READ_DATA) > 0 ||
                (desiredAccess & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_READ) > 0)
            {
                result |= FileAccess.Read;
            }

            if ((desiredAccess & FileAccessMask.FILE_WRITE_DATA) > 0 ||
                (desiredAccess & FileAccessMask.FILE_APPEND_DATA) > 0 ||
                (desiredAccess & FileAccessMask.MAXIMUM_ALLOWED) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_ALL) > 0 ||
                (desiredAccess & FileAccessMask.GENERIC_WRITE) > 0)
            {
                result |= FileAccess.Write;
            }

            return result;
        }

        public static FileShare ToFileShare(ShareAccess shareAccess)
        {
            FileShare result = FileShare.None;
            if ((shareAccess & ShareAccess.FILE_SHARE_READ) > 0)
            {
                result |= FileShare.Read;
            }

            if ((shareAccess & ShareAccess.FILE_SHARE_WRITE) > 0)
            {
                result |= FileShare.Write;
            }

            if ((shareAccess & ShareAccess.FILE_SHARE_DELETE) > 0)
            {
                result |= FileShare.Delete;
            }

            return result;
        }

        public static FileNetworkOpenInformation GetNetworkOpenInformation(INTFileStore fileStore, string path, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, FileAccessMask.FILE_READ_ATTRIBUTES, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, 0, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                return null;
            }
            FileInformation fileInfo;
            NTStatus queryStatus = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileNetworkOpenInformation);
            fileStore.CloseFile(handle);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                return null;
            }
            return (FileNetworkOpenInformation)fileInfo;
        }

        public static FileNetworkOpenInformation GetNetworkOpenInformation(INTFileStore fileStore, object handle)
        {
            FileInformation fileInfo;
            NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileNetworkOpenInformation);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return null;
            }

            return (FileNetworkOpenInformation)fileInfo;
        }
    }
}
