/* Copyright (C) 2014-2022 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using DiskAccessLibrary.FileSystems.Abstractions;
using Utilities;

namespace SMBLibrary.Adapters
{
    public partial class NTFileSystemAdapter
    {
        /// <param name="fileName">Expression as described in [MS-FSA] 2.1.4.4</param>
        public NTStatus QueryDirectory(out List<QueryDirectoryFileInformation> result, object handle, string fileName, FileInformationClass informationClass, SecurityContext securityContext)
        {
            result = null;
            FileHandle directoryHandle = (FileHandle)handle;
            if (!directoryHandle.IsDirectory)
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            if (fileName == String.Empty)
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            string path = directoryHandle.Path;
            bool findExactName = !ContainsWildcardCharacters(fileName);

            List<FileSystemEntry> entries;
            if (!findExactName)
            {
                try
                {
                    entries = m_fileSystem.ListEntriesInDirectory(path);
                }
                catch (UnauthorizedAccessException)
                {
                    return NTStatus.STATUS_ACCESS_DENIED;
                }

                entries = GetFiltered(entries, fileName);

                // Windows will return "." and ".." when enumerating directory files.
                // The SMB1 / SMB2 specifications mandate that when zero entries are found, the server SHOULD / MUST return STATUS_NO_SUCH_FILE.
                // For this reason, we MUST include the current directory and/or parent directory when enumerating a directory
                // in order to diffrentiate between a directory that does not exist and a directory with no entries.
                FileSystemEntry currentDirectory = m_fileSystem.GetEntry(path).Clone();
                currentDirectory.Name = ".";
                FileSystemEntry parentDirectory = m_fileSystem.GetEntry(FileSystem.GetParentDirectory(path)).Clone();
                parentDirectory.Name = "..";
                entries.Insert(0, parentDirectory);
                entries.Insert(0, currentDirectory);
            }
            else
            {
                path = FileSystem.GetDirectoryPath(path);
                FileSystemEntry entry;
                try
                {
                    entry = m_fileSystem.GetEntry(path + fileName);
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        NTStatus status = ToNTStatus(ex);
                        Log(Severity.Verbose, "QueryDirectory: Error querying '{0}'. {1}.", path, status);
                        return status;
                    }
                    else
                    {
                        throw;
                    }
                }
                entries = new List<FileSystemEntry>();
                entries.Add(entry);
            }

            try
            {
                result = FromFileSystemEntries(entries, informationClass);
            }
            catch (UnsupportedInformationLevelException)
            {
                return NTStatus.STATUS_INVALID_INFO_CLASS;
            }
            return NTStatus.STATUS_SUCCESS;
        }

        /// <param name="expression">Expression as described in [MS-FSA] 2.1.4.4</param>
        private static List<FileSystemEntry> GetFiltered(List<FileSystemEntry> entries, string expression)
        {
            if (expression == "*")
            {
                return entries;
            }

            List<FileSystemEntry> result = new List<FileSystemEntry>();
            foreach (FileSystemEntry entry in entries)
            {
                if (IsFileNameInExpression(entry.Name, expression))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        private static bool ContainsWildcardCharacters(string expression)
        {
            return (expression.Contains("?") || expression.Contains("*") || expression.Contains("\"") || expression.Contains(">") || expression.Contains("<"));
        }

        // [MS-FSA] 2.1.4.4
        // The FileName is string compared with Expression using the following wildcard rules:
        // * (asterisk) Matches zero or more characters.
        // ? (question mark) Matches a single character.
        // DOS_DOT (" quotation mark) Matches either a period or zero characters beyond the name string.
        // DOS_QM (> greater than) Matches any single character or, upon encountering a period or end of name string, advances the expression to the end of the set of contiguous DOS_QMs.
        // DOS_STAR (< less than) Matches zero or more characters until encountering and matching the final . in the name.
        private static bool IsFileNameInExpression(string fileName, string expression)
        {
            if (expression == "*")
            {
                return true;
            }
            else if (expression.EndsWith("*")) // expression.Length > 1
            {
                string desiredFileNameStart = expression.Substring(0, expression.Length - 1);
                bool findExactNameWithoutExtension = false;
                if (desiredFileNameStart.EndsWith("\""))
                {
                    findExactNameWithoutExtension = true;
                    desiredFileNameStart = desiredFileNameStart.Substring(0, desiredFileNameStart.Length - 1);
                }

                if (!findExactNameWithoutExtension)
                {
                    if (fileName.StartsWith(desiredFileNameStart, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    if (fileName.StartsWith(desiredFileNameStart + ".", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(desiredFileNameStart, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else if (expression.StartsWith("<"))
            {
                string desiredFileNameEnd = expression.Substring(1);
                if (fileName.EndsWith(desiredFileNameEnd, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (String.Equals(fileName, expression, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private static List<QueryDirectoryFileInformation> FromFileSystemEntries(List<FileSystemEntry> entries, FileInformationClass informationClass)
        {
            List<QueryDirectoryFileInformation> result = new List<QueryDirectoryFileInformation>();
            foreach (FileSystemEntry entry in entries)
            {
                QueryDirectoryFileInformation information = FromFileSystemEntry(entry, informationClass);
                result.Add(information);
            }
            return result;
        }

        private static QueryDirectoryFileInformation FromFileSystemEntry(FileSystemEntry entry, FileInformationClass informationClass)
        {
            switch (informationClass)
            {
                case FileInformationClass.FileBothDirectoryInformation:
                    {
                        FileBothDirectoryInformation result = new FileBothDirectoryInformation();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.ChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)GetAllocationSize(entry.Size);
                        result.FileAttributes = GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case FileInformationClass.FileDirectoryInformation:
                    {
                        FileDirectoryInformation result = new FileDirectoryInformation();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.ChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)GetAllocationSize(entry.Size);
                        result.FileAttributes = GetFileAttributes(entry);
                        result.FileName = entry.Name;
                        return result;
                    }
                case FileInformationClass.FileFullDirectoryInformation:
                    {
                        FileFullDirectoryInformation result = new FileFullDirectoryInformation();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.ChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)GetAllocationSize(entry.Size);
                        result.FileAttributes = GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case FileInformationClass.FileIdBothDirectoryInformation:
                    {
                        FileIdBothDirectoryInformation result = new FileIdBothDirectoryInformation();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.ChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)GetAllocationSize(entry.Size);
                        result.FileAttributes = GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.FileId = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case FileInformationClass.FileIdFullDirectoryInformation:
                    {
                        FileIdFullDirectoryInformation result = new FileIdFullDirectoryInformation();
                        result.CreationTime = entry.CreationTime;
                        result.LastAccessTime = entry.LastAccessTime;
                        result.LastWriteTime = entry.LastWriteTime;
                        result.ChangeTime = entry.LastWriteTime;
                        result.EndOfFile = (long)entry.Size;
                        result.AllocationSize = (long)GetAllocationSize(entry.Size);
                        result.FileAttributes = GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.FileId = 0;
                        result.FileName = entry.Name;
                        return result;
                    }
                case FileInformationClass.FileNamesInformation:
                    {
                        FileNamesInformation result = new FileNamesInformation();
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
