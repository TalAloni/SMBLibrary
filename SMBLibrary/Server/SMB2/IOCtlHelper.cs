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
        internal static SMB2Command GetIOCtlResponse(IOCtlRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            if (request.CtlCode == (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS ||
                request.CtlCode == (uint)IoControlCode.FSCTL_DFS_GET_REFERRALS_EX)
            {
                // [MS-SMB2] 3.3.5.15.2 Handling a DFS Referral Information Request
                return new ErrorResponse(request.CommandName, NTStatus.STATUS_FS_DRIVER_REQUIRED);
            }

            OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
            object handle;
            if (openFile == null)
            {
                if (request.CtlCode == (uint)IoControlCode.FSCTL_PIPE_WAIT)
                {
                    // [MS-SMB2] 3.2.4.20.9 - FSCTL_PIPE_WAIT request has FileId set to 0xFFFFFFFFFFFFFFFF
                    handle = null;
                }
                else
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
                }
            }
            else
            {
                handle = openFile.Handle;
            }

            int maxOutputLength = (int)request.MaxOutputResponse;
            byte[] output;
            NTStatus status = share.FileStore.DeviceIOControl(handle, request.CtlCode, request.Input, out output, maxOutputLength);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(request.CommandName, status);
            }

            IOCtlResponse response = new IOCtlResponse();
            response.CtlCode = request.CtlCode;
            response.Output = output;
            return response;
        }
    }
}
