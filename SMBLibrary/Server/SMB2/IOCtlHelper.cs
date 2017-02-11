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
    public class IOCtlHelper
    {
        private const uint FSCTL_DFS_GET_REFERRALS = 0x00060194;
        private const uint FSCTL_DFS_GET_REFERRALS_EX = 0x000601B0;
        private const uint FSCTL_PIPE_TRANSCEIVE = 0x0011C017;

        internal static SMB2Command GetIOCtlResponse(IOCtlRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            if (request.CtlCode == FSCTL_DFS_GET_REFERRALS || request.CtlCode == FSCTL_DFS_GET_REFERRALS_EX)
            {
                // [MS-SMB2] 3.3.5.15.2 Handling a DFS Referral Information Request
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FS_DRIVER_REQUIRED);
            }

            OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
            if (openFile == null)
            {
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
            }

            if (share is NamedPipeShare)
            {
                if (request.CtlCode == FSCTL_PIPE_TRANSCEIVE)
                {
                    IOCtlResponse response = new IOCtlResponse();
                    response.CtlCode = request.CtlCode;
                    int numberOfBytesWritten;
                    NTStatus writeStatus = share.FileStore.WriteFile(out numberOfBytesWritten, openFile.Handle, 0, request.Input);
                    if (writeStatus != NTStatus.STATUS_SUCCESS)
                    {
                        return new ErrorResponse(request.CommandName, writeStatus);
                    }
                    int maxCount = (int)request.MaxOutputResponse;
                    NTStatus readStatus = share.FileStore.ReadFile(out response.Output, openFile.Handle, 0, maxCount);
                    if (readStatus != NTStatus.STATUS_SUCCESS)
                    {
                        return new ErrorResponse(request.CommandName, readStatus);
                    }
                    return response;
                }
            }

            return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
        }
    }
}
