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
    public class ReadWriteResponseHelper
    {
        internal static SMB2Command GetReadResponse(ReadRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FileId);
            if (openFile == null)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
            }

            byte[] data;
            NTStatus readStatus = share.FileStore.ReadFile(out data, openFile.Handle, (long)request.Offset, (int)request.ReadLength);
            if (readStatus != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName, readStatus);
            }
            ReadResponse response = new ReadResponse();
            response.Data = data;
            return response;
        }

        internal static SMB2Command GetWriteResponse(WriteRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FileId);
            if (openFile == null)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
            }

            int numberOfBytesWritten;
            NTStatus writeStatus = share.FileStore.WriteFile(out numberOfBytesWritten, openFile.Handle, (long)request.Offset, request.Data);
            if (writeStatus != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName, writeStatus);
            }
            WriteResponse response = new WriteResponse();
            response.Count = (uint)numberOfBytesWritten;
            return response;
        }
    }
}
