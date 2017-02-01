/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    public class CloseHelper
    {
        internal static SMB2Command GetCloseResponse(CloseRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
            if (openFile == null)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
            }
            string path = openFile.Path;
            session.RemoveOpenFile(request.FileId.Persistent);
            CloseResponse response = new CloseResponse();
            if (request.PostQueryAttributes)
            {
                if (share is NamedPipeShare)
                {
                    response.FileAttributes = FileAttributes.Temporary;
                }
                else // FileSystemShare
                {
                    IFileSystem fileSystem = ((FileSystemShare)share).FileSystem;
                    FileSystemEntry entry = fileSystem.GetEntry(path);
                    if (entry != null)
                    {
                        response.CreationTime = entry.CreationTime;
                        response.LastAccessTime = entry.LastAccessTime;
                        response.LastWriteTime = entry.LastWriteTime;
                        response.ChangeTime = entry.LastWriteTime;
                        response.AllocationSize = NTFileSystemHelper.GetAllocationSize(entry.Size);
                        response.EndofFile = entry.Size;
                        response.FileAttributes = NTFileSystemHelper.GetFileAttributes(entry);
                    }
                }
            }
            return response;
        }
    }
}
