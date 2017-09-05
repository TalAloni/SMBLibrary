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
        // Filename pattern examples:
        // '\Directory' - Get the directory entry
        // '\Directory\*' - List the directory files
        // '\Directory\s*' - List the directory files starting with s (cmd.exe will use this syntax when entering 's' and hitting tab for autocomplete)
        // '\Directory\<.inf' (Update driver will use this syntax)
        // '\Directory\exefile"*' (cmd.exe will use this syntax when entering an exe without its extension, explorer will use this opening a directory from the run menu)
        /// <param name="fileNamePattern">The filename pattern to search for. This field MAY contain wildcard characters</param>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public static NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, INTFileStore fileStore, string fileNamePattern, FileInformationClass fileInformation, SecurityContext securityContext)
        {
            int separatorIndex = fileNamePattern.LastIndexOf('\\');
            if (separatorIndex >= 0)
            {
                string path = fileNamePattern.Substring(0, separatorIndex + 1);
                string fileName = fileNamePattern.Substring(separatorIndex + 1);
                object handle;
                FileStatus fileStatus;
                DirectoryAccessMask accessMask = DirectoryAccessMask.FILE_LIST_DIRECTORY | DirectoryAccessMask.FILE_TRAVERSE | DirectoryAccessMask.SYNCHRONIZE;
                CreateOptions createOptions = CreateOptions.FILE_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT;
                NTStatus status = fileStore.CreateFile(out handle, out fileStatus, path, (AccessMask)accessMask, 0, ShareAccess.FILE_SHARE_READ | ShareAccess.FILE_SHARE_WRITE, CreateDisposition.FILE_OPEN, createOptions, securityContext);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    result = null;
                    return status;
                }
                status = fileStore.QueryDirectory(out result, handle, fileName, fileInformation);
                fileStore.CloseFile(handle);
                return status;
            }
            else
            {
                result = null;
                return NTStatus.STATUS_INVALID_PARAMETER;
            }
        }

        /// <exception cref="SMBLibrary.UnsupportedInformationLevelException"></exception>
        public static FindInformationList GetFindInformationList(List<QueryDirectoryFileInformation> entries, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys, int maxLength)
        {
            FindInformationList result = new FindInformationList();
            int pageLength = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                FindInformation infoEntry = GetFindInformation(entries[index], informationLevel, isUnicode, returnResumeKeys);
                int entryLength = infoEntry.GetLength(isUnicode);
                if (pageLength + entryLength <= maxLength)
                {
                    result.Add(infoEntry);
                    pageLength += entryLength;
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        /// <exception cref="SMBLibrary.UnsupportedInformationLevelException"></exception>
        public static FindInformation GetFindInformation(QueryDirectoryFileInformation entry, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
        {
            switch (informationLevel)
            {
                case FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO:
                    {
                        FileDirectoryInformation fileDirectoryInfo = (FileDirectoryInformation)entry;

                        FindFileDirectoryInfo result = new FindFileDirectoryInfo();
                        result.FileIndex = fileDirectoryInfo.FileIndex;
                        result.CreationTime = fileDirectoryInfo.CreationTime;
                        result.LastAccessTime = fileDirectoryInfo.LastAccessTime;
                        result.LastWriteTime = fileDirectoryInfo.LastWriteTime;
                        result.LastAttrChangeTime = fileDirectoryInfo.LastWriteTime;
                        result.EndOfFile = fileDirectoryInfo.EndOfFile;
                        result.AllocationSize = fileDirectoryInfo.AllocationSize;
                        result.ExtFileAttributes = (ExtendedFileAttributes)fileDirectoryInfo.FileAttributes;
                        result.FileName = fileDirectoryInfo.FileName;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_FULL_DIRECTORY_INFO:
                    {
                        FileFullDirectoryInformation fileFullDirectoryInfo = (FileFullDirectoryInformation)entry;

                        FindFileFullDirectoryInfo result = new FindFileFullDirectoryInfo();
                        result.FileIndex = fileFullDirectoryInfo.FileIndex;
                        result.CreationTime = fileFullDirectoryInfo.CreationTime;
                        result.LastAccessTime = fileFullDirectoryInfo.LastAccessTime;
                        result.LastWriteTime = fileFullDirectoryInfo.LastWriteTime;
                        result.LastAttrChangeTime = fileFullDirectoryInfo.LastWriteTime;
                        result.EndOfFile = fileFullDirectoryInfo.EndOfFile;
                        result.AllocationSize = fileFullDirectoryInfo.AllocationSize;
                        result.ExtFileAttributes = (ExtendedFileAttributes)fileFullDirectoryInfo.FileAttributes;
                        result.EASize = fileFullDirectoryInfo.EaSize;
                        result.FileName = fileFullDirectoryInfo.FileName;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_NAMES_INFO:
                    {
                        FileNamesInformation fileNamesInfo = (FileNamesInformation)entry;

                        FindFileNamesInfo result = new FindFileNamesInfo();
                        result.FileIndex = fileNamesInfo.FileIndex;
                        result.FileName = fileNamesInfo.FileName;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_BOTH_DIRECTORY_INFO:
                    {
                        FileBothDirectoryInformation fileBothDirectoryInfo = (FileBothDirectoryInformation)entry;

                        FindFileBothDirectoryInfo result = new FindFileBothDirectoryInfo();
                        result.FileIndex = fileBothDirectoryInfo.FileIndex;
                        result.CreationTime = fileBothDirectoryInfo.CreationTime;
                        result.LastAccessTime = fileBothDirectoryInfo.LastAccessTime;
                        result.LastWriteTime = fileBothDirectoryInfo.LastWriteTime;
                        result.LastChangeTime = fileBothDirectoryInfo.LastWriteTime;
                        result.EndOfFile = fileBothDirectoryInfo.EndOfFile;
                        result.AllocationSize = fileBothDirectoryInfo.AllocationSize;
                        result.ExtFileAttributes = (ExtendedFileAttributes)fileBothDirectoryInfo.FileAttributes;
                        result.EASize = fileBothDirectoryInfo.EaSize;
                        result.Reserved = fileBothDirectoryInfo.Reserved;
                        result.ShortName = fileBothDirectoryInfo.ShortName;
                        result.FileName = fileBothDirectoryInfo.FileName;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_ID_FULL_DIRECTORY_INFO:
                    {
                        FileIdFullDirectoryInformation fileIDFullDirectoryInfo = (FileIdFullDirectoryInformation)entry;

                        FindFileIDFullDirectoryInfo result = new FindFileIDFullDirectoryInfo();
                        result.FileIndex = fileIDFullDirectoryInfo.FileIndex;
                        result.CreationTime = fileIDFullDirectoryInfo.CreationTime;
                        result.LastAccessTime = fileIDFullDirectoryInfo.LastAccessTime;
                        result.LastWriteTime = fileIDFullDirectoryInfo.LastWriteTime;
                        result.LastAttrChangeTime = fileIDFullDirectoryInfo.LastWriteTime;
                        result.EndOfFile = fileIDFullDirectoryInfo.EndOfFile;
                        result.AllocationSize = fileIDFullDirectoryInfo.AllocationSize;
                        result.ExtFileAttributes = (ExtendedFileAttributes)fileIDFullDirectoryInfo.FileAttributes;
                        result.EASize = fileIDFullDirectoryInfo.EaSize;
                        result.Reserved = fileIDFullDirectoryInfo.Reserved;
                        result.FileID = fileIDFullDirectoryInfo.FileId;
                        result.FileName = fileIDFullDirectoryInfo.FileName;
                        return result;
                    }
                case FindInformationLevel.SMB_FIND_FILE_ID_BOTH_DIRECTORY_INFO:
                    {
                        FileIdBothDirectoryInformation fileIDBothDirectoryInfo = (FileIdBothDirectoryInformation)entry;

                        FindFileIDBothDirectoryInfo result = new FindFileIDBothDirectoryInfo();
                        result.FileIndex = fileIDBothDirectoryInfo.FileIndex;
                        result.CreationTime = fileIDBothDirectoryInfo.CreationTime;
                        result.LastAccessTime = fileIDBothDirectoryInfo.LastAccessTime;
                        result.LastWriteTime = fileIDBothDirectoryInfo.LastWriteTime;
                        result.LastChangeTime = fileIDBothDirectoryInfo.LastWriteTime;
                        result.EndOfFile = fileIDBothDirectoryInfo.EndOfFile;
                        result.AllocationSize = fileIDBothDirectoryInfo.AllocationSize;
                        result.ExtFileAttributes = (ExtendedFileAttributes)fileIDBothDirectoryInfo.FileAttributes;
                        result.EASize = fileIDBothDirectoryInfo.EaSize;
                        result.Reserved = fileIDBothDirectoryInfo.Reserved1;
                        result.ShortName = fileIDBothDirectoryInfo.ShortName;
                        result.Reserved2 = fileIDBothDirectoryInfo.Reserved2;
                        result.FileID = fileIDBothDirectoryInfo.FileId;
                        result.FileName = fileIDBothDirectoryInfo.FileName;
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
