/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.SMB1
{
    public class QueryInformationHelper
    {
        /// <exception cref="SMBLibrary.UnsupportedInformationLevelException"></exception>
        public static FileInformationClass ToFileInformationClass(QueryInformationLevel informationLevel)
        {
            switch (informationLevel)
            {
                case QueryInformationLevel.SMB_QUERY_FILE_BASIC_INFO:
                    return FileInformationClass.FileBasicInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_STANDARD_INFO:
                    return FileInformationClass.FileStandardInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_EA_INFO:
                    return FileInformationClass.FileEaInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_NAME_INFO:
                    return FileInformationClass.FileNameInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_ALL_INFO:
                    return FileInformationClass.FileAllInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_ALT_NAME_INFO:
                    return FileInformationClass.FileAlternateNameInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_STREAM_INFO:
                    return FileInformationClass.FileStreamInformation;
                case QueryInformationLevel.SMB_QUERY_FILE_COMPRESSION_INFO:
                    return FileInformationClass.FileCompressionInformation;
                default:
                    throw new UnsupportedInformationLevelException();
            }
        }

        public static QueryInformation FromFileInformation(FileInformation fileInformation)
        {
            if (fileInformation is FileBasicInformation)
            {
                FileBasicInformation fileBasicInfo = (FileBasicInformation)fileInformation;
                QueryFileBasicInfo result = new QueryFileBasicInfo();
                result.CreationTime = fileBasicInfo.CreationTime;
                result.LastAccessTime = fileBasicInfo.LastAccessTime;
                result.LastWriteTime = fileBasicInfo.LastWriteTime;
                result.LastChangeTime = fileBasicInfo.ChangeTime;
                result.ExtFileAttributes = (ExtendedFileAttributes)fileBasicInfo.FileAttributes;
                return result;
            }
            else if (fileInformation is FileStandardInformation)
            {
                FileStandardInformation fileStandardInfo = (FileStandardInformation)fileInformation;
                QueryFileStandardInfo result = new QueryFileStandardInfo();
                result.AllocationSize = fileStandardInfo.AllocationSize;
                result.EndOfFile = fileStandardInfo.EndOfFile;
                result.DeletePending = fileStandardInfo.DeletePending;
                result.Directory = fileStandardInfo.Directory;
                return result;
            }
            else if (fileInformation is FileEaInformation)
            {
                FileEaInformation fileEAInfo = (FileEaInformation)fileInformation;
                QueryFileExtendedAttributeInfo result = new QueryFileExtendedAttributeInfo();
                result.EASize = fileEAInfo.EaSize;
                return result;
            }
            else if (fileInformation is FileNameInformation)
            {
                FileNameInformation fileNameInfo = (FileNameInformation)fileInformation;
                QueryFileNameInfo result = new QueryFileNameInfo();
                result.FileName = fileNameInfo.FileName;
                return result;
            }
            else if (fileInformation is FileAllInformation)
            {
                FileAllInformation fileAllInfo = (FileAllInformation)fileInformation;
                QueryFileAllInfo result = new QueryFileAllInfo();
                result.CreationTime = fileAllInfo.BasicInformation.CreationTime;
                result.LastAccessTime = fileAllInfo.BasicInformation.LastAccessTime;
                result.LastWriteTime = fileAllInfo.BasicInformation.LastWriteTime;
                result.LastChangeTime = fileAllInfo.BasicInformation.ChangeTime;
                result.ExtFileAttributes = (ExtendedFileAttributes)fileAllInfo.BasicInformation.FileAttributes;
                result.AllocationSize = fileAllInfo.StandardInformation.AllocationSize;
                result.EndOfFile = fileAllInfo.StandardInformation.EndOfFile;
                result.DeletePending = fileAllInfo.StandardInformation.DeletePending;
                result.Directory = fileAllInfo.StandardInformation.Directory;
                result.EaSize = fileAllInfo.EaInformation.EaSize;
                result.FileName = fileAllInfo.NameInformation.FileName;
                return result;
            }
            else if (fileInformation is FileAlternateNameInformation)
            {
                FileAlternateNameInformation fileAltNameInfo = (FileAlternateNameInformation)fileInformation;
                QueryFileAltNameInfo result = new QueryFileAltNameInfo();
                result.FileName = fileAltNameInfo.FileName;
                return result;
            }
            else if (fileInformation is FileStreamInformation)
            {
                FileStreamInformation fileStreamInfo = (FileStreamInformation)fileInformation;
                QueryFileStreamInfo result = new QueryFileStreamInfo();
                result.Entries.AddRange(fileStreamInfo.Entries);
                return result;
            }
            else if (fileInformation is FileCompressionInformation)
            {
                FileCompressionInformation fileCompressionInfo = (FileCompressionInformation)fileInformation;
                QueryFileCompressionInfo result = new QueryFileCompressionInfo();
                result.CompressedFileSize = fileCompressionInfo.CompressedFileSize;
                result.CompressionFormat = fileCompressionInfo.CompressionFormat;
                result.CompressionUnitShift = fileCompressionInfo.CompressionUnitShift;
                result.ChunkShift = fileCompressionInfo.ChunkShift;
                result.ClusterShift = fileCompressionInfo.ClusterShift;
                result.Reserved = fileCompressionInfo.Reserved;
                return result;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
