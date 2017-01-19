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
        public static FindInformation GetFindInformation(FileSystemEntry entry, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
        {
            switch (informationLevel)
            {
                case FindInformationLevel.SMB_INFO_STANDARD:
                    {
                        FindInfoStandard result = new FindInfoStandard(returnResumeKeys);
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.FileDataSize = (uint)Math.Min(entry.Size, UInt32.MaxValue);
                        result.AllocationSize = (uint)Math.Min(NTFileSystemHelper.GetAllocationSize(entry.Size), UInt32.MaxValue);
                        result.Attributes = GetFileAttributes(entry);
                        result.FileName = entry.Name;
                        return result;
                    }
                case FindInformationLevel.SMB_INFO_QUERY_EA_SIZE:
                    {
                        FindInfoQueryEASize result = new FindInfoQueryEASize(returnResumeKeys);
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.FileDataSize = (uint)Math.Min(entry.Size, UInt32.MaxValue);
                        result.AllocationSize = (uint)Math.Min(NTFileSystemHelper.GetAllocationSize(entry.Size), UInt32.MaxValue);
                        result.Attributes = GetFileAttributes(entry);
                        result.EASize = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST:
                    {
                        FindInfoQueryExtendedAttributesFromList result = new FindInfoQueryExtendedAttributesFromList(returnResumeKeys);
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.FileDataSize = (uint)Math.Min(entry.Size, UInt32.MaxValue);
                        result.AllocationSize = (uint)Math.Min(NTFileSystemHelper.GetAllocationSize(entry.Size), UInt32.MaxValue);
                        result.Attributes = GetFileAttributes(entry);
                        result.ExtendedAttributeList = new FullExtendedAttributeList();
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO:
                    {
                        FindFileDirectoryInfo result = new FindFileDirectoryInfo();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.LastAttrChangeTime = entry.LastWriteTime;
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        result.FileName = entry.Name;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_FULL_DIRECTORY_INFO:
                    {
                        FindFileFullDirectoryInfo result = new FindFileFullDirectoryInfo();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.LastAttrChangeTime = entry.LastWriteTime;
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        result.FileName = entry.Name;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_NAMES_INFO:
                    {
                        FindFileNamesInfo result = new FindFileNamesInfo();
                        result.FileName = entry.Name;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_BOTH_DIRECTORY_INFO:
                    {
                        FindFileBothDirectoryInfo result = new FindFileBothDirectoryInfo();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.LastChangeTime = entry.LastWriteTime;
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        result.ShortName = NTFileSystemHelper.GetShortName(entry.Name);
                        result.FileName = entry.Name;
                        return result;
                    }
                default:
                    {
                        throw new UnsupportedInformationLevelException();
                    }
            }
        }
    }
}
