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

namespace SMBLibrary.Server
{
    public partial class NTFileSystemHelper
    {
        public static NTStatus GetNamedPipeInformation(out FileInformation result, FileInformationClass informationClass)
        {
            switch (informationClass)
            {
                case FileInformationClass.FileBasicInformation:
                    {
                        FileBasicInformation information = new FileBasicInformation();
                        information.FileAttributes = FileAttributes.Temporary;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileStandardInformation:
                    {
                        FileStandardInformation information = new FileStandardInformation();
                        information.DeletePending = true;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                default:
                    result = null;
                    return NTStatus.STATUS_INVALID_INFO_CLASS;
            }
        }

        public static NTStatus GetFileInformation(out FileInformation result, FileSystemEntry entry, bool deletePending, FileInformationClass informationClass)
        {
            switch (informationClass)
            {
                case FileInformationClass.FileBasicInformation:
                    {
                        FileBasicInformation information = new FileBasicInformation();
                        information.CreationTime = entry.CreationTime;
                        information.LastAccessTime = entry.LastAccessTime;
                        information.LastWriteTime = entry.LastWriteTime;
                        information.ChangeTime = entry.LastWriteTime;
                        information.FileAttributes = GetFileAttributes(entry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileStandardInformation:
                    {
                        FileStandardInformation information = new FileStandardInformation();
                        information.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.Directory = entry.IsDirectory;
                        information.DeletePending = deletePending;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileInternalInformation:
                    {
                        FileInternalInformation information = new FileInternalInformation();
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileEaInformation:
                    {
                        FileEaInformation information = new FileEaInformation();
                        information.EaSize = 0;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FilePositionInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileFullEaInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileModeInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileAlignmentInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileAllInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileAlternateNameInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileStreamInformation:
                    {
                        // This information class is used to enumerate the data streams of a file or a directory.
                        // A buffer of FileStreamInformation data elements is returned by the server.
                        FileStreamInformation information = new FileStreamInformation();
                        information.StreamSize = (long)entry.Size;
                        information.StreamAllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.StreamName = "::$DATA";
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FilePipeInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FilePipeLocalInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FilePipeRemoteInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileCompressionInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case FileInformationClass.FileNetworkOpenInformation:
                    {
                        FileNetworkOpenInformation information = new FileNetworkOpenInformation();
                        information.CreationTime = entry.CreationTime;
                        information.LastAccessTime = entry.LastAccessTime;
                        information.LastWriteTime = entry.LastWriteTime;
                        information.ChangeTime = entry.LastWriteTime;
                        information.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.FileAttributes = GetFileAttributes(entry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileInformationClass.FileAttributeTagInformation:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                default:
                    result = null;
                    return NTStatus.STATUS_INVALID_INFO_CLASS;
            }
        }

        public static FileAttributes GetFileAttributes(FileSystemEntry entry)
        {
            FileAttributes attributes = 0;
            if (entry.IsHidden)
            {
                attributes |= FileAttributes.Hidden;
            }
            if (entry.IsReadonly)
            {
                attributes |= FileAttributes.ReadOnly;
            }
            if (entry.IsArchived)
            {
                attributes |= FileAttributes.Archive;
            }
            if (entry.IsDirectory)
            {
                attributes |= FileAttributes.Directory;
            }

            if (attributes == 0)
            {
                attributes = FileAttributes.Normal;
            }

            return attributes;
        }
    }
}
