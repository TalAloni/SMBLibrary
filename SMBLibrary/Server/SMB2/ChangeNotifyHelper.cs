/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    internal class ChangeNotifyHelper
    {
        internal static SMB2Command GetChangeNotifyInterimResponse(ChangeNotifyRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            OpenFileObject openFile = session.GetOpenFileObject(request.FileId);
            bool watchTree = (request.Flags & ChangeNotifyFlags.WatchTree) > 0;
            SMB2AsyncContext context = state.CreateAsyncContext(request.FileId, state, request.Header.SessionID, request.Header.TreeID);
            // We have to make sure that we don't send an interim response after the final response.
            lock (context)
            {
                NTStatus status = share.FileStore.NotifyChange(out context.IORequest, openFile.Handle, request.CompletionFilter, watchTree, (int)request.OutputBufferLength, OnNotifyChangeCompleted, context);
                if (status == NTStatus.STATUS_PENDING)
                {
                    state.LogToServer(Severity.Verbose, "NotifyChange: Monitoring of '{0}{1}' started. AsyncID: {2}.", share.Name, openFile.Path, context.AsyncID);
                }
                // [MS-SMB2] If the underlying object store does not support change notifications, the server MUST fail this request with STATUS_NOT_SUPPORTED
                ErrorResponse response = new ErrorResponse(request.CommandName, status);
                // Windows 7 / 8 / 10 will infinitely retry sending ChangeNotify requests if the response does not have SMB2_FLAGS_ASYNC_COMMAND set.
                // Note: NoRemoteChangeNotify can be set in the registry to prevent the client from sending ChangeNotify requests altogether.
                response.Header.IsAsync = true;
                response.Header.AsyncID = context.AsyncID;
                return response;
            }
        }

        private static void OnNotifyChangeCompleted(NTStatus status, byte[] buffer, object context)
        {
            SMB2AsyncContext asyncContext = (SMB2AsyncContext)context;
            // Wait until the interim response has been sent
            lock (asyncContext)
            {
                SMB2ConnectionState connection = asyncContext.Connection;
                connection.RemoveAsyncContext(asyncContext);
                SMB2Session session = connection.GetSession(asyncContext.SessionID);
                if (session != null)
                {
                    OpenFileObject openFile = session.GetOpenFileObject(asyncContext.FileID);
                    if (openFile != null)
                    {
                        connection.LogToServer(Severity.Verbose, "NotifyChange: Monitoring of '{0}{1}' completed. NTStatus: {2}. AsyncID: {3}", openFile.ShareName, openFile.Path, status, asyncContext.AsyncID);
                    }

                    if (status == NTStatus.STATUS_SUCCESS ||
                        status == NTStatus.STATUS_NOTIFY_CLEANUP ||
                        status == NTStatus.STATUS_NOTIFY_ENUM_DIR)
                    {
                        ChangeNotifyResponse response = new ChangeNotifyResponse();
                        response.Header.Status = status;
                        response.Header.IsAsync = true;
                        response.Header.IsSigned = session.SigningRequired;
                        response.Header.AsyncID = asyncContext.AsyncID;
                        response.Header.SessionID = asyncContext.SessionID;
                        response.OutputBuffer = buffer;

                        SMBServer.EnqueueResponse(connection, response);
                    }
                    else
                    {
                        // [MS-SMB2] If the object store returns an error, the server MUST fail the request with the error code received.
                        ErrorResponse response = new ErrorResponse(SMB2CommandName.ChangeNotify);
                        response.Header.Status = status;
                        response.Header.IsAsync = true;
                        response.Header.IsSigned = session.SigningRequired;
                        response.Header.AsyncID = asyncContext.AsyncID;

                        SMBServer.EnqueueResponse(connection, response);
                    }
                }
            }
        }
    }
}
