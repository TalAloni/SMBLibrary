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
    internal partial class SMB1FileStoreHelper
    {
        public static NTStatus GetFileSystemInformation(out QueryFSInformation result, INTFileStore fileStore, QueryFSInformationLevel informationLevel)
        {
            result = null;

            FileSystemInformation fsInfo;
            switch (informationLevel)
            {
                case QueryFSInformationLevel.SMB_QUERY_FS_VOLUME_INFO:
                    {
                        NTStatus status = fileStore.GetFileSystemInformation(out fsInfo, FileSystemInformationClass.FileFsVolumeInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileFsVolumeInformation volumeInfo = (FileFsVolumeInformation)fsInfo;

                        QueryFSVolumeInfo information = new QueryFSVolumeInfo();
                        information.VolumeCreationTime = volumeInfo.VolumeCreationTime;
                        information.SerialNumber = volumeInfo.VolumeSerialNumber;
                        information.VolumeLabel = volumeInfo.VolumeLabel;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_SIZE_INFO:
                    {
                        NTStatus status = fileStore.GetFileSystemInformation(out fsInfo, FileSystemInformationClass.FileFsSizeInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileFsSizeInformation fsSizeInfo = (FileFsSizeInformation)fsInfo;

                        QueryFSSizeInfo information = new QueryFSSizeInfo();
                        information.TotalAllocationUnits = fsSizeInfo.TotalAllocationUnits;
                        information.TotalFreeAllocationUnits = fsSizeInfo.AvailableAllocationUnits;
                        information.BytesPerSector = fsSizeInfo.BytesPerSector;
                        information.SectorsPerAllocationUnit = fsSizeInfo.SectorsPerAllocationUnit;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_DEVICE_INFO:
                    {
                        NTStatus status = fileStore.GetFileSystemInformation(out fsInfo, FileSystemInformationClass.FileFsDeviceInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileFsDeviceInformation fsDeviceInfo = (FileFsDeviceInformation)fsInfo;

                        QueryFSDeviceInfo information = new QueryFSDeviceInfo();
                        information.DeviceType = fsDeviceInfo.DeviceType;
                        information.DeviceCharacteristics = fsDeviceInfo.Characteristics;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryFSInformationLevel.SMB_QUERY_FS_ATTRIBUTE_INFO:
                    {
                        NTStatus status = fileStore.GetFileSystemInformation(out fsInfo, FileSystemInformationClass.FileFsAttributeInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileFsAttributeInformation fsAttributeInfo = (FileFsAttributeInformation)fsInfo;

                        QueryFSAttibuteInfo information = new QueryFSAttibuteInfo();
                        information.FileSystemAttributes = fsAttributeInfo.FileSystemAttributes;
                        information.MaxFileNameLengthInBytes = fsAttributeInfo.MaximumComponentNameLength;
                        information.FileSystemName = fsAttributeInfo.FileSystemName;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                default:
                    {
                        return NTStatus.STATUS_OS2_INVALID_LEVEL;
                    }
            }
        }
    }
}
