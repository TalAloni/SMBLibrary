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
    internal class Transaction2SubcommandHelper
    {
        internal static Transaction2FindFirst2Response GetSubcommandResponse(SMB1Header header, Transaction2FindFirst2Request subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            string fileNamePattern = subcommand.FileName;
            if (!fileNamePattern.StartsWith(@"\"))
            {
                fileNamePattern = @"\" + fileNamePattern;
            }

            List<QueryDirectoryFileInformation> entries;
            FileInformationClass informationClass;
            try
            {
                informationClass = GetFileInformationClass(subcommand.InformationLevel);
            }
            catch (UnsupportedInformationLevelException)
            {
                header.Status = NTStatus.STATUS_OS2_INVALID_LEVEL;
                return null;
            }

            NTStatus searchStatus = SMB1FileStoreHelper.QueryDirectory(out entries, share.FileStore, fileNamePattern, informationClass, session.SecurityContext);
            if (searchStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "FindFirst2: Searched for '{0}{1}', NTStatus: {2}", share.Name, fileNamePattern, searchStatus.ToString());
                header.Status = searchStatus;
                return null;
            }
            // We ignore SearchAttributes
            state.LogToServer(Severity.Information, "FindFirst2: Searched for '{0}{1}', found {2} matching entries", share.Name, fileNamePattern, entries.Count);

            // [MS-CIFS] If no matching entries are found, the server SHOULD fail the request with STATUS_NO_SUCH_FILE.
            if (entries.Count == 0)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_FILE;
                return null;
            }

            bool returnResumeKeys = (subcommand.Flags & FindFlags.SMB_FIND_RETURN_RESUME_KEYS) > 0;
            int entriesToReturn = Math.Min(subcommand.SearchCount, entries.Count);
            List<QueryDirectoryFileInformation> segment = entries.GetRange(0, entriesToReturn);
            int maxLength = (int)state.GetMaxDataCount(header.PID).Value;
            FindInformationList findInformationList;
            try
            {
                findInformationList = SMB1FileStoreHelper.GetFindInformationList(segment, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
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

        internal static Transaction2FindNext2Response GetSubcommandResponse(SMB1Header header, Transaction2FindNext2Request subcommand, ISMBShare share, SMB1ConnectionState state)
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
            List<QueryDirectoryFileInformation> segment = openSearch.Entries.GetRange(openSearch.EnumerationLocation, maxCount);
            FindInformationList findInformationList;
            try
            {
                findInformationList = SMB1FileStoreHelper.GetFindInformationList(segment, subcommand.InformationLevel, header.UnicodeFlag, returnResumeKeys, maxLength);
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

        internal static Transaction2QueryFSInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFSInformationRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, @"\"))
                {
                    state.LogToServer(Severity.Verbose, "QueryFileSystemInformation on '{0}' failed. User '{1}' was denied access.", share.Name, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return null;
                }
            }

            Transaction2QueryFSInformationResponse response = new Transaction2QueryFSInformationResponse();
            QueryFSInformation queryFSInformation;
            NTStatus queryStatus = SMB1FileStoreHelper.GetFileSystemInformation(out queryFSInformation, share.FileStore, subcommand.InformationLevel);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileSystemInformation on '{0}' failed. Information level: {1}, NTStatus: {2}", share.Name, subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
            state.LogToServer(Severity.Information, "GetFileSystemInformation on '{0}' succeeded. Information level: {1}", share.Name, subcommand.InformationLevel);
            response.SetQueryFSInformation(queryFSInformation, header.UnicodeFlag);
            return response;
        }

        internal static Transaction2QueryPathInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryPathInformationRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            string path = subcommand.FileName;
            if (!path.StartsWith(@"\"))
            {
                path = @"\" + path;
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, path))
                {
                    state.LogToServer(Severity.Verbose, "QueryPathInformation on '{0}{1}' failed. User '{2}' was denied access.", share.Name, path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return null;
                }
            }

            Transaction2QueryPathInformationResponse response = new Transaction2QueryPathInformationResponse();
            QueryInformation queryInformation;
            NTStatus queryStatus = SMB1FileStoreHelper.GetFileInformation(out queryInformation, share.FileStore, path, subcommand.InformationLevel, session.SecurityContext);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileInformation on '{0}{1}' failed. Information level: {2}, NTStatus: {3}", share.Name, path, subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
            state.LogToServer(Severity.Information, "GetFileInformation on '{0}{1}' succeeded. Information level: {2}", share.Name, path, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);
            return response;
        }

        internal static Transaction2QueryFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2QueryFileInformationRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "QueryFileInformation on '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return null;
                }
            }

            Transaction2QueryFileInformationResponse response = new Transaction2QueryFileInformationResponse();
            QueryInformation queryInformation;
            NTStatus queryStatus = SMB1FileStoreHelper.GetFileInformation(out queryInformation, share.FileStore, openFile.Handle, subcommand.InformationLevel);
            if (queryStatus != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "GetFileInformation on '{0}{1}' failed. Information level: {2}, NTStatus: {3}", share.Name, openFile.Path, subcommand.InformationLevel, queryStatus);
                header.Status = queryStatus;
                return null;
            }
            state.LogToServer(Severity.Information, "GetFileInformation on '{0}{1}' succeeded. Information level: {2}", share.Name, openFile.Path, subcommand.InformationLevel);
            response.SetQueryInformation(queryInformation);
            return response;
        }

        internal static Transaction2SetFileInformationResponse GetSubcommandResponse(SMB1Header header, Transaction2SetFileInformationRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            if (share is FileSystemShare)
            {
                if (!((FileSystemShare)share).HasWriteAccess(session.SecurityContext, openFile.Path))
                {
                    state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return null;
                }
            }

            SetInformation information;
            try
            {
                information = SetInformation.GetSetInformation(subcommand.InformationBytes, subcommand.InformationLevel);
            }
            catch(UnsupportedInformationLevelException)
            {
                state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information level: {2}, NTStatus: STATUS_OS2_INVALID_LEVEL", share.Name, openFile.Path, subcommand.InformationLevel);
                header.Status = NTStatus.STATUS_OS2_INVALID_LEVEL;
                return null;
            }
            catch(Exception)
            {
                state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information level: {2}, NTStatus: STATUS_INVALID_PARAMETER", share.Name, openFile.Path, subcommand.InformationLevel);
                header.Status = NTStatus.STATUS_INVALID_PARAMETER;
                return null;
            }

            NTStatus status = SMB1FileStoreHelper.SetFileInformation(share.FileStore, openFile.Handle, information);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                state.LogToServer(Severity.Verbose, "SetFileInformation on '{0}{1}' failed. Information level: {2}, NTStatus: {3}", share.Name, openFile.Path, subcommand.InformationLevel, status);
                header.Status = status;
                return null;
            }
            state.LogToServer(Severity.Information, "SetFileInformation on '{0}{1}' succeeded. Information level: {2}", share.Name, openFile.Path, subcommand.InformationLevel);
            Transaction2SetFileInformationResponse response = new Transaction2SetFileInformationResponse();
            return response;
        }

        private static FileInformationClass GetFileInformationClass(FindInformationLevel informationLevel)
        {
            switch (informationLevel)
            {
                case FindInformationLevel.SMB_INFO_STANDARD:
                    return FileInformationClass.FileDirectoryInformation;
                case FindInformationLevel.SMB_INFO_QUERY_EA_SIZE:
                    return FileInformationClass.FileFullDirectoryInformation;
                case FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST:
                    return FileInformationClass.FileDirectoryInformation;
                case FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO:
                    return FileInformationClass.FileDirectoryInformation;
                case FindInformationLevel.SMB_FIND_FILE_FULL_DIRECTORY_INFO:
                    return FileInformationClass.FileFullDirectoryInformation;
                case FindInformationLevel.SMB_FIND_FILE_NAMES_INFO:
                    return FileInformationClass.FileNamesInformation;
                case FindInformationLevel.SMB_FIND_FILE_BOTH_DIRECTORY_INFO:
                    return FileInformationClass.FileBothDirectoryInformation;
                default:
                    throw new UnsupportedInformationLevelException();
            }
        }
    }
}
