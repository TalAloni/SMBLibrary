/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.NetBios;
using SMBLibrary.Server.SMB2;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Server
{
    public partial class SMBServer
    {
        // Key is the persistent portion of the FileID
        private Dictionary<ulong, OpenFileObject> m_globalOpenFiles = new Dictionary<ulong, OpenFileObject>();
        private static ulong m_nextPersistentFileID = 1; // A numeric value that uniquely identifies the open handle to a file or a pipe within the scope of all opens granted by the server

        private ulong? AllocatePersistentFileID()
        {
            for (ulong offset = 0; offset < UInt64.MaxValue; offset++)
            {
                ulong persistentID = (ulong)(m_nextPersistentFileID + offset);
                if (persistentID == 0 || persistentID == 0xFFFFFFFFFFFFFFFF)
                {
                    continue;
                }
                if (!m_globalOpenFiles.ContainsKey(persistentID))
                {
                    m_nextPersistentFileID = (ulong)(persistentID + 1);
                    return persistentID;
                }
            }
            return null;
        }

        private void ProcessSMB2RequestChain(List<SMB2Command> requestChain, ref ConnectionState state)
        {
            List<SMB2Command> responseChain = new List<SMB2Command>();
            FileID? fileID = null;
            foreach (SMB2Command request in requestChain)
            {
                if (request.Header.IsRelatedOperations && fileID.HasValue)
                {
                    SetRequestFileID(request, fileID.Value);
                }
                SMB2Command response = ProcessSMB2Command(request, ref state);
                if (response != null)
                {
                    UpdateSMB2Header(response, request);
                    responseChain.Add(response);
                    if (!request.Header.IsRelatedOperations)
                    {
                        fileID = GetResponseFileID(response);
                    }
                }
            }
            if (responseChain.Count > 0)
            {
                TrySendResponseChain(state, responseChain);
            }
        }

        /// <summary>
        /// May return null
        /// </summary>
        private SMB2Command ProcessSMB2Command(SMB2Command command, ref ConnectionState state)
        {
            if (state.ServerDialect == SMBDialect.NotSet)
            {
                if (command is NegotiateRequest)
                {
                    NegotiateRequest request = (NegotiateRequest)command;
                    SMB2Command response = NegotiateHelper.GetNegotiateResponse(request, m_securityProvider, state, m_serverGuid, m_serverStartTime);
                    if (state.ServerDialect != SMBDialect.NotSet)
                    {
                        state = new SMB2ConnectionState(state, AllocatePersistentFileID);
                    }
                    return response;
                }
                else
                {
                    // [MS-SMB2] If the request being received is not an SMB2 NEGOTIATE Request [..]
                    // and Connection.NegotiateDialect is 0xFFFF or 0x02FF, the server MUST
                    // disconnect the connection.
                    state.LogToServer(Severity.Debug, "Invalid Connection State for command {0}", command.CommandName.ToString());
                    state.ClientSocket.Close();
                    return null;
                }
            }
            else if (command is NegotiateRequest)
            {
                // [MS-SMB2] If Connection.NegotiateDialect is 0x0202, 0x0210, 0x0300, 0x0302, or 0x0311,
                // the server MUST disconnect the connection.
                state.LogToServer(Severity.Debug, "Rejecting NegotiateRequest. NegotiateDialect is already set");
                state.ClientSocket.Close();
                return null;
            }
            else
            {
                return ProcessSMB2Command(command, (SMB2ConnectionState)state);
            }
        }

        private SMB2Command ProcessSMB2Command(SMB2Command command, SMB2ConnectionState state)
        {
            if (command is SessionSetupRequest)
            {
                return SessionSetupHelper.GetSessionSetupResponse((SessionSetupRequest)command, m_securityProvider, state);
            }
            else if (command is EchoRequest)
            {
                return new EchoResponse();
            }
            else
            {
                SMB2Session session = state.GetSession(command.Header.SessionID);
                if (session == null)
                {
                    return new ErrorResponse(command.CommandName, NTStatus.STATUS_USER_SESSION_DELETED);
                }

                if (command is TreeConnectRequest)
                {
                    return TreeConnectHelper.GetTreeConnectResponse((TreeConnectRequest)command, state, m_services, m_shares);
                }
                else if (command is LogoffRequest)
                {
                    m_securityProvider.DeleteSecurityContext(ref session.SecurityContext.AuthenticationContext);
                    state.RemoveSession(command.Header.SessionID);
                    return new LogoffResponse();
                }
                else
                {
                    // Cancel requests can have an ASYNC header (TreeID will not be present)
                    if (command is CancelRequest)
                    {
                        if (command.Header.IsAsync && command.Header.AsyncID == 0)
                        {
                            ErrorResponse response = new ErrorResponse(command.CommandName, NTStatus.STATUS_CANCELLED);
                            response.Header.IsAsync = true;
                            return response;
                        }

                        // [MS-SMB2] If a request is not found, the server MUST stop processing for this cancel request. No response is sent.
                        return null;
                    }

                    ISMBShare share = session.GetConnectedTree(command.Header.TreeID);
                    if (share == null)
                    {
                        return new ErrorResponse(command.CommandName, NTStatus.STATUS_NETWORK_NAME_DELETED);
                    }

                    if (command is TreeDisconnectRequest)
                    {
                        session.RemoveConnectedTree(command.Header.TreeID);
                        return new TreeDisconnectResponse();
                    }
                    else if (command is CreateRequest)
                    {
                        return CreateHelper.GetCreateResponse((CreateRequest)command, share, state);
                    }
                    else if (command is QueryInfoRequest)
                    {
                        return QueryInfoHelper.GetQueryInfoResponse((QueryInfoRequest)command, share, state);
                    }
                    else if (command is SetInfoRequest)
                    {
                        return SetInfoHelper.GetSetInfoResponse((SetInfoRequest)command, share, state);
                    }
                    else if (command is QueryDirectoryRequest)
                    {
                        return QueryDirectoryHelper.GetQueryDirectoryResponse((QueryDirectoryRequest)command, share, state);
                    }
                    else if (command is ReadRequest)
                    {
                        return ReadWriteResponseHelper.GetReadResponse((ReadRequest)command, share, state);
                    }
                    else if (command is WriteRequest)
                    {
                        return ReadWriteResponseHelper.GetWriteResponse((WriteRequest)command, share, state);
                    }
                    else if (command is FlushRequest)
                    {
                        FlushRequest request = (FlushRequest)command;
                        OpenFileObject openFile = session.GetOpenFileObject(request.FileId.Persistent);
                        if (openFile == null)
                        {
                            return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
                        }
                        NTStatus status = share.FileStore.FlushFileBuffers(openFile.Handle);
                        if (status != NTStatus.STATUS_SUCCESS)
                        {
                            return new ErrorResponse(request.CommandName, status);
                        }
                        return new FlushResponse();
                    }
                    else if (command is CloseRequest)
                    {
                        return CloseHelper.GetCloseResponse((CloseRequest)command, share, state);
                    }
                    else if (command is IOCtlRequest)
                    {
                        return IOCtlHelper.GetIOCtlResponse((IOCtlRequest)command, share, state);
                    }
                    else if (command is ChangeNotifyRequest)
                    {
                        // [MS-SMB2] If the underlying object store does not support change notifications, the server MUST fail this request with STATUS_NOT_SUPPORTED
                        ErrorResponse response = new ErrorResponse(command.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
                        // Windows 7 / 8 / 10 will infinitely retry sending ChangeNotify requests if the response does not have SMB2_FLAGS_ASYNC_COMMAND set.
                        // Note: NoRemoteChangeNotify can be set in the registry to prevent the client from sending ChangeNotify requests altogether.
                        response.Header.IsAsync = true;
                        return response;
                    }
                }
            }

            return new ErrorResponse(command.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
        }

        private static void TrySendResponse(ConnectionState state, SMB2Command response)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = response.GetBytes();
            TrySendPacket(state, packet);
            state.LogToServer(Severity.Verbose, "SMB2 response sent: {0}, Packet length: {1}", response.CommandName.ToString(), packet.Length);
        }

        private static void TrySendResponseChain(ConnectionState state, List<SMB2Command> responseChain)
        {
            byte[] sessionKey = null;
            if (state is SMB2ConnectionState)
            {
                // Note: multiple sessions MAY be multiplexed on the same connection, so theoretically
                // we could have compounding unrelated requests from different sessions.
                // In practice however this is not a real problem.
                ulong sessionID = responseChain[0].Header.SessionID;
                if (sessionID != 0)
                {
                    SMB2Session session = ((SMB2ConnectionState)state).GetSession(sessionID);
                    if (session != null)
                    {
                        sessionKey = session.SessionKey;
                    }
                }
            }

            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = SMB2Command.GetCommandChainBytes(responseChain, sessionKey);
            TrySendPacket(state, packet);
            state.LogToServer(Severity.Verbose, "SMB2 response chain sent: Response count: {0}, First response: {1}, Packet length: {2}", responseChain.Count, responseChain[0].CommandName.ToString(), packet.Length);
        }

        private static void UpdateSMB2Header(SMB2Command response, SMB2Command request)
        {
            response.Header.MessageID = request.Header.MessageID;
            response.Header.CreditCharge = request.Header.CreditCharge;
            response.Header.Credits = Math.Max((ushort)1, request.Header.Credits);
            response.Header.IsRelatedOperations = request.Header.IsRelatedOperations;
            // [MS-SMB2] The server SHOULD sign the message [..] if the request was signed by the client,
            // and the response is not an interim response to an asynchronously processed request.
            bool isInterimResponse = (request.Header.IsAsync && request.Header.Status == NTStatus.STATUS_PENDING);
            response.Header.IsSigned = request.Header.IsSigned && !isInterimResponse;
            response.Header.Reserved = request.Header.Reserved;
            if (response.Header.SessionID == 0)
            {
                response.Header.SessionID = request.Header.SessionID;
            }
            if (response.Header.TreeID == 0)
            {
                response.Header.TreeID = request.Header.TreeID;
            }
        }

        private static void SetRequestFileID(SMB2Command command, FileID fileID)
        {
            if (command is ChangeNotifyRequest)
            {
                ((ChangeNotifyRequest)command).FileId = fileID;
            }
            else if (command is CloseRequest)
            {
                ((CloseRequest)command).FileId = fileID;
            }
            else if (command is FlushRequest)
            {
                ((FlushRequest)command).FileId = fileID;
            }
            else if (command is IOCtlRequest)
            {
                ((IOCtlRequest)command).FileId = fileID;
            }
            else if (command is LockRequest)
            {
                ((LockRequest)command).FileId = fileID;
            }
            else if (command is QueryDirectoryRequest)
            {
                ((QueryDirectoryRequest)command).FileId = fileID;
            }
            else if (command is QueryInfoRequest)
            {
                ((QueryInfoRequest)command).FileId = fileID;
            }
            else if (command is ReadRequest)
            {
                ((ReadRequest)command).FileId = fileID;
            }
            else if (command is SetInfoRequest)
            {
                ((SetInfoRequest)command).FileId = fileID;
            }
            else if (command is WriteRequest)
            {
                ((WriteRequest)command).FileId = fileID;
            }
        }

        private static FileID? GetResponseFileID(SMB2Command command)
        {
            if (command is CreateResponse)
            {
                return ((CreateResponse)command).FileId;
            }
            else if (command is IOCtlResponse)
            {
                return ((IOCtlResponse)command).FileId;
            }
            return null;
        }
    }
}
