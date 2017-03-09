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
        public static NTStatus GetFileInformation(out QueryInformation result, INTFileStore fileStore, string path, QueryInformationLevel informationLevel, SecurityContext securityContext)
        {
            object handle;
            FileStatus fileStatus;
            NTStatus openStatus = fileStore.CreateFile(out handle, out fileStatus, path, FileAccessMask.FILE_READ_ATTRIBUTES, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, 0, securityContext);
            if (openStatus != NTStatus.STATUS_SUCCESS)
            {
                result = null;
                return openStatus;
            }
            NTStatus returnStatus = GetFileInformation(out result, fileStore, handle, informationLevel);
            fileStore.CloseFile(handle);
            return returnStatus;
        }

        public static NTStatus GetFileInformation(out QueryInformation result, INTFileStore fileStore, object handle, QueryInformationLevel informationLevel)
        {
            result = null;
            FileInformation fileInfo;
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
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileBasicInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileBasicInformation fileBasicInfo = (FileBasicInformation)fileInfo;

                        QueryFileBasicInfo information = new QueryFileBasicInfo();
                        information.CreationTime = fileBasicInfo.CreationTime;
                        information.LastAccessTime = fileBasicInfo.LastAccessTime;
                        information.LastWriteTime = fileBasicInfo.LastWriteTime;
                        information.LastChangeTime = fileBasicInfo.LastWriteTime;
                        information.ExtFileAttributes = (ExtendedFileAttributes)fileBasicInfo.FileAttributes;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STANDARD_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileStandardInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileStandardInformation fileStandardInfo = (FileStandardInformation)fileInfo;

                        QueryFileStandardInfo information = new QueryFileStandardInfo();
                        information.AllocationSize = fileStandardInfo.AllocationSize;
                        information.EndOfFile = fileStandardInfo.EndOfFile;
                        information.DeletePending = fileStandardInfo.DeletePending;
                        information.Directory = fileStandardInfo.Directory;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_EA_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileEaInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileEaInformation fileEAInfo = (FileEaInformation)fileInfo;

                        QueryFileExtendedAttributeInfo information = new QueryFileExtendedAttributeInfo();
                        information.EASize = fileEAInfo.EaSize;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_NAME_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileNameInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileNameInformation fileNameInfo = (FileNameInformation)fileInfo;

                        QueryFileNameInfo information = new QueryFileNameInfo();
                        information.FileName = fileNameInfo.FileName;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALL_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileAllInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileAllInformation fileAllInfo = (FileAllInformation)fileInfo;

                        QueryFileAllInfo information = new QueryFileAllInfo();
                        information.CreationDateTime = fileAllInfo.BasicInformation.CreationTime;
                        information.LastAccessDateTime = fileAllInfo.BasicInformation.LastAccessTime;
                        information.LastWriteDateTime = fileAllInfo.BasicInformation.LastWriteTime;
                        information.LastChangeTime = fileAllInfo.BasicInformation.LastWriteTime;
                        information.ExtFileAttributes = (ExtendedFileAttributes)fileAllInfo.BasicInformation.FileAttributes;
                        information.AllocationSize = fileAllInfo.StandardInformation.AllocationSize;
                        information.EndOfFile = fileAllInfo.StandardInformation.EndOfFile;
                        information.DeletePending = fileAllInfo.StandardInformation.DeletePending;
                        information.Directory = fileAllInfo.StandardInformation.Directory;
                        information.EASize = fileAllInfo.EaInformation.EaSize;
                        information.FileName = fileAllInfo.NameInformation.FileName;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_ALT_NAME_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileAlternateNameInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileAlternateNameInformation fileAltNameInfo = (FileAlternateNameInformation)fileInfo;

                        QueryFileAltNameInfo information = new QueryFileAltNameInfo();
                        information.FileName = fileAltNameInfo.FileName;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileStreamInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileStreamInformation fileStreamInfo = (FileStreamInformation)fileInfo;

                        QueryFileStreamInfo information = new QueryFileStreamInfo();
                        information.StreamSize = fileStreamInfo.StreamSize;
                        information.StreamAllocationSize = fileStreamInfo.StreamAllocationSize;
                        information.StreamName = fileStreamInfo.StreamName;
                        result = information;
                        return NTStatus.STATUS_SUCCESS;
                    }
                case QueryInformationLevel.SMB_QUERY_FILE_COMPRESSION_INFO:
                    {
                        NTStatus status = fileStore.GetFileInformation(out fileInfo, handle, FileInformationClass.FileCompressionInformation);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return status;
                        }

                        FileCompressionInformation fileCompressionInfo = (FileCompressionInformation)fileInfo;

                        QueryFileCompressionInfo information = new QueryFileCompressionInfo();
                        information.CompressedFileSize = fileCompressionInfo.CompressedFileSize;
                        information.CompressionFormat = fileCompressionInfo.CompressionFormat;
                        information.CompressionUnitShift = fileCompressionInfo.CompressionUnitShift;
                        information.ChunkShift = fileCompressionInfo.ChunkShift;
                        information.ClusterShift = fileCompressionInfo.ClusterShift;
                        information.Reserved = fileCompressionInfo.Reserved;
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
