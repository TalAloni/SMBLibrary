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

namespace SMBLibrary.Server
{
    public partial class NTFileSystemHelper
    {
        // Filename pattern examples:
        // '\Directory' - Get the directory entry
        // '\Directory\*' - List the directory files
        // '\Directory\s*' - List the directory files starting with s (cmd.exe will use this syntax when entering 's' and hitting tab for autocomplete)
        // '\Directory\<.inf' (Update driver will use this syntax)
        // '\Directory\exefile"*' (cmd.exe will use this syntax when entering an exe without its extension, explorer will use this opening a directory from the run menu)
        /// <param name="fileNamePattern">The filename pattern to search for. This field MAY contain wildcard characters</param>
        /// <returns>null if the path does not exist</returns>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public static NTStatus FindEntries(out List<FileSystemEntry> entries, IFileSystem fileSystem, string fileNamePattern)
        {
            int separatorIndex = fileNamePattern.LastIndexOf('\\');
            if (separatorIndex >= 0)
            {
                string path = fileNamePattern.Substring(0, separatorIndex + 1);
                string expression = fileNamePattern.Substring(separatorIndex + 1);
                return FindEntries(out entries, fileSystem, path, expression);
            }
            else
            {
                entries = null;
                return NTStatus.STATUS_INVALID_PARAMETER;
            }
        }

        /// <param name="expression">Expression as described in [MS-FSA] 2.1.4.4</param>
        /// <returns>null if the path does not exist</returns>
        public static NTStatus FindEntries(out List<FileSystemEntry> entries, IFileSystem fileSystem, string path, string expression)
        {
            entries = null;
            FileSystemEntry entry = fileSystem.GetEntry(path);
            if (entry == null)
            {
                return NTStatus.STATUS_NO_SUCH_FILE;
            }

            if (expression == String.Empty)
            {
                return NTStatus.STATUS_INVALID_PARAMETER;
            }

            bool findExactName = !ContainsWildcardCharacters(expression);

            if (!findExactName)
            {
                try
                {
                    entries = fileSystem.ListEntriesInDirectory(path);
                }
                catch (Exception ex)
                {
                    NTStatus status = ToNTStatus(ex);
                    return status; ;
                }

                entries = GetFiltered(entries, expression);

                // Windows will return "." and ".." when enumerating directory files.
                // The SMB1 / SMB2 specifications mandate that when zero entries are found, the server SHOULD / MUST return STATUS_NO_SUCH_FILE.
                // For this reason, we MUST include the current directory and/or parent directory when enumerating a directory
                // in order to diffrentiate between a directory that does not exist and a directory with no entries.
                FileSystemEntry currentDirectory = fileSystem.GetEntry(path);
                currentDirectory.Name = ".";
                FileSystemEntry parentDirectory = fileSystem.GetEntry(FileSystem.GetParentDirectory(path));
                parentDirectory.Name = "..";
                entries.Insert(0, parentDirectory);
                entries.Insert(0, currentDirectory);
            }
            else
            {
                path = FileSystem.GetDirectoryPath(path);
                entry = fileSystem.GetEntry(path + expression);
                if (entry == null)
                {
                    return NTStatus.STATUS_NO_SUCH_FILE;
                }
                entries = new List<FileSystemEntry>();
                entries.Add(entry);
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
                    if (fileName.StartsWith(desiredFileNameStart, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    if (fileName.StartsWith(desiredFileNameStart + ".", StringComparison.InvariantCultureIgnoreCase) ||
                        fileName.Equals(desiredFileNameStart, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else if (expression.StartsWith("<"))
            {
                string desiredFileNameEnd = expression.Substring(1);
                if (fileName.EndsWith(desiredFileNameEnd, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            else if (String.Equals(fileName, expression, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}
