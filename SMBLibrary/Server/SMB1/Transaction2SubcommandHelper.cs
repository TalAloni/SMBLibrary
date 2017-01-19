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
        internal static Transaction2FindFirst2Response GetSubcommandResponse(SMB1Header header, Transaction2FindFirst2Request subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            IFileSystem fileSystem = share.FileSystem;
            string fileNamePattern = subcommand.FileName;

            List<FileSystemEntry> entries;
            NTStatus searchStatus = NTFileSystemHelper.FindEntries(out entries, fileSystem, fileNamePattern);
            if (searchStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "FindFirst2: Searched for '{0}', NTStatus: {1}", fileNamePattern, searchStatus.ToString());
                header.Status = searchStatus;
                return null;
            }
            // We ignore SearchAttributes
            state.LogToServer(Severity.Verbose, "FindFirst2: Searched for '{0}', found {1} matching entries", fileNamePattern, entries.Count);

            // [MS-CIFS] If no matching entries are found, the server SHOULD fail the request with STATUS_NO_SUCH_FILE.
            if (entries.Count == 0)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return null;
            }

            bool returnResumeKeys = (subcommand.Flags & FindFlags.SMB_FIND_RETURN_RESUME_KEYS) > 0;
            int entriesToReturn = Math.Min(subcommand.SearchCount, entries.Count);
            List<FileSystemEntry> temp = entries.GetRange(0, entriesToReturn);
            int maxLength = (int)state.GetMaxDataCount(header.PID).Value;
            FindInformationList findInformationList = SMB1FileSystemHelper.GetFindInformationList(temp, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
            int returnCount = findInformationList.Count;
            Transaction2FindFirst2Response response = new Transaction2FindFirst2Response();
            response.SetFindInformationList(findInformationList, header.UnicodeFlag);
            response.EndOfSearch = (returnCount == entries.Count);
            // If [..] the search fit within a single response and SMB_FIND_CLOSE_AT_EOS is set in the Flags field,
            // or if SMB_FIND_CLOSE_AFTER_REQUEST is set in the request,
            // the server SHOULD return a SID field value of zero.
            // This indicates that the search has been closed and is no longer active on the server.
            if ((response.EndOfSearch && subcommand.CloseAtEndOfSearch) || subcommand.CloseAfterRequest)
            {
                response.SID = 0;
            }
            else
            {
                ushort? searchHandle = session.AddOpenSearch(entries, returnCount);
                if (!searchHandle.HasValue)
                {
                    header.Status = NTStatus.STATUS_OS2_NO_MORE_SIDS;
                    return null;
                }
                response.SID = searchHandle.Value;
            }
            return response;
        }

        internal static Transaction2FindNext2Response GetSubcommandResponse(SMB1Header header, Transaction2FindNext2Request subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenSearch openSearch = session.GetOpenSearch(subcommand.SID);
            if (openSearch == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            bool returnResumeKeys = (subcommand.Flags & FindFlags.SMB_FIND_RETURN_RESUME_KEYS) > 0;
            int maxLength = (int)state.GetMaxDataCount(header.PID).Value;
            int maxCount = Math.Min(openSearch.Entries.Count - openSearch.EnumerationLocation, subcommand.SearchCount);
            List<FileSystemEntry> temp = openSearch.Entries.GetRange(openSearch.EnumerationLocation, maxCount);
            FindInformationList findInformationList = SMB1FileSystemHelper.GetFindInformationList(temp, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
            int returnCount = findInformationList.Count;
            Transaction2FindNext2Response response = new Transaction2FindNext2Response();
            response.SetFindInformationList(findInformationList, header.UnicodeFlag);
            openSearch.EnumerationLocation += returnCount;
            response.EndOfSearch = (openSearch.EnumerationLocation == openSearch.Entries.Count);
            if (response.EndOfSearch)
            {
                session.RemoveOpenSearch(subcommand.SID);
            }
            return response;
        }

        internal static Transaction2QueryFSInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFSInformationRequest subcommand, FileSystemShare share)
        {
            Transaction2QueryFSInformationResponse response = new Transaction2QueryFSInformationResponse();
            QueryFSInformation queryFSInformation = SMB1FileSystemHelper.GetFileSystemInformation(subcommand.InformationLevel, share.FileSystem);
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
            QueryInformation queryInformation = SMB1FileSystemHelper.GetFileInformation(entry, false, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);

            return response;
        }

        internal static Transaction2QueryFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFileInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            IFileSystem fileSystem = share.FileSystem;
            OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            FileSystemEntry entry = fileSystem.GetEntry(openFile.Path);
            if (entry == null)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return null;
            }
            Transaction2QueryFileInformationResponse response = new Transaction2QueryFileInformationResponse();
            QueryInformation queryInformation = SMB1FileSystemHelper.GetFileInformation(entry, openFile.DeleteOnClose, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);

            return response;
        }

        internal static Transaction2SetFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2SetFileInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

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
                    if (!share.HasWriteAccess(session.UserName))
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
                        share.FileSystem.SetAttributes(openFile.Path, isHidden, isReadonly, isArchived);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return null;
                    }

                    try
                    {
                        share.FileSystem.SetDates(openFile.Path, info.CreationTime, info.LastWriteTime, info.LastAccessTime);
                    }
                    catch (IOException ex)
                    {
                        ushort errorCode = IOExceptionHelper.GetWin32ErrorCode(ex);
                        if (errorCode == (ushort)Win32Error.ERROR_SHARING_VIOLATION)
                        {
                            // Returning STATUS_SHARING_VIOLATION is undocumented but apparently valid
                            state.LogToServer(Severity.Debug, "Transaction2SetFileInformation: Sharing violation setting file dates, Path: '{0}'", openFile.Path);
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
                        if (!share.HasWriteAccess(session.UserName))
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return null;
                        }

                        if (openFile.Stream != null)
                        {
                            openFile.Stream.Close();
                        }
                        try
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Deleting file '{0}'", openFile.Path);
                            share.FileSystem.Delete(openFile.Path);
                        }
                        catch (IOException)
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Error deleting '{0}'", openFile.Path);
                            header.Status = NTStatus.STATUS_SHARING_VIOLATION;
                            return null;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            state.LogToServer(Severity.Information, "NTCreate: Error deleting '{0}', Access Denied", openFile.Path);
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
                        openFile.Stream.SetLength((long)allocationSize);
                    }
                    catch (IOException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_ALLOCATION_INFO: Cannot set allocation for '{0}'", openFile.Path);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_ALLOCATION_INFO: Cannot set allocation for '{0}'. Access Denied", openFile.Path);
                    }
                    return response;
                }
                case SetInformationLevel.SMB_SET_FILE_END_OF_FILE_INFO:
                {
                    ulong endOfFile = ((SetFileEndOfFileInfo)subcommand.SetInfo).EndOfFile;
                    try
                    {
                        openFile.Stream.SetLength((long)endOfFile);
                    }
                    catch (IOException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_END_OF_FILE_INFO: Cannot set end of file for '{0}'", openFile.Path);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        state.LogToServer(Severity.Debug, "SMB_SET_FILE_END_OF_FILE_INFO: Cannot set end of file for '{0}'. Access Denied", openFile.Path);
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
