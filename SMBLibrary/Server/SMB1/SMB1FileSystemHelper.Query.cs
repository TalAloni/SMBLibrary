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
    public partial class SMB1FileSystemHelper
    {
        public static NTStatus GetFileInformation(out QueryInformation result, FileSystemEntry entry, bool deletePending, QueryInformationLevel informationLevel)
        {
            switch (informationLevel)
            {
                case QueryInformationLevel.SMB_INFO_QUERY_ALL_EAS:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case QueryInformationLevel.SMB_INFO_IS_NAME_VALID:
                    {
                        result = null;
                        return NTStatus.STATUS_NOT_IMPLEMENTED;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_BASIC_INFO:
                    {
                        QueryFileBasicInfo information = new QueryFileBasicInfo();
                        information.CreationDateTime = entry.CreationTime;
                        information.LastAccessDateTime = entry.LastAccessTime;
                        information.LastWriteDateTime = entry.LastWriteTime;
                        information.LastChangeTime = entry.LastWriteTime;
                        information.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STANDARD_INFO:
                    {
                        QueryFileStandardInfo information = new QueryFileStandardInfo();
                        information.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.DeletePending = deletePending;
                        information.Directory = entry.IsDirectory;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_EA_INFO:
                    {
                        QueryFileExtendedAttributeInfo information = new QueryFileExtendedAttributeInfo();
                        information.EASize = 0;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_NAME_INFO:
                    {
                        QueryFileNameInfo information = new QueryFileNameInfo();
                        information.FileName = entry.Name;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALL_INFO:
                    {
                        QueryFileAllInfo information = new QueryFileAllInfo();
                        information.CreationDateTime = entry.CreationTime;
                        information.LastAccessDateTime = entry.LastAccessTime;
                        information.LastWriteDateTime = entry.LastWriteTime;
                        information.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        information.LastChangeTime = entry.LastWriteTime;
                        information.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.EndOfFile = (long)entry.Size;
                        information.DeletePending = deletePending;
                        information.Directory = entry.IsDirectory;
                        information.EASize = 0;
                        information.FileName = entry.Name;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALT_NAME_INFO:
                    {
                        QueryFileAltNameInfo information = new QueryFileAltNameInfo();
                        information.FileName = NTFileSystemHelper.GetShortName(entry.Name);
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO:
                    {
                        QueryFileStreamInfo information = new QueryFileStreamInfo();
                        information.StreamSize = (long)entry.Size;
                        information.StreamAllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
                        information.StreamName = "::$DATA";
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_COMPRESSION_INFO:
                    {
                        QueryFileCompressionInfo information = new QueryFileCompressionInfo();
                        information.CompressionFormat = CompressionFormat.COMPRESSION_FORMAT_NONE;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                default:
                    {
                        result = null;
                        return NTStatus.STATUS_OS2_INVALID_LEVEL;
                    }
            }
        }

        public static SMBFileAttributes GetFileAttributes(FileSystemEntry entry)
        {
            SMBFileAttributes attributes = SMBFileAttributes.Normal;
            if (entry.IsHidden)
            {
                attributes |= SMBFileAttributes.Hidden;
            }
            if (entry.IsReadonly)
            {
                attributes |= SMBFileAttributes.ReadOnly;
            }
            if (entry.IsArchived)
            {
                attributes |= SMBFileAttributes.Archive;
            }
            if (entry.IsDirectory)
            {
                attributes |= SMBFileAttributes.Directory;
            }

            return attributes;
        }

        public static ExtendedFileAttributes GetExtendedFileAttributes(FileSystemEntry entry)
        {
            ExtendedFileAttributes attributes = 0;
            if (entry.IsHidden)
            {
                attributes |= ExtendedFileAttributes.Hidden;
            }
            if (entry.IsReadonly)
            {
                attributes |= ExtendedFileAttributes.Readonly;
            }
            if (entry.IsArchived)
            {
                attributes |= ExtendedFileAttributes.Archive;
            }
            if (entry.IsDirectory)
            {
                attributes |= ExtendedFileAttributes.Directory;
            }

            if ((uint)attributes == 0)
            {
                attributes = ExtendedFileAttributes.Normal;
            }

            return attributes;
        }
    }
}
