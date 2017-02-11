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
        // Filename pattern examples:
        // '\Directory' - Get the directory entry
        // '\Directory\*' - List the directory files
        // '\Directory\s*' - List the directory files starting with s (cmd.exe will use this syntax when entering 's' and hitting tab for autocomplete)
        // '\Directory\<.inf' (Update driver will use this syntax)
        // '\Directory\exefile"*' (cmd.exe will use this syntax when entering an exe without its extension, explorer will use this opening a directory from the run menu)
        /// <param name="fileNamePattern">The filename pattern to search for. This field MAY contain wildcard characters</param>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public static NTStatus FindEntries(out List<FileSystemEntry> entries, IFileSystem fileSystem, string fileNamePattern)
        {
            int separatorIndex = fileNamePattern.LastIndexOf('\\');
            if (separatorIndex >= 0)
            {
                string path = fileNamePattern.Substring(0, separatorIndex + 1);
                string fileName = fileNamePattern.Substring(separatorIndex + 1);
                return NTFileSystemHelper.FindEntries(out entries, fileSystem, path, fileName);
            }
            else
            {
                entries = null;
                return NTStatus.STATUS_INVALID_PARAMETER;
            }
        }

        /// <exception cref="SMBLibrary.UnsupportedInformationLevelException"></exception>
        public static FindInformationList GetFindInformationList(List<FileSystemEntry> entries, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys, int maxLength)
        {
            FindInformationList result = new FindInformationList();
            for (int index = 0; index < entries.Count; index++)
            {
                FindInformation infoEntry = GetFindInformation(entries[index], informationLevel, isUnicode, returnResumeKeys);
                result.Add(infoEntry);
                if (result.GetLength(isUnicode) > maxLength)
                {
                    result.RemoveAt(result.Count - 1);
                    break;
                }
            }
            return result;
        }

        /// <exception cref="SMBLibrary.UnsupportedInformationLevelException"></exception>
        public static FindInformation GetFindInformation(FileSystemEntry entry, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
        {
            switch (informationLevel)
            {
                case FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO:
                    {
                        FindFileDirectoryInfo result = new FindFileDirectoryInfo();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.LastAttrChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
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
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
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
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)NTFileSystemHelper.GetAllocationSize(entry.Size);
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
