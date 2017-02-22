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
using Utilities;

namespace SMBLibrary.Server.SMB1
{
    public class NTTransactHelper
    {
        /// <summary>
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// </summary>
        internal static List<SMB1Command> GetNTTransactResponse(SMB1Header header, NTTransactRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            if (request.TransParameters.Length < request.TotalParameterCount ||
                request.TransData.Length < request.TotalDataCount)
            {
                ProcessStateObject processState = state.ObtainProcessState(header.PID);
                // A secondary transaction request is pending
                processState.SubcommandID = (ushort)request.Function;
                processState.TransactionSetup = request.Setup;
                processState.TransactionParameters = new byte[request.TotalParameterCount];
                processState.TransactionData = new byte[request.TotalDataCount];
                ByteWriter.WriteBytes(processState.TransactionParameters, 0, request.TransParameters);
                ByteWriter.WriteBytes(processState.TransactionData, 0, request.TransData);
                processState.TransactionParametersReceived += request.TransParameters.Length;
                processState.TransactionDataReceived += request.TransData.Length;
                return new NTTransactInterimResponse();
            }
            else
            {
                // We have a complete command
                return GetCompleteNTTransactResponse(header, request.Function, request.Setup, request.TransParameters, request.TransData, share, state);
            }
        }

        /// <summary>
        /// There are no secondary response messages.
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// </summary>
        internal static List<SMB1Command> GetNTTransactResponse(SMB1Header header, NTTransactSecondaryRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            ProcessStateObject processState = state.GetProcessState(header.PID);
            if (processState == null)
            {
                throw new InvalidRequestException();
            }
            ByteWriter.WriteBytes(processState.TransactionParameters, (int)request.ParameterDisplacement, request.TransParameters);
            ByteWriter.WriteBytes(processState.TransactionData, (int)request.DataDisplacement, request.TransData);
            processState.TransactionParametersReceived += request.TransParameters.Length;
            processState.TransactionDataReceived += request.TransData.Length;

            if (processState.TransactionParametersReceived < processState.TransactionParameters.Length ||
                processState.TransactionDataReceived < processState.TransactionData.Length)
            {
                return new List<SMB1Command>();
            }
            else
            {
                // We have a complete command
                return GetCompleteNTTransactResponse(header, (NTTransactSubcommandName)processState.SubcommandID, processState.TransactionSetup, processState.TransactionParameters, processState.TransactionData, share, state);
            }
        }

        internal static List<SMB1Command> GetCompleteNTTransactResponse(SMB1Header header, NTTransactSubcommandName subcommandName, byte[] requestSetup, byte[] requestParameters, byte[] requestData, ISMBShare share, SMB1ConnectionState state)
        {
            NTTransactSubcommand subcommand = NTTransactSubcommand.GetSubcommandRequest(subcommandName, requestSetup, requestParameters, requestData, header.UnicodeFlag);
            NTTransactSubcommand subcommandResponse = null;

            if (subcommand is NTTransactCreateRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is NTTransactIOCTLRequest)
            {
                subcommandResponse = GetSubcommandResponse(header, (NTTransactIOCTLRequest)subcommand, share, state);
            }
            else if (subcommand is NTTransactSetSecurityDescriptor)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is NTTransactNotifyChangeRequest)
            {
                // [MS-CIFS] If the server does not support the NT_TRANSACT_NOTIFY_CHANGE subcommand, it can return an
                // error response with STATUS_NOT_IMPLEMENTED [..] in response to an NT_TRANSACT_NOTIFY_CHANGE Request.
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is NTTransactQuerySecurityDescriptorRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else
            {
                header.Status = NTStatus.STATUS_SMB_BAD_COMMAND;
            }

            if (header.Status != NTStatus.STATUS_SUCCESS)
            {
                return new ErrorResponse(CommandName.SMB_COM_NT_TRANSACT);
            }

            byte[] responseSetup = subcommandResponse.GetSetup();
            byte[] responseParameters = subcommandResponse.GetParameters(header.UnicodeFlag);
            byte[] responseData = subcommandResponse.GetData();
            return GetNTTransactResponse(responseSetup, responseParameters, responseData, state.MaxBufferSize);
        }

        private static NTTransactIOCTLResponse GetSubcommandResponse(SMB1Header header, NTTransactIOCTLRequest subcommand, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            NTTransactIOCTLResponse response = new NTTransactIOCTLResponse();
            if (subcommand.IsFsctl)
            {
                OpenFileObject openFile = session.GetOpenFileObject(subcommand.FID);
                if (openFile == null)
                {
                    header.Status = NTStatus.STATUS_INVALID_HANDLE;
                    return null;
                }
                int maxOutputLength = UInt16.MaxValue;
                byte[] output;
                header.Status = share.FileStore.DeviceIOControl(openFile.Handle, subcommand.FunctionCode, subcommand.Data, out output, maxOutputLength);
                if (header.Status != NTStatus.STATUS_SUCCESS)
                {
                    return null;
                }

                response.Data = output;
                return response;
            }
            else
            {
                // [MS-SMB] If the IsFsctl field is set to zero, the server SHOULD fail the request with STATUS_NOT_SUPPORTED
                header.Status = NTStatus.STATUS_NOT_SUPPORTED;
                return null;
            }
        }

        private static List<SMB1Command> GetNTTransactResponse(byte[] responseSetup, byte[] responseParameters, byte[] responseData, int maxBufferSize)
        {
            if (NTTransactResponse.CalculateMessageSize(responseSetup.Length, responseParameters.Length, responseData.Length) <= maxBufferSize)
            {
                NTTransactResponse response = new NTTransactResponse();
                response.Setup = responseSetup;
                response.TotalParameterCount = (ushort)responseParameters.Length;
                response.TotalDataCount = (ushort)responseData.Length;
                response.TransParameters = responseParameters;
                response.TransData = responseData;
                return response;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
