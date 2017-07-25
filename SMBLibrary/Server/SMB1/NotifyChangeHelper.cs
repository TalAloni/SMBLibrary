/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    internal class NotifyChangeHelper
    {
        internal static void ProcessNTTransactNotifyChangeRequest(SMB1Header header, uint maxParameterCount, NTTransactNotifyChangeRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
            SMB1AsyncContext context = state.CreateAsyncContext(header.UID, header.TID, header.PID, header.MID, subcommand.FID, state);
            // We wish to make sure that the 'Monitoring started' will appear before the 'Monitoring completed' in the log
            lock (context)
            {
                header.Status = share.FileStore.NotifyChange(out context.IORequest, openFile.Handle, subcommand.CompletionFilter, subcommand.WatchTree, (int)maxParameterCount, OnNotifyChangeCompleted, context);
                if (header.Status == NTStatus.STATUS_PENDING)
                {
                    state.LogToServer(Severity.Verbose, "NotifyChange: Monitoring of '{0}{1}' started. PID: {2}. MID: {3}.", share.Name, openFile.Path, context.PID, context.MID);
                }
                else if (header.Status == NTStatus.STATUS_NOT_SUPPORTED)
                {
                    // [MS-CIFS] If the server does not support the NT_TRANSACT_NOTIFY_CHANGE subcommand, it can return an
                    // error response with STATUS_NOT_IMPLEMENTED [..] in response to an NT_TRANSACT_NOTIFY_CHANGE Request.
                    header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
                }
            }
        }

        private static void OnNotifyChangeCompleted(NTStatus status, byte[] buffer, object context)
        {
            NTTransactNotifyChangeResponse notifyChangeResponse = new NTTransactNotifyChangeResponse();
            SMB1AsyncContext asyncContext = (SMB1AsyncContext)context;
            // Wait until the 'Monitoring started' will be written to the log
            lock (asyncContext)
            {
                SMB1ConnectionState connection = asyncContext.Connection;
                connection.RemoveAsyncContext(asyncContext);
                SMB1Session session = connection.GetSession(asyncContext.UID);
                if (session != null)
                {
                    ISMBShare share = session.GetConnectedTree(asyncContext.TID);
                    OpenFileObject openFile = session.GetOpenFileObject(asyncContext.FileID);
                    if (share != null && openFile != null)
                    {
                        connection.LogToServer(Severity.Verbose, "NotifyChange: Monitoring of '{0}{1}' completed. NTStatus: {2}. PID: {3}. MID: {4}.", share.Name, openFile.Path, status, asyncContext.PID, asyncContext.MID);
                    }
                }
                SMB1Header header = new SMB1Header();
                header.Command = CommandName.SMB_COM_NT_TRANSACT;
                header.Status = status;
                header.Flags = HeaderFlags.CaseInsensitive | HeaderFlags.CanonicalizedPaths | HeaderFlags.Reply;
                // [MS-CIFS] SMB_FLAGS2_UNICODE SHOULD be set to 1 when the negotiated dialect is NT LANMAN.
                // [MS-CIFS] The Windows NT Server implementation of NT_TRANSACT_NOTIFY_CHANGE always returns the names of changed files in Unicode format.
                header.Flags2 = HeaderFlags2.Unicode | HeaderFlags2.NTStatusCode;
                header.UID = asyncContext.UID;
                header.TID = asyncContext.TID;
                header.PID = asyncContext.PID;
                header.MID = asyncContext.MID;
                notifyChangeResponse.FileNotifyInformationBytes = buffer;

                byte[] responseSetup = notifyChangeResponse.GetSetup();
                byte[] responseParameters = notifyChangeResponse.GetParameters(false);
                byte[] responseData = notifyChangeResponse.GetData();
                List<SMB1Command> responseList = NTTransactHelper.GetNTTransactResponse(responseSetup, responseParameters, responseData, asyncContext.Connection.MaxBufferSize);
                foreach (SMB1Command response in responseList)
                {
                    SMB1Message reply = new SMB1Message();
                    reply.Header = header;
                    reply.Commands.Add(response);
                    SMBServer.EnqueueMessage(asyncContext.Connection, reply);
                }
            }
        }
    }
}
