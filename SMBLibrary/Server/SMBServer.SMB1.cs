/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.NetBios;
using SMBLibrary.Server.SMB1;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server
{
    public partial class SMBServer
    {
        public void ProcessSMB1Message(SMB1Message message, SMB1ConnectionState state)
        {
            SMB1Message reply = new SMB1Message();
            PrepareResponseHeader(reply, message);
            List<SMB1Command> sendQueue = new List<SMB1Command>();

            foreach (SMB1Command command in message.Commands)
            {
                SMB1Command response = ProcessSMB1Command(reply.Header, command, state, sendQueue);
                if (response != null)
                {
                    reply.Commands.Add(response);
                }
                if (reply.Header.Status != NTStatus.STATUS_SUCCESS)
                {
                    break;
                }
            }

            if (reply.Commands.Count > 0)
            {
                TrySendMessage(state, reply);

                foreach (SMB1Command command in sendQueue)
                {
                    SMB1Message secondaryReply = new SMB1Message();
                    secondaryReply.Header = reply.Header;
                    secondaryReply.Commands.Add(command);
                    TrySendMessage(state, secondaryReply);
                }
            }
        }

        /// <summary>
        /// May return null
        /// </summary>
        public SMB1Command ProcessSMB1Command(SMB1Header header, SMB1Command command, SMB1ConnectionState state, List<SMB1Command> sendQueue)
        {
            if (command is NegotiateRequest)
            {
                NegotiateRequest request = (NegotiateRequest)command;
                if (request.Dialects.Contains(SMBServer.NTLanManagerDialect))
                {
                    if (EnableExtendedSecurity && header.ExtendedSecurityFlag)
                    {
                        return NegotiateHelper.GetNegotiateResponseExtended(request, m_serverGuid);
                    }
                    else
                    {
                        byte[] serverChallenge = m_users.GenerateServerChallenge();
                        return NegotiateHelper.GetNegotiateResponse(header, request, serverChallenge);
                    }
                }
                else
                {
                    return new NegotiateResponseNotSupported();
                }
            }
            else if (command is SessionSetupAndXRequest)
            {
                SessionSetupAndXRequest request = (SessionSetupAndXRequest)command;
                state.MaxBufferSize = request.MaxBufferSize;
                return NegotiateHelper.GetSessionSetupResponse(header, request, m_users, state);
            }
            else if (command is SessionSetupAndXRequestExtended)
            {
                SessionSetupAndXRequestExtended request = (SessionSetupAndXRequestExtended)command;
                state.MaxBufferSize = request.MaxBufferSize;
                return NegotiateHelper.GetSessionSetupResponseExtended(header, request, m_users, state);
            }
            else if (command is EchoRequest)
            {
                return ServerResponseHelper.GetEchoResponse((EchoRequest)command, sendQueue);
            }
            else if (state.IsAuthenticated(header.UID))
            {
                if (command is TreeConnectAndXRequest)
                {
                    TreeConnectAndXRequest request = (TreeConnectAndXRequest)command;
                    return TreeConnectHelper.GetTreeConnectResponse(header, request, state, m_shares);
                }
                else if (command is LogoffAndXRequest)
                {
                    return new LogoffAndXResponse();
                }
                else if (state.IsTreeConnected(header.TID))
                {
                    string rootPath = state.GetConnectedTreePath(header.TID);
                    ISMBShare share;
                    if (state.IsIPC(header.TID))
                    {
                        share = m_services;
                    }
                    else
                    {
                        share = m_shares.GetShareFromRelativePath(rootPath);
                    }

                    if (command is CreateDirectoryRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        CreateDirectoryRequest request = (CreateDirectoryRequest)command;
                        return FileSystemResponseHelper.GetCreateDirectoryResponse(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is DeleteDirectoryRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        DeleteDirectoryRequest request = (DeleteDirectoryRequest)command;
                        return FileSystemResponseHelper.GetDeleteDirectoryResponse(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is CloseRequest)
                    {
                        CloseRequest request = (CloseRequest)command;
                        return ServerResponseHelper.GetCloseResponse(header, request, share, state);
                    }
                    else if (command is FlushRequest)
                    {
                        return new FlushResponse();
                    }
                    else if (command is DeleteRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        DeleteRequest request = (DeleteRequest)command;
                        return FileSystemResponseHelper.GetDeleteResponse(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is RenameRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        RenameRequest request = (RenameRequest)command;
                        return FileSystemResponseHelper.GetRenameResponse(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is QueryInformationRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        QueryInformationRequest request = (QueryInformationRequest)command;
                        return FileSystemResponseHelper.GetQueryInformationResponse(header, request, (FileSystemShare)share);
                    }
                    else if (command is SetInformationRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        SetInformationRequest request = (SetInformationRequest)command;
                        return FileSystemResponseHelper.GetSetInformationResponse(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is ReadRequest)
                    {
                        ReadRequest request = (ReadRequest)command;
                        return ReadWriteResponseHelper.GetReadResponse(header, request, share, state);
                    }
                    else if (command is WriteRequest)
                    {
                        string userName = state.GetConnectedUserName(header.UID);
                        if (share is FileSystemShare && !((FileSystemShare)share).HasWriteAccess(userName))
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return new ErrorResponse(command.CommandName);
                        }
                        WriteRequest request = (WriteRequest)command;
                        return ReadWriteResponseHelper.GetWriteResponse(header, request, share, state);
                    }
                    else if (command is CheckDirectoryRequest)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        CheckDirectoryRequest request = (CheckDirectoryRequest)command;
                        return FileSystemResponseHelper.GetCheckDirectoryResponse(header, request, (FileSystemShare)share);
                    }
                    else if (command is WriteRawRequest)
                    {
                        // [MS-CIFS] 3.3.5.26 - Receiving an SMB_COM_WRITE_RAW Request:
                        // the server MUST verify that the Server.Capabilities include CAP_RAW_MODE,
                        // If an error is detected [..] the Write Raw operation MUST fail and
                        // the server MUST return a Final Server Response [..] with the Count field set to zero.
                        return new WriteRawFinalResponse();
                    }
                    else if (command is SetInformation2Request)
                    {
                        if (!(share is FileSystemShare))
                        {
                            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                            return new ErrorResponse(command.CommandName);
                        }
                        SetInformation2Request request = (SetInformation2Request)command;
                        return FileSystemResponseHelper.GetSetInformation2Response(header, request, (FileSystemShare)share, state);
                    }
                    else if (command is LockingAndXRequest)
                    {
                        header.Status = NTStatus.STATUS_ACCESS_DENIED;
                        return new ErrorResponse(CommandName.SMB_COM_LOCKING_ANDX);
                    }
                    else if (command is OpenAndXRequest)
                    {
                        OpenAndXRequest request = (OpenAndXRequest)command;
                        return OpenAndXHelper.GetOpenAndXResponse(header, request, share, state);
                    }
                    else if (command is ReadAndXRequest)
                    {
                        ReadAndXRequest request = (ReadAndXRequest)command;
                        return ReadWriteResponseHelper.GetReadResponse(header, request, share, state);
                    }
                    else if (command is WriteAndXRequest)
                    {
                        string userName = state.GetConnectedUserName(header.UID);
                        if (share is FileSystemShare && !((FileSystemShare)share).HasWriteAccess(userName))
                        {
                            header.Status = NTStatus.STATUS_ACCESS_DENIED;
                            return new ErrorResponse(command.CommandName);
                        }
                        WriteAndXRequest request = (WriteAndXRequest)command;
                        return ReadWriteResponseHelper.GetWriteResponse(header, request, share, state);
                    }
                    else if (command is FindClose2Request)
                    {
                        return ServerResponseHelper.GetFindClose2Request(header, (FindClose2Request)command, state);
                    }
                    else if (command is TreeDisconnectRequest)
                    {
                        TreeDisconnectRequest request = (TreeDisconnectRequest)command;
                        return TreeConnectHelper.GetTreeDisconnectResponse(header, request, state);
                    }
                    else if (command is TransactionRequest) // Both TransactionRequest and Transaction2Request
                    {
                        TransactionRequest request = (TransactionRequest)command;
                        try
                        {
                            return TransactionHelper.GetTransactionResponse(header, request, share, state, sendQueue);
                        }
                        catch (UnsupportedInformationLevelException)
                        {
                            header.Status = NTStatus.STATUS_INVALID_PARAMETER;
                            return new ErrorResponse(command.CommandName);
                        }
                    }
                    else if (command is TransactionSecondaryRequest) // Both TransactionSecondaryRequest and Transaction2SecondaryRequest
                    {
                        TransactionSecondaryRequest request = (TransactionSecondaryRequest)command;
                        try
                        {
                            return TransactionHelper.GetTransactionResponse(header, request, share, state, sendQueue);
                        }
                        catch (UnsupportedInformationLevelException)
                        {
                            header.Status = NTStatus.STATUS_INVALID_PARAMETER;
                            return new ErrorResponse(command.CommandName);
                        }
                    }
                    else if (command is NTTransactRequest)
                    {
                        NTTransactRequest request = (NTTransactRequest)command;
                        return NTTransactHelper.GetNTTransactResponse(header, request, share, state, sendQueue);
                    }
                    else if (command is NTTransactSecondaryRequest)
                    {
                        NTTransactSecondaryRequest request = (NTTransactSecondaryRequest)command;
                        return NTTransactHelper.GetNTTransactResponse(header, request, share, state, sendQueue);
                    }
                    else if (command is NTCreateAndXRequest)
                    {
                        NTCreateAndXRequest request = (NTCreateAndXRequest)command;
                        return NTCreateHelper.GetNTCreateResponse(header, request, share, state);
                    }
                }
                else
                {
                    header.Status = NTStatus.STATUS_SMB_BAD_TID;
                    return new ErrorResponse(command.CommandName);
                }
            }

            header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
            return new ErrorResponse(command.CommandName);
        }

        public static void TrySendMessage(SMB1ConnectionState state, SMB1Message response)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = response.GetBytes();
            TrySendPacket(state, packet);
            System.Diagnostics.Debug.Print("[{0}] Response sent: {1} Commands, First Command: {2}, Packet length: {3}", DateTime.Now.ToString("HH:mm:ss:ffff"), response.Commands.Count, response.Commands[0].CommandName.ToString(), packet.Length);
        }

        private static void PrepareResponseHeader(SMB1Message response, SMB1Message request)
        {
            response.Header.Status = NTStatus.STATUS_SUCCESS;
            response.Header.Flags = HeaderFlags.CaseInsensitive | HeaderFlags.CanonicalizedPaths | HeaderFlags.Reply;
            response.Header.Flags2 = HeaderFlags2.NTStatusCode;
            if ((request.Header.Flags2 & HeaderFlags2.LongNamesAllowed) > 0)
            {
                response.Header.Flags2 |= HeaderFlags2.LongNamesAllowed | HeaderFlags2.LongNameUsed;
            }
            if ((request.Header.Flags2 & HeaderFlags2.ExtendedAttributes) > 0)
            {
                response.Header.Flags2 |= HeaderFlags2.ExtendedAttributes;
            }
            if ((request.Header.Flags2 & HeaderFlags2.ExtendedSecurity) > 0)
            {
                response.Header.Flags2 |= HeaderFlags2.ExtendedSecurity;
            }
            if ((request.Header.Flags2 & HeaderFlags2.Unicode) > 0)
            {
                response.Header.Flags2 |= HeaderFlags2.Unicode;
            }
            response.Header.MID = request.Header.MID;
            response.Header.PID = request.Header.PID;
            response.Header.UID = request.Header.UID;
            response.Header.TID = request.Header.TID;
        }
    }
}
