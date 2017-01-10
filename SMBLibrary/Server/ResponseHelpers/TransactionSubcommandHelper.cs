/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.RPC;
using SMBLibrary.SMB1;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Server
{
    public class TransactionSubcommandHelper
    {
        internal static TransactionTransactNamedPipeResponse GetSubcommandResponse(SMB1Header header, TransactionTransactNamedPipeRequest subcommand, NamedPipeShare share, SMB1ConnectionState state)
        {
            string openedFilePath = state.GetOpenedFilePath(subcommand.FID);
            if (openedFilePath == null)
            {
                header.Status = NTStatus.STATUS_INVALID_HANDLE;
                return null;
            }

            TransactionTransactNamedPipeResponse response = new TransactionTransactNamedPipeResponse();
            RemoteService service = share.GetService(openedFilePath);
            if (service != null)
            {
                RPCPDU rpcRequest = RPCPDU.GetPDU(subcommand.WriteData);
                RPCPDU rpcReply = RemoteServiceHelper.GetRPCReply(rpcRequest, service);
                response.ReadData = rpcReply.GetBytes();
                return response;
            }

            // This code should not execute unless the request sequence is invalid
            header.Status = NTStatus.STATUS_INVALID_SMB;
            return null;
        }
    }
}
