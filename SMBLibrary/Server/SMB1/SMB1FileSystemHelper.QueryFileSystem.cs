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
        public static QueryFSInformation GetFileSystemInformation(QueryFSInformationLevel informationLevel, IFileSystem fileSystem)
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
                        result.DeviceCharacteristics = DeviceCharacteristics.IsMounted;
                        result.DeviceType = DeviceType.Disk;
                        return result;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_ATTRIBUTE_INFO:
                    {
                        QueryFSAttibuteInfo result = new QueryFSAttibuteInfo();
                        result.FileSystemAttributes = FileSystemAttributes.UnicodeOnDisk;
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
    }
}
