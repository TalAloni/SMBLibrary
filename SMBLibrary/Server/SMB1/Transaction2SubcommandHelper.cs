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
            FindInformationList findInformationList;
            try
            {
                findInformationList = SMB1FileSystemHelper.GetFindInformationList(temp, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
            }
            catch (UnsupportedInformationLevelException)
            {
                header.Status = NTStatus.STATUS_OS2_INVALID_LEVEL;
                return null;
            }
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
            FindInformationList findInformationList;
            try
            {
                findInformationList = SMB1FileSystemHelper.GetFindInformationList(temp, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
            }
            catch (UnsupportedInformationLevelException)
            {
                header.Status = NTStatus.STATUS_OS2_INVALID_LEVEL;
                return null;
            }
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

        internal static Transaction2QueryFSInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFSInformationRequest subcommand, FileSystemShare share, SMB1ConnectionState state)
        {
            Transaction2QueryFSInformationResponse response = new Transaction2QueryFSInformationResponse();
            QueryFSInformation queryFSInformation;
            NTStatus queryStatus = SMB1FileSystemHelper.GetFileSystemInformation(out queryFSInformation, subcommand.InformationLevel, share.FileSystem);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileSystemInformation failed. Information level: {0}, NTStatus: {1}", subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
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
            QueryInformation queryInformation;
            NTStatus queryStatus = SMB1FileSystemHelper.GetFileInformation(out queryInformation, entry, false, subcommand.InformationLevel);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileInformation on '{0}' failed. Information level: {1}, NTStatus: {2}", path, subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
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
            QueryInformation queryInformation;
            NTStatus queryStatus = SMB1FileSystemHelper.GetFileInformation(out queryInformation, entry, openFile.DeleteOnClose, subcommand.InformationLevel);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileInformation on '{0}' failed. Information level: {1}, NTStatus: {2}", openFile.Path, subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
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

            SetInformation information;
            try
            {
                information = SetInformation.GetSetInformation(subcommand.InformationBytes, subcommand.InformationLevel);
            }
            catch(UnsupportedInformationLevelException)
            {
                header.Status = NTStatus.STATUS_OS2_INVALID_LEVEL;
                return null;
            }
            catch(Exception)
            {
                header.Status = NTStatus.STATUS_INVALID_PARAMETER;
                return null;
            }

            if (!share.HasWriteAccess(session.UserName))
            {
                header.Status = NTStatus.STATUS_ACCESS_DENIED;
                return null;
            }

            NTStatus status = SMB1FileSystemHelper.SetFileInformation(share.FileSystem, openFile, information, state);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}' failed. Information level: {1}, NTStatus: {2}", openFile.Path, information.InformationLevel, status);
                header.Status = status;
                return null;
            }
            Transaction2SetFileInformationResponse response = new Transaction2SetFileInformationResponse();
            return response;
        }
    }
}
