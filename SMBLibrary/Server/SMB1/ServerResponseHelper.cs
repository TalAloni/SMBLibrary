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
    public partial class ServerResponseHelper
    {
        internal static SMB1Command GetCloseResponse(SMB1Header header, CloseRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FID);
            if (openFile == null)
            {
                header.Status = NTStatus.STATUS_SMB_BAD_FID;
                return new ErrorResponse(CommandName.SMB_COM_CLOSE);
            }

            state.LogToServer(Severity.Verbose, "Close: Closing file '{0}'", openFile.Path);
            session.RemoveOpenFile(request.FID);
            if (openFile.DeleteOnClose && share is FileSystemShare)
            {
                try
                {
                    ((FileSystemShare)share).FileSystem.Delete(openFile.Path);
                }
                catch
                {
                    state.LogToServer(Severity.Debug, "Close: Cannot delete '{0}'", openFile.Path);
                }
            }
            CloseResponse response = new CloseResponse();
            return response;
        }

        internal static SMB1Command GetFindClose2Request(SMB1Header header, FindClose2Request request, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            session.RemoveOpenSearch(request.SearchHandle);
            return new FindClose2Response();
        }

        internal static EchoResponse GetEchoResponse(EchoRequest request, List<SMB1Command> sendQueue)
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
