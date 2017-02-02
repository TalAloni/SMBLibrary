/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.SMB1;
using SMBLibrary.RPC;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class TransactionHelper
    {
        /// <summary>
        /// There are no interim response messages.
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// The server MUST respond to the transaction request as a whole.
        /// </summary>
        internal static SMB1Command GetTransactionResponse(SMB1Header header, TransactionRequest request, ISMBShare share, SMB1ConnectionState state, List<SMB1Command> sendQueue)
        {
            ProcessStateObject processState = state.ObtainProcessState(header.PID);
            processState.MaxDataCount = request.MaxDataCount;

            if (request.TransParameters.Length < request.TotalParameterCount ||
                request.TransData.Length < request.TotalDataCount)
            {
                // A secondary transaction request is pending
                processState.Name = request.Name;
                processState.TransactionSetup = request.Setup;
                processState.TransactionParameters = new byte[request.TotalParameterCount];
                processState.TransactionData = new byte[request.TotalDataCount];
                ByteWriter.WriteBytes(processState.TransactionParameters, 0, request.TransParameters);
                ByteWriter.WriteBytes(processState.TransactionData, 0, request.TransData);
                processState.TransactionParametersReceived += request.TransParameters.Length;
                processState.TransactionDataReceived += request.TransData.Length;
                return null;
            }
            else
            {
                // We have a complete command
                if (request is Transaction2Request)
                {
                    return GetCompleteTransaction2Response(header, request.Setup, request.TransParameters, request.TransData, share, state, sendQueue);
                }
                else
                {
                    return GetCompleteTransactionResponse(header, request.Name, request.Setup, request.TransParameters, request.TransData, share, state, sendQueue);
                }
            }
        }

        /// <summary>
        /// There are no secondary response messages.
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// The server MUST respond to the transaction request as a whole.
        /// </summary>
        internal static SMB1Command GetTransactionResponse(SMB1Header header, TransactionSecondaryRequest request, ISMBShare share, SMB1ConnectionState state, List<SMB1Command> sendQueue)
        {
            ProcessStateObject processState = state.GetProcessState(header.PID);
            if (processState == null)
            {
                throw new InvalidRequestException();
            }
            ByteWriter.WriteBytes(processState.TransactionParameters, request.ParameterDisplacement, request.TransParameters);
            ByteWriter.WriteBytes(processState.TransactionData, request.DataDisplacement, request.TransData);
            processState.TransactionParametersReceived += request.TransParameters.Length;
            processState.TransactionDataReceived += request.TransData.Length;

            if (processState.TransactionParametersReceived < processState.TransactionParameters.Length ||
                processState.TransactionDataReceived < processState.TransactionData.Length)
            {
                return null;
            }
            else
            {
                // We have a complete command
                if (request is Transaction2SecondaryRequest)
                {
                    return GetCompleteTransaction2Response(header, processState.TransactionSetup, processState.TransactionParameters, processState.TransactionData, share, state, sendQueue);
                }
                else
                {
                    return GetCompleteTransactionResponse(header, processState.Name, processState.TransactionSetup, processState.TransactionParameters, processState.TransactionData, share, state, sendQueue);
                }
            }
        }

        internal static SMB1Command GetCompleteTransactionResponse(SMB1Header header, string name, byte[] requestSetup, byte[] requestParameters, byte[] requestData, ISMBShare share, SMB1ConnectionState state, List<SMB1Command> sendQueue)
        {
            TransactionSubcommand subcommand = TransactionSubcommand.GetSubcommandRequest(requestSetup, requestParameters, requestData, header.UnicodeFlag);
            TransactionSubcommand subcommandResponse = null;

            if (subcommand is TransactionSetNamedPipeStateRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionRawReadNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionQueryNamedPipeStateRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionQueryNamedPipeInfoRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionPeekNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionTransactNamedPipeRequest)
            {
                if (!(share is NamedPipeShare))
                {
                    header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                    return new ErrorResponse(CommandName.SMB_COM_TRANSACTION);
                }

                subcommandResponse = TransactionSubcommandHelper.GetSubcommandResponse(header, (TransactionTransactNamedPipeRequest)subcommand, (NamedPipeShare)share, state);
            }
            else if (subcommand is TransactionRawWriteNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionReadNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionWriteNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionWaitNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is TransactionCallNamedPipeRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else
            {
                header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
            }

            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_TRANSACTION);
            }

            byte[] responseSetup = subcommandResponse.GetSetup();
            byte[] responseParameters = subcommandResponse.GetParameters(header.UnicodeFlag);
            byte[] responseData = subcommandResponse.GetData();
            TransactionResponse response = new TransactionResponse();
            PrepareResponse(response, responseSetup, responseParameters, responseData, state.MaxBufferSize, sendQueue);
            return response;
        }

        internal static SMB1Command GetCompleteTransaction2Response(SMB1Header header, byte[] requestSetup, byte[] requestParameters, byte[] requestData, ISMBShare share, SMB1ConnectionState state, List<SMB1Command> sendQueue)
        {
            Transaction2Subcommand subcommand = Transaction2Subcommand.GetSubcommandRequest(requestSetup, requestParameters, requestData, header.UnicodeFlag);
            Transaction2Subcommand subcommandResponse = null;

            if (!(share is FileSystemShare))
            {
                header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
                return new ErrorResponse(CommandName.SMB_COM_TRANSACTION2);
            }

            FileSystemShare fileSystemShare = (FileSystemShare)share;

            if (subcommand is Transaction2FindFirst2Request)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2FindFirst2Request)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2FindNext2Request)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2FindNext2Request)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2QueryFSInformationRequest)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2QueryFSInformationRequest)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2QueryPathInformationRequest)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2QueryPathInformationRequest)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2SetPathInformationRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is Transaction2QueryFileInformationRequest)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2QueryFileInformationRequest)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2SetFileInformationRequest)
            {
                subcommandResponse = Transaction2SubcommandHelper.GetSubcommandResponse(header, (Transaction2SetFileInformationRequest)subcommand, fileSystemShare, state);
            }
            else if (subcommand is Transaction2CreateDirectoryRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is Transaction2GetDfsReferralRequest)
            {
                header.Status = NTStatus.STATUS_NO_SUCH_DEVICE;
            }
            else
            {
                header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
            }

            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_TRANSACTION2);
            }

            byte[] responseSetup = subcommandResponse.GetSetup();
            byte[] responseParameters = subcommandResponse.GetParameters(header.UnicodeFlag);
            byte[] responseData = subcommandResponse.GetData(header.UnicodeFlag);
            Transaction2Response response = new Transaction2Response();
            PrepareResponse(response, responseSetup, responseParameters, responseData, state.MaxBufferSize, sendQueue);
            return response;
        }

        internal static void PrepareResponse(TransactionResponse response, byte[] responseSetup, byte[] responseParameters, byte[] responseData, int maxBufferSize, List<SMB1Command> sendQueue)
        {
            int responseSize = TransactionResponse.CalculateMessageSize(responseSetup.Length, responseParameters.Length, responseData.Length);
            if (responseSize <= maxBufferSize)
            {
                response.Setup = responseSetup;
                response.TotalParameterCount = (ushort)responseParameters.Length;
                response.TotalDataCount = (ushort)responseData.Length;
                response.TransParameters = responseParameters;
                response.TransData = responseData;
            }
            else
            {
                int currentDataLength = maxBufferSize - (responseSize - responseData.Length);
                byte[] buffer = new byte[currentDataLength];
                Array.Copy(responseData, 0, buffer, 0, currentDataLength);
                response.Setup = responseSetup;
                response.TotalParameterCount = (ushort)responseParameters.Length;
                response.TotalDataCount = (ushort)responseData.Length;
                response.TransParameters = responseParameters;
                response.TransData = buffer;

                int dataBytesLeftToSend = responseData.Length - currentDataLength;
                while (dataBytesLeftToSend > 0)
                {
                    TransactionResponse additionalResponse;
                    if (response is Transaction2Response)
                    {
                        additionalResponse = new Transaction2Response();
                    }
                    else
                    {
                        additionalResponse = new TransactionResponse();
                    }
                    currentDataLength = dataBytesLeftToSend;
                    responseSize = TransactionResponse.CalculateMessageSize(0, 0, dataBytesLeftToSend);
                    if (responseSize > maxBufferSize)
                    {
                        currentDataLength = maxBufferSize - (responseSize - dataBytesLeftToSend);
                    }
                    buffer = new byte[currentDataLength];
                    int dataBytesSent = responseData.Length - dataBytesLeftToSend;
                    Array.Copy(responseData, dataBytesSent, buffer, 0, currentDataLength);
                    additionalResponse.TotalParameterCount = (ushort)responseParameters.Length;
                    additionalResponse.TotalDataCount = (ushort)responseData.Length;
                    additionalResponse.TransData = buffer;
                    additionalResponse.ParameterDisplacement = (ushort)response.TransParameters.Length;
                    additionalResponse.DataDisplacement = (ushort)dataBytesSent;
                    sendQueue.Add(additionalResponse);

                    dataBytesLeftToSend -= currentDataLength;
                }
            }
        }
    }
}
