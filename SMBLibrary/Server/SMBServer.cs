/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SMBLibrary.NetBios;
using SMBLibrary.Server.SMB1;
using SMBLibrary.Services;
using SMBLibrary.SMB1;
using Utilities;

namespace SMBLibrary.Server
{
    public class SMBServer
    {
        public const int NetBiosOverTCPPort = 139;
        public const int DirectTCPPort = 445;
        public const string NTLanManagerDialect = "NT LM 0.12";
        public const bool EnableExtendedSecurity = true;

        private ShareCollection m_shares; // e.g. Shared folders
        private INTLMAuthenticationProvider m_users;
        private NamedPipeShare m_services; // Named pipes
        private IPAddress m_serverAddress;
        private SMBTransportType m_transport;

        private Socket m_listenerSocket;
        private bool m_listening;
        private Guid m_serverGuid;

        public SMBServer(ShareCollection shares, INTLMAuthenticationProvider users, IPAddress serverAddress, SMBTransportType transport)
        {
            m_shares = shares;
            m_users = users;
            m_serverAddress = serverAddress;
            m_serverGuid = Guid.NewGuid();
            m_transport = transport;

            m_services = new NamedPipeShare(shares.ListShares());
        }

        public void Start()
        {
            if (!m_listening)
            {
                m_listening = true;

                m_listenerSocket = new Socket(m_serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                int port = (m_transport == SMBTransportType.DirectTCPTransport ? DirectTCPPort : NetBiosOverTCPPort);
                m_listenerSocket.Bind(new IPEndPoint(m_serverAddress, port));
                m_listenerSocket.Listen((int)SocketOptionName.MaxConnections);
                m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
            }
        }

        public void Stop()
        {
            m_listening = false;
            SocketUtils.ReleaseSocket(m_listenerSocket);
        }

        // This method Accepts new connections
        private void ConnectRequestCallback(IAsyncResult ar)
        {
            System.Diagnostics.Debug.Print("[{0}] New connection request", DateTime.Now.ToString("HH:mm:ss:ffff"));
            Socket listenerSocket = (Socket)ar.AsyncState;

            Socket clientSocket;
            try
            {
                clientSocket = listenerSocket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                const int WSAECONNRESET = 10054;
                // Client may have closed the connection before we start to process the connection request.
                // When we get this error, we have to continue to accept other requests.
                // See http://stackoverflow.com/questions/7704417/socket-endaccept-error-10054
                if (ex.ErrorCode == WSAECONNRESET)
                {
                    m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
                }
                System.Diagnostics.Debug.Print("[{0}] Connection request error {1}", DateTime.Now.ToString("HH:mm:ss:ffff"), ex.ErrorCode);
                return;
            }

            SMB1ConnectionState state = new SMB1ConnectionState();
            // Disable the Nagle Algorithm for this tcp socket:
            clientSocket.NoDelay = true;
            state.ClientSocket = clientSocket;
            try
            {
                // Direct TCP transport packet is actually an NBT Session Message Packet,
                // So in either case (NetBios over TCP or Direct TCP Transport) we will receive an NBT packet.
                clientSocket.BeginReceive(state.ReceiveBuffer.Buffer, state.ReceiveBuffer.WriteOffset, state.ReceiveBuffer.AvailableLength, 0, ReceiveCallback, state);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
            m_listenerSocket.BeginAccept(ConnectRequestCallback, m_listenerSocket);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            SMB1ConnectionState state = (SMB1ConnectionState)result.AsyncState;
            Socket clientSocket = state.ClientSocket;

            if (!m_listening)
            {
                clientSocket.Close();
                return;
            }

            int numberOfBytesReceived;
            try
            {
                numberOfBytesReceived = clientSocket.EndReceive(result);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            if (numberOfBytesReceived == 0)
            {
                // The other side has closed the connection
                System.Diagnostics.Debug.Print("[{0}] The other side closed the connection", DateTime.Now.ToString("HH:mm:ss:ffff"));
                clientSocket.Close();
                return;
            }

            NBTConnectionReceiveBuffer receiveBuffer = state.ReceiveBuffer;
            receiveBuffer.SetNumberOfBytesReceived(numberOfBytesReceived);
            ProcessConnectionBuffer(state);

            if (clientSocket.Connected)
            {
                try
                {
                    clientSocket.BeginReceive(state.ReceiveBuffer.Buffer, state.ReceiveBuffer.WriteOffset, state.ReceiveBuffer.AvailableLength, 0, ReceiveCallback, state);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (SocketException)
                {
                }
            }
        }

        public void ProcessConnectionBuffer(SMB1ConnectionState state)
        {
            Socket clientSocket = state.ClientSocket;

            NBTConnectionReceiveBuffer receiveBuffer = state.ReceiveBuffer;
            while (receiveBuffer.HasCompletePacket())
            {
                SessionPacket packet = null;
                try
                {
                    packet = receiveBuffer.DequeuePacket();
                }
                catch (Exception)
                {
                    state.ClientSocket.Close();
                }

                if (packet != null)
                {
                    ProcessPacket(packet, state);
                }
            }
        }

        public void ProcessPacket(SessionPacket packet, SMB1ConnectionState state)
        {
            if (packet is SessionRequestPacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                PositiveSessionResponsePacket response = new PositiveSessionResponsePacket();
                TrySendPacket(state, response);
            }
            else if (packet is SessionKeepAlivePacket && m_transport == SMBTransportType.NetBiosOverTCP)
            {
                // [RFC 1001] NetBIOS session keep alives do not require a response from the NetBIOS peer
            }
            else if (packet is SessionMessagePacket)
            {
                SMB1Message message = null;
#if DEBUG
                message = SMB1Message.GetSMB1Message(packet.Trailer);
                System.Diagnostics.Debug.Print("[{0}] Message Received: {1} Commands, First Command: {2}, Packet length: {3}", DateTime.Now.ToString("HH:mm:ss:ffff"), message.Commands.Count, message.Commands[0].CommandName.ToString(), packet.Length);
#else
                try
                {
                    message = SMB1Message.GetSMB1Message(packet.Trailer);
                }
                catch (Exception)
                {
                    state.ClientSocket.Close();
                    return;
                }
#endif
                ProcessMessage(message, state);
            }
            else
            {
                System.Diagnostics.Debug.Print("[{0}] Invalid NetBIOS packet", DateTime.Now.ToString("HH:mm:ss:ffff"));
                state.ClientSocket.Close();
                return;
            }
        }

        public void ProcessMessage(SMB1Message message, SMB1ConnectionState state)
        {
            SMB1Message reply = new SMB1Message();
            PrepareResponseHeader(reply, message);
            List<SMB1Command> sendQueue = new List<SMB1Command>();

            foreach (SMB1Command command in message.Commands)
            {
                SMB1Command response = ProcessCommand(reply.Header, command, state, sendQueue);
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
        public SMB1Command ProcessCommand(SMB1Header header, SMB1Command command, SMB1ConnectionState state, List<SMB1Command> sendQueue)
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

        public static void TrySendMessage(SMB1ConnectionState state, SMB1Message reply)
        {
            SessionMessagePacket packet = new SessionMessagePacket();
            packet.Trailer = reply.GetBytes();
            TrySendPacket(state, packet);
            System.Diagnostics.Debug.Print("[{0}] Reply sent: {1} Commands, First Command: {2}, Packet length: {3}", DateTime.Now.ToString("HH:mm:ss:ffff"), reply.Commands.Count, reply.Commands[0].CommandName.ToString(), packet.Length);
        }

        public static void TrySendPacket(SMB1ConnectionState state, SessionPacket response)
        {
            Socket clientSocket = state.ClientSocket;
            try
            {
                clientSocket.Send(response.GetBytes());
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
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
