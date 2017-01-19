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
        public static NTStatus GetFileSystemInformation(out QueryFSInformation result, QueryFSInformationLevel informationLevel, IFileSystem fileSystem)
        {
            switch (informationLevel)
            {
                case QueryFSInformationLevel.SMB_INFO_ALLOCATION:
                    {
                        QueryFSInfoAllocation information = new QueryFSInfoAllocation();
                        information.FileSystemID = 0;
                        information.SectorUnit = NTFileSystemHelper.ClusterSize / NTFileSystemHelper.BytesPerSector;
                        information.UnitsTotal = (uint)Math.Min(fileSystem.Size / NTFileSystemHelper.ClusterSize, UInt32.MaxValue);
                        information.UnitsAvailable = (uint)Math.Min(fileSystem.FreeSpace / NTFileSystemHelper.ClusterSize, UInt32.MaxValue);
                        information.Sector = NTFileSystemHelper.BytesPerSector;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_INFO_VOLUME:
                    {
                        QueryFSInfoVolume information = new QueryFSInfoVolume();
                        information.VolumeLabel = String.Empty;
                        information.VolumeSerialNumber = 0;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_VOLUME_INFO:
                    {
                        QueryFSVolumeInfo information = new QueryFSVolumeInfo();
                        information.VolumeCreationTime = DateTime.Now;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_SIZE_INFO:
                    {
                        QueryFSSizeInfo information = new QueryFSSizeInfo();
                        information.TotalAllocationUnits = (ulong)(fileSystem.Size / NTFileSystemHelper.ClusterSize);
                        information.TotalFreeAllocationUnits = (ulong)(fileSystem.FreeSpace / NTFileSystemHelper.ClusterSize);
                        information.BytesPerSector = NTFileSystemHelper.BytesPerSector;
                        information.SectorsPerAllocationUnit = NTFileSystemHelper.ClusterSize / NTFileSystemHelper.BytesPerSector;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_DEVICE_INFO:
                    {
                        QueryFSDeviceInfo information = new QueryFSDeviceInfo();
                        information.DeviceCharacteristics = DeviceCharacteristics.IsMounted;
                        information.DeviceType = DeviceType.Disk;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_ATTRIBUTE_INFO:
                    {
                        QueryFSAttibuteInfo information = new QueryFSAttibuteInfo();
                        information.FileSystemAttributes = FileSystemAttributes.UnicodeOnDisk;
                        information.MaxFileNameLengthInBytes = 255;
                        information.FileSystemName = fileSystem.Name;
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
    }
}
