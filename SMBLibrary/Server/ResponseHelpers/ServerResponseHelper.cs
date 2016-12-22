/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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

namespace SMBLibrary.Server
{
    public partial class ServerResponseHelper
    {
        internal static SMBCommand GetCloseResponse(SMBHeader header, CloseRequest request, StateObject state)
        {
            string openedFilePath = state.GetOpenedFilePath(request.FID);
            if (openedFilePath == null)
            {
                header.Status = NTStatus.STATUS_SMB_BAD_FID;
                return new ErrorResponse(CommandName.SMB_COM_CLOSE);
            }

            state.RemoveOpenedFile(request.FID);
            CloseResponse response = new CloseResponse();
            return response;
        }

        internal static SMBCommand GetFindClose2Request(SMBHeader header, FindClose2Request request, StateObject state)
        {
            state.ReleaseSearchHandle(request.SearchHandle);
            return new FindClose2Response();
        }

        internal static EchoResponse GetEchoResponse(EchoRequest request, List<SMBCommand> sendQueue)
        {
            EchoResponse response = new EchoResponse();
            response.SequenceNumber = 0;
            response.SMBData = request.SMBData;
            for (int index = 1; index < request.EchoCount; index++)
            {
                EchoResponse echo = new EchoResponse();
                echo.SequenceNumber = (ushort)index;
                echo.SMBData = request.SMBData;
                sendQueue.Add(echo);
            }
            return response;
        }
    }
}
