/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.Authentication;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    public class QueryDirectoryHelper
    {
        internal static SMB2Command GetQueryDirectoryResponse(QueryDirectoryRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            if (!(share is FileSystemShare))
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
            }

            SMB2Session session = state.GetSession(request.Header.SessionID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
            if (openFile == null)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
            }

            if (!((FileSystemShare)share).HasReadAccess(session.UserName, openFile.Path, state.ClientEndPoint))
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
            }

            FileSystemShare fileSystemShare = (FileSystemShare)share;
            IFileSystem fileSystem = fileSystemShare.FileSystem;

            if (!fileSystem.GetEntry(openFile.Path).IsDirectory)
            {
                if ((request.Flags & QueryDirectoryFlags.SMB2_REOPEN) > 0)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
                }
                else
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_INVALID_PARAMETER);
                }
            }

            ulong fileID = request.FileId.Persistent;
            OpenSearch openSearch = session.GetOpenSearch(fileID);
            if (openSearch == null || request.Reopen)
            {
                if (request.Reopen)
                {
                    session.RemoveOpenSearch(fileID);
                }
                List<FileSystemEntry> entries;
                NTStatus searchStatus = NTFileSystemHelper.FindEntries(out entries, fileSystemShare.FileSystem, openFile.Path, request.FileName);
                if (searchStatus != NTStatus.STATUS_SUCCESS)
                {
                    state.LogToServer(Severity.Verbose, "Query Directory: Path: '{0}', Searched for '{1}', NTStatus: {2}", openFile.Path, request.FileName, searchStatus.ToString());
                    return new ErrorResponse(request.CommandName, searchStatus);
                }
                state.LogToServer(Severity.Verbose, "Query Directory: Path: '{0}', Searched for '{1}', found {2} matching entries", openFile.Path, request.FileName, entries.Count);
                openSearch = session.AddOpenSearch(fileID, entries, 0);
            }

            if (request.Restart || request.Reopen)
            {
                openSearch.EnumerationLocation = 0;
            }

            if (openSearch.Entries.Count == 0)
            {
                // [MS-SMB2] If there are no entries to return [..] the server MUST fail the request with STATUS_NO_SUCH_FILE.
                session.RemoveOpenSearch(fileID);
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_NO_SUCH_FILE);
            }

            if (openSearch.EnumerationLocation == openSearch.Entries.Count)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_NO_MORE_FILES);
            }

            List<QueryDirectoryFileInformation> page = new List<QueryDirectoryFileInformation>();
            int pageLength = 0;
            for (int index = openSearch.EnumerationLocation; index < openSearch.Entries.Count; index++)
            {
                QueryDirectoryFileInformation fileInformation;
                try
                {
                    fileInformation = FromFileSystemEntry(openSearch.Entries[index], request.FileInformationClass);
                }
                catch (UnsupportedInformationLevelException)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_INVALID_INFO_CLASS);
                }

                if (pageLength + fileInformation.Length <= request.OutputBufferLength)
                {
                    page.Add(fileInformation);
                    pageLength += fileInformation.Length;
                    openSearch.EnumerationLocation = index + 1;
                }
                else
                {
                    break;
                }

                if (request.ReturnSingleEntry)
                {
                    break;
                }
            }
            
            QueryDirectoryResponse response = new QueryDirectoryResponse();
            response.SetFileInformationList(page);
            return response;
        }

        internal static QueryDirectoryFileInformation FromFileSystemEntry(FileSystemEntry entry, FileInformationClass informationClass)
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
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.ShortName = NTFileSystemHelper.GetShortName(entry.Name);
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
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
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
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
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
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
                        result.EaSize = 0;
                        result.ShortName = NTFileSystemHelper.GetShortName(entry.Name);
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
                        result.EndOfFile = entry.Size;
                        result.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        result.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
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
