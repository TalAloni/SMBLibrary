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
    public class InfoHelper
    {
        internal static FindInformation FromFileSystemEntry(FileSystemEntry entry, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
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

        internal static QueryInformation FromFileSystemEntry(FileSystemEntry entry, bool deletePending, QueryInformationLevel informationLevel)
        {
            switch (informationLevel)
            {
                case QueryInformationLevel.SMB_INFO_STANDARD:
                    {
                        QueryInfoStandard result = new QueryInfoStandard();
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.FileDataSize = (uint)Math.Min(entry.Size, UInt32.MaxValue);
                        result.AllocationSize = (uint)Math.Min(NTFileSystemHelper.GetAllocationSize(entry.Size), UInt32.MaxValue);
                        return result;
                    }
                case QueryInformationLevel.SMB_INFO_QUERY_EA_SIZE:
                    {
                        QueryEASize result = new QueryEASize();
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.FileDataSize = (uint)Math.Min(entry.Size, UInt32.MaxValue);
                        result.AllocationSize = (uint)Math.Min(NTFileSystemHelper.GetAllocationSize(entry.Size), UInt32.MaxValue);
                        result.Attributes = GetFileAttributes(entry);
                        result.EASize = 0;
                        return result;
                    }
                case QueryInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST:
                    {
                        throw new NotImplementedException();
                    }
                case QueryInformationLevel.SMB_INFO_QUERY_ALL_EAS:
                    {
                        throw new NotImplementedException();
                    }
                case QueryInformationLevel.SMB_INFO_IS_NAME_VALID:
                    {
                        throw new NotImplementedException();
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_BASIC_INFO:
                    {
                        QueryFileBasicInfo result = new QueryFileBasicInfo();
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.LastChangeTime = entry.LastWriteTime;
                        result.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STANDARD_INFO:
                    {
                        QueryFileStandardInfo result = new QueryFileStandardInfo();
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.EndOfFile = entry.Size;
                        result.DeletePending = deletePending;
                        result.Directory = entry.IsDirectory;
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_EA_INFO:
                    {
                        QueryFileExtendedAttributeInfo result = new QueryFileExtendedAttributeInfo();
                        result.EASize = 0;
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_NAME_INFO:
                    {
                        QueryFileNameInfo result = new QueryFileNameInfo();
                        result.FileName = entry.Name;
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALL_INFO:
                    {
                        QueryFileAllInfo result = new QueryFileAllInfo();
                        result.CreationDateTime = entry.CreationTime;
                        result.LastAccessDateTime = entry.LastAccessTime;
                        result.LastWriteDateTime = entry.LastWriteTime;
                        result.ExtFileAttributes = GetExtendedFileAttributes(entry);
                        result.LastChangeTime = entry.LastWriteTime;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.EndOfFile = entry.Size;
                        result.DeletePending = deletePending;
                        result.Directory = entry.IsDirectory;
                        result.EASize = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALT_NAME_INFO:
                    {
                        QueryFileAltNameInfo result = new QueryFileAltNameInfo();
                        result.FileName = NTFileSystemHelper.GetShortName(entry.Name);
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO:
                    {
                        QueryFileStreamInfo result = new QueryFileStreamInfo();
                        result.StreamSize = entry.Size;
                        result.StreamAllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.StreamName = "::$DATA";
                        return result;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_COMPRESSION_INFO:
                    {
                        QueryFileCompressionInfo result = new QueryFileCompressionInfo();
                        result.CompressionFormat = CompressionFormat.COMPRESSION_FORMAT_NONE;
                        return result;
                    }
                default:
                    {
                        throw new UnsupportedInformationLevelException();
                    }
            }
        }

        internal static QueryFSInformation GetFSInformation(QueryFSInformationLevel informationLevel, IFileSystem fileSystem)
        {
            switch (informationLevel)
            {
                case QueryFSInformationLevel.SMB_INFO_ALLOCATION:
                    {
                        QueryFSInfoAllocation result = new QueryFSInfoAllocation();
                        result.FileSystemID = 0;
                        result.SectorUnit = NTFileSystemHelper.ClusterSize / NTFileSystemHelper.BytesPerSector;
                        result.UnitsTotal = (uint)Math.Min(fileSystem.Size / NTFileSystemHelper.ClusterSize, UInt32.MaxValue);
                        result.UnitsAvailable = (uint)Math.Min(fileSystem.FreeSpace / NTFileSystemHelper.ClusterSize, UInt32.MaxValue);
                        result.Sector = NTFileSystemHelper.BytesPerSector;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_INFO_VOLUME:
                    {
                        QueryFSInfoVolume result = new QueryFSInfoVolume();
                        result.VolumeLabel = String.Empty;
                        result.VolumeSerialNumber = 0;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_VOLUME_INFO:
                    {
                        QueryFSVolumeInfo result = new QueryFSVolumeInfo();
                        result.VolumeCreationTime = DateTime.Now;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_SIZE_INFO:
                    {
                        QueryFSSizeInfo result = new QueryFSSizeInfo();
                        result.TotalAllocationUnits = (ulong)(fileSystem.Size / NTFileSystemHelper.ClusterSize);
                        result.TotalFreeAllocationUnits = (ulong)(fileSystem.FreeSpace / NTFileSystemHelper.ClusterSize);
                        result.BytesPerSector = NTFileSystemHelper.BytesPerSector;
                        result.SectorsPerAllocationUnit = NTFileSystemHelper.ClusterSize / NTFileSystemHelper.BytesPerSector;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_DEVICE_INFO:
                    {
                        QueryFSDeviceInfo result = new QueryFSDeviceInfo();
                        result.DeviceCharacteristics = DeviceCharacteristics.FILE_DEVICE_IS_MOUNTED;
                        result.DeviceType = DeviceType.FILE_DEVICE_DISK;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_ATTRIBUTE_INFO:
                    {
                        QueryFSAttibuteInfo result = new QueryFSAttibuteInfo();
                        result.FileSystemAttributes = FileSystemAttributes.FILE_UNICODE_ON_DISK;
                        result.MaxFileNameLengthInBytes = 255;
                        result.FileSystemName = fileSystem.Name;
                        return result;
                    }
                default:
                    {
                        throw new UnsupportedInformationLevelException();
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
