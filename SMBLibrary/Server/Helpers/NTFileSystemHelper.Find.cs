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
        // Windows servers will return "." and ".." when enumerating directory files, Windows clients do not require it.
        // It seems that Ubuntu 10.04.4 and 13.10 expect at least one entry in the response (so empty directory listing cause a problem when omitting both).
        private const bool IncludeCurrentDirectoryInResults = true;
        private const bool IncludeParentDirectoryInResults = true;

        // Filename pattern examples:
        // '\Directory' - Get the directory entry
        // '\Directory\*' - List the directory files
        // '\Directory\s*' - List the directory files starting with s (cmd.exe will use this syntax when entering 's' and hitting tab for autocomplete)
        // '\Directory\<.inf' (Update driver will use this syntax)
        // '\Directory\exefile"*' (cmd.exe will use this syntax when entering an exe without its extension, explorer will use this opening a directory from the run menu)
        /// <param name="fileNamePattern">The filename pattern to search for. This field MAY contain wildcard characters</param>
        /// <returns>null if the path does not exist</returns>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        public static List<FileSystemEntry> FindEntries(IFileSystem fileSystem, string path)
        {
            bool isDirectoryEnumeration = false;
            string searchPattern = String.Empty;
            if (path.Contains("*") || path.Contains("<"))
            {
                isDirectoryEnumeration = true;
                int separatorIndex = path.LastIndexOf('\\');
                searchPattern = path.Substring(separatorIndex + 1);
                path = path.Substring(0, separatorIndex + 1);
            }
            bool exactNameWithoutExtension = searchPattern.Contains("\"");

            FileSystemEntry entry = fileSystem.GetEntry(path);
            if (entry == null)
            {
                return null;
            }

            List<FileSystemEntry> entries;
            if (isDirectoryEnumeration)
            {
                entries = fileSystem.ListEntriesInDirectory(path);

                if (searchPattern != String.Empty)
                {
                    entries = GetFiltered(entries, searchPattern);
                }

                if (!exactNameWithoutExtension)
                {
                    if (IncludeParentDirectoryInResults)
                    {
                        entries.Insert(0, fileSystem.GetEntry(FileSystem.GetParentDirectory(path)));
                        entries[0].Name = "..";
                    }
                    if (IncludeCurrentDirectoryInResults)
                    {
                        entries.Insert(0, fileSystem.GetEntry(path));
                        entries[0].Name = ".";
                    }
                }
            }
            else
            {
                entries = new List<FileSystemEntry>();
                entries.Add(entry);
            }
            return entries;
        }

        // [MS-FSA] 2.1.4.4
        // The FileName is string compared with Expression using the following wildcard rules:
        // * (asterisk) Matches zero or more characters.
        // ? (question mark) Matches a single character.
        // DOS_DOT (" quotation mark) Matches either a period or zero characters beyond the name string.
        // DOS_QM (> greater than) Matches any single character or, upon encountering a period or end of name string, advances the expression to the end of the set of contiguous DOS_QMs.
        // DOS_STAR (< less than) Matches zero or more characters until encountering and matching the final . in the name.
        private static List<FileSystemEntry> GetFiltered(List<FileSystemEntry> entries, string searchPattern)
        {
            if (searchPattern == String.Empty || searchPattern == "*")
            {
                return entries;
            }

            List<FileSystemEntry> result = new List<FileSystemEntry>();
            if (searchPattern.EndsWith("*") && searchPattern.Length > 1)
            {
                string fileNameStart = searchPattern.Substring(0, searchPattern.Length - 1);
                bool exactNameWithoutExtensionMatch = false;
                if (fileNameStart.EndsWith("\""))
                {
                    exactNameWithoutExtensionMatch = true;
                    fileNameStart = fileNameStart.Substring(0, fileNameStart.Length - 1);
                }

                foreach (FileSystemEntry entry in entries)
                {
                    if (!exactNameWithoutExtensionMatch)
                    {
                        if (entry.Name.StartsWith(fileNameStart, StringComparison.InvariantCultureIgnoreCase))
                        {
                            result.Add(entry);
                        }
                    }
                    else
                    {
                        if (entry.Name.StartsWith(fileNameStart + ".", StringComparison.InvariantCultureIgnoreCase) ||
                            entry.Name.Equals(fileNameStart, StringComparison.InvariantCultureIgnoreCase))
                        {
                            result.Add(entry);
                        }
                    }
                }
            }
            else if (searchPattern.StartsWith("<"))
            {
                string fileNameEnd = searchPattern.Substring(1);
                foreach (FileSystemEntry entry in entries)
                {
                    if (entry.Name.EndsWith(fileNameEnd, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Add(entry);
                    }
                }
            }
            return result;
        }
    }
}
