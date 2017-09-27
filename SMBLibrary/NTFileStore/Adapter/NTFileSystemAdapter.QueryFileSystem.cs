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
    public partial class NTFileSystemAdapter
    {
        public NTStatus GetFileSystemInformation(out FileSystemInformation result, FileSystemInformationClass informationClass)
        {
            switch (informationClass)
            {
                case FileSystemInformationClass.FileFsVolumeInformation:
                    {
                        FileFsVolumeInformation information = new FileFsVolumeInformation();
                        information.SupportsObjects = false;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsSizeInformation:
                    {
                        FileFsSizeInformation information = new FileFsSizeInformation();
                        information.TotalAllocationUnits = m_fileSystem.Size / ClusterSize;
                        information.AvailableAllocationUnits = m_fileSystem.FreeSpace / ClusterSize;
                        information.SectorsPerAllocationUnit = ClusterSize / BytesPerSector;
                        information.BytesPerSector = BytesPerSector;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsDeviceInformation:
                    {
                        FileFsDeviceInformation information = new FileFsDeviceInformation();
                        information.DeviceType = DeviceType.Disk;
                        information.Characteristics = DeviceCharacteristics.IsMounted;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsAttributeInformation:
                    {
                        FileFsAttributeInformation information = new FileFsAttributeInformation();
                        information.FileSystemAttributes = FileSystemAttributes.UnicodeOnDisk;
                        information.MaximumComponentNameLength = 255;
                        information.FileSystemName = m_fileSystem.Name;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsControlInformation:
                    {
                        FileFsControlInformation information = new FileFsControlInformation();
                        information.FileSystemControlFlags = FileSystemControlFlags.ContentIndexingDisabled;
                        information.DefaultQuotaThreshold = UInt64.MaxValue;
                        information.DefaultQuotaLimit = UInt64.MaxValue;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsFullSizeInformation:
                    {
                        FileFsFullSizeInformation information = new FileFsFullSizeInformation();
                        information.TotalAllocationUnits = m_fileSystem.Size / ClusterSize;
                        information.CallerAvailableAllocationUnits = m_fileSystem.FreeSpace / ClusterSize;
                        information.ActualAvailableAllocationUnits = m_fileSystem.FreeSpace / ClusterSize;
                        information.SectorsPerAllocationUnit = ClusterSize / BytesPerSector;
                        information.BytesPerSector = BytesPerSector;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case FileSystemInformationClass.FileFsObjectIdInformation:
                    {
                        result = null;
                        // STATUS_INVALID_PARAMETER is returned when the file system does not implement object IDs
                        // See: https://msdn.microsoft.com/en-us/library/cc232106.aspx
                        return NTStatus.STATUS_INVALID_PARAMETER;
                    }
                case FileSystemInformationClass.FileFsSectorSizeInformation:
                    {
                        FileFsSectorSizeInformation information = new FileFsSectorSizeInformation();
                        information.LogicalBytesPerSector = BytesPerSector;
                        information.PhysicalBytesPerSectorForAtomicity = BytesPerSector;
                        information.PhysicalBytesPerSectorForPerformance = BytesPerSector;
                        information.FileSystemEffectivePhysicalBytesPerSectorForAtomicity = BytesPerSector;
                        information.ByteOffsetForSectorAlignment = 0;
                        information.ByteOffsetForPartitionAlignment = 0;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                default:
                    {
                        result = null;
                        return NTStatus.STATUS_INVALID_INFO_CLASS;
                    }
            }
        }

        public NTStatus SetFileSystemInformation(FileSystemInformation information)
        {
            return NTStatus.STATUS_NOT_SUPPORTED;
        }
    }
}
