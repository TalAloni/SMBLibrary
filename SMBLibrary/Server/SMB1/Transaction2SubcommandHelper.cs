/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class Transaction2SubcommandHelper
    {
        // Windows servers will return "." and ".." when enumerating directory files, Windows clients do not require it.
        // It seems that Ubuntu 10.04.4 and 13.10 expect at least one entry in the response (so empty directory listing cause a problem when omitting both).
        public const bool IncludeCurrentDirectoryInResults = true;
        public const bool IncludeParentDirectoryInResults = true;

        internal static Transaction2FindFirst2Response GetSubcommandResponse(SMB1Header header, Transaction2FindFirst2Request subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            IFileSystem fileSystem = share.FileSystem;
            string path = subcommand.FileName;
            // '\Directory' - Get the directory info
            // '\Directory\*' - List the directory files
            // '\Directory\s*' - List the directory files starting with s (cmd.exe will use this syntax when entering 's' and hitting tab for autocomplete)
            // '\Directory\<.inf' (Update driver will use this syntax)
            // '\Directory\exefile"*' (cmd.exe will use this syntax when entering an exe without its extension, explorer will use this opening a directory from the run menu)
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
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return null;
            }

            List<FileSystemEntry> entries;
            if (isDirectoryEnumeration)
            {
                try
                {
                    entries = fileSystem.ListEntriesInDirectory(path);
                }
                catch (UnauthorizedAccessException)
                {
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return null;
                }

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

                // If no matching entries are found, the server SHOULD fail the request with STATUS_NO_SUCH_FILE.
                if (entries.Count == 0)
                {
                    header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                    return null;
                }
            }
            else
            {
                entries = new List<FileSystemEntry>();
                entries.Add(entry);
            }

            bool returnResumeKeys = (subcommand.Flags & FindFlags.SMB_FIND_RETURN_RESUME_KEYS) > 0;
            int entriesToReturn = Math.Min(subcommand.SearchCount, entries.Count);
            // We ignore SearchAttributes
            FindInformationList findInformationList = new FindInformationList();
            for (int index = 0; index < entriesToReturn; index++)
            {
                FindInformation infoEntry = InfoHelper.FromFileSystemEntry(entries[index], subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys);
                findInformationList.Add(infoEntry);
                if (findInformationList.GetLength(header.UnicodeFlag) > state.GetMaxDataCount(header.PID))
                {
                    findInformationList.RemoveAt(findInformationList.Count - 1);
                    break;
                }
            }
            int returnCount = findInformationList.Count;
            Transaction2FindFirst2Response response = new Transaction2FindFirst2Response();
            response.SetFindInformationList(findInformationList, header.UnicodeFlag);
            response.EndOfSearch = (returnCount == entries.Count) && (entries.Count <= subcommand.SearchCount);
            bool closeAtEndOfSearch = (subcommand.Flags & FindFlags.SMB_FIND_CLOSE_AT_EOS) > 0;
            bool closeAfterRequest = (subcommand.Flags & FindFlags.SMB_FIND_CLOSE_AFTER_REQUEST) > 0;
            // If [..] the search fit within a single response and SMB_FIND_CLOSE_AT_EOS is set in the Flags field,
            // or if SMB_FIND_CLOSE_AFTER_REQUEST is set in the request,
            // the server SHOULD return a SID field value of zero.
            // This indicates that the search has been closed and is no longer active on the server.
            if ((response.EndOfSearch && closeAtEndOfSearch) || closeAfterRequest)
            {
                response.SID = 0;
            }
            else
            {
                ushort? searchHandle = state.AllocateSearchHandle();
                if (!searchHandle.HasValue)
                {
                    header.Status = NTStatus.STATUS_OS2_NO_MORE_SIDS;
                    return null;
                }
                response.SID = searchHandle.Value;
                entries.RemoveRange(0, returnCount);
                state.OpenSearches.Add(response.SID, entries);
            }
            return response;
        }

        // [MS-FSA] 2.1.4.4
        // The FileName is string compared with Expression using the following wildcard rules:
        // * (asterisk) Matches zero or more characters.
        // ? (question mark) Matches a single character.
        // DOS_DOT (" quotation mark) Matches either a period or zero characters beyond the name string.
        // DOS_QM (> greater than) Matches any single character or, upon encountering a period or end of name string, advances the expression to the end of the set of contiguous DOS_QMs.
        // DOS_STAR (< less than) Matches zero or more characters until encountering and matching the final . in the name.
        internal static List<FileSystemEntry> GetFiltered(List<FileSystemEntry> entries, string searchPattern)
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

        internal static Transaction2FindNext2Response GetSubcommandResponse(SMB1Header header, Transaction2FindNext2Request subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            if (!state.OpenSearches.ContainsKey(subcommand.SID))
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            bool returnResumeKeys = (subcommand.Flags & FindFlags.SMB_FIND_RETURN_RESUME_KEYS) > 0;
            List<FileSystemEntry> entries = state.OpenSearches[subcommand.SID];
            FindInformationList findInformationList = new FindInformationList();
            for (int index = 0; index < entries.Count; index++)
            {
                FindInformation infoEntry = InfoHelper.FromFileSystemEntry(entries[index], subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys);
                findInformationList.Add(infoEntry);
                if (findInformationList.GetLength(header.UnicodeFlag) > state.GetMaxDataCount(header.PID))
                {
                    findInformationList.RemoveAt(findInformationList.Count - 1);
                    break;
                }
            }
            int returnCount = findInformationList.Count;
            Transaction2FindNext2Response response = new Transaction2FindNext2Response();
            response.SetFindInformationList(findInformationList, header.UnicodeFlag);
            entries.RemoveRange(0, returnCount);
            state.OpenSearches[subcommand.SID] = entries;
            response.EndOfSearch = (returnCount == entries.Count) && (entries.Count <= subcommand.SearchCount);
            if (response.EndOfSearch)
            {
                state.ReleaseSearchHandle(subcommand.SID);
            }
            return response;
        }

        internal static Transaction2QueryFSInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFSInformationRequest subcommand, FileSystemShare share)
        {
            Transaction2QueryFSInformationResponse response = new Transaction2QueryFSInformationResponse();
            QueryFSInformation queryFSInformation = InfoHelper.GetFSInformation(subcommand.InformationLevel, share.FileSystem);
            response.SetQueryFSInformation(queryFSInformation, header.UnicodeFlag);
            return response;
        }

        internal static Transaction2QueryPathInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryPathInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            IFileSystem fileSystem = share.FileSystem;
            string path = subcommand.FileName;
            FileSystemEntry entry = fileSystem.GetEntry(path);
            if (entry == null)
            {
                // Windows Server 2003 will return STATUS_OBJECT_NAME_NOT_FOUND
                // Returning STATUS_NO_SUCH_FILE caused an issue when executing ImageX.exe from WinPE 3.0 (32-bit)
                state.LogToServer(Severity.Debug, "Transaction2QueryPathInformation: File not found, Path: '{0}'", path);
                header.Status = NTStatus.STATUS_OBJECT_NAME_NOT_FOUND;
                return null;
            }
            Transaction2QueryPathInformationResponse response = new Transaction2QueryPathInformationResponse();
            QueryInformation queryInformation = InfoHelper.FromFileSystemEntry(entry, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);

            return response;
        }

        internal static Transaction2QueryFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFileInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            IFileSystem fileSystem = share.FileSystem;
            string openedFilePath = state.GetOpenedFilePath(subcommand.FID);
            if (openedFilePath == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            FileSystemEntry entry = fileSystem.GetEntry(openedFilePath);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return null;
            }
            Transaction2QueryFileInformationResponse response = new Transaction2QueryFileInformationResponse();
            QueryInformation queryInformation = InfoHelper.FromFileSystemEntry(entry, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);

            return response;
        }

        internal static Transaction2SetFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2SetFileInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            string openedFilePath = state.GetOpenedFilePath(subcommand.FID);
            if (openedFilePath == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            OpenedFileObject fileObject = state.GetOpenedFileObject(subcommand.FID);

            Transaction2SetFileInformationResponse response = new Transaction2SetFileInformationResponse();
            switch (subcommand.InformationLevel)
            {
                case SetInformationLevel.SMB_INFO_STANDARD:
                {
                    return response;
                }
                case SetInformationLevel.SMB_INFO_SET_EAS:
                {
                    throw new NotImplementedException();
                }
                case SetInformationLevel.SMB_SET_FILE_BASIC_INFO:
                {
                    string userName = state.GetConnectedUserName(header.UID);
                    if (!share.HasWriteAccess(userName))
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return null;
                    }

                    SetFileBasicInfo info = (SetFileBasicInfo)subcommand.SetInfo;
                    bool isHidden = (info.ExtFileAttributes & ExtendedFileAttributes.Hidden) > 0;
                    bool isReadonly = (info.ExtFileAttributes & ExtendedFileAttributes.Readonly) > 0;
                    bool isArchived = (info.ExtFileAttributes & ExtendedFileAttributes.Archive) > 0;
                    try
                    {
                        share.FileSystem.SetAttributes(openedFilePath, isHidden, isReadonly, isArchived);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return null;
                    }

                    try
                    {
                        share.FileSystem.SetDates(openedFilePath, info.CreationTime, info.LastWriteTime, info.LastAccessTime);
                    }
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                            state.LogToServer(Severity.Debug, "Transaction2SetFileInformation: Sharing violation setting file dates, Path: '{0}'", openedFilePath);
                            header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                            return null;
                        }
                        else
                        {
                            header.Status = NTStatus.STATUS_DATA_ERROR;
                            return null;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return null;
                    }
                    return response;
                }
                case SetInformationLevel.SMB_SET_FILE_DISPOSITION_INFO:
                {
                    if (((SetFileDispositionInfo)subcommand.SetInfo).DeletePending)
                    {
                        // We're supposed to delete the file on close, but it's too late to report errors at this late stage
                        string userName = state.GetConnectedUserName(header.UID);
                        if (!share.HasWriteAccess(userName))
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return null;
                        }

                        if (fileObject.Stream != null)
                        {
                            fileObject.Stream.Close();
                        }
                        try
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Deleting file '{0}'", openedFilePath);
                            share.FileSystem.Delete(openedFilePath);
                        }
                        catch (IOException)
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Error deleting '{0}'", openedFilePath);
                            header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                            return null;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Error deleting '{0}', Access Denied", openedFilePath);
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return null;
                        }
                    }
                    return response;
                }
                case SetInformationLevel.SMB_SET_FILE_ALLOCATION_INFO:
                {
                    // This subcommand is used to set the file length in bytes.
                    // Note: the input will NOT be a multiple of the cluster size / bytes per sector.
                    ulong allocationSize = ((SetFileAllocationInfo)subcommand.SetInfo).AllocationSize;
                    try
                    {
                        fileObject.Stream.SetLength((long)allocationSize);
                    }
                    catch (IOException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_ALLOCATION_INFO: Cannot set allocation for '{0}'", openedFilePath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_ALLOCATION_INFO: Cannot set allocation for '{0}'. Access Denied", openedFilePath);
                    }
                    return response;
                }
                case SetInformationLevel.SMB_SET_FILE_END_OF_FILE_INFO:
                {
                    ulong endOfFile = ((SetFileEndOfFileInfo)subcommand.SetInfo).EndOfFile;
                    try
                    {
                        fileObject.Stream.SetLength((long)endOfFile);
                    }
                    catch (IOException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_END_OF_FILE_INFO: Cannot set end of file for '{0}'", openedFilePath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_END_OF_FILE_INFO: Cannot set end of file for '{0}'. Access Denied", openedFilePath);
                    }
                    return response;
                }
                default:
                {
                    throw new InvalidRequestException();
                }
            }
        }
    }
}
