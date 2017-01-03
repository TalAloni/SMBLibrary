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

namespace SMBLibrary.Server
{
    public class NTTransactHelper
    {
        /// <summary>
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// </summary>
        internal static SMBCommand GetNTTransactResponse(SMBHeader header, NTTransactRequest request, object share, StateObject state, List<SMBCommand> sendQueue)
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
                return GetCompleteNTTransactResponse(header, request.Function, request.Setup, request.TransParameters, request.TransData, share, state, sendQueue);
            }
        }

        /// <summary>
        /// There are no secondary response messages.
        /// The client MUST send as many secondary requests as are needed to complete the transfer of the transaction request.
        /// </summary>
        internal static SMBCommand GetNTTransactResponse(SMBHeader header, NTTransactSecondaryRequest request, object share, StateObject state, List<SMBCommand> sendQueue)
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
                return null;
            }
            else
            {
                // We have a complete command
                return GetCompleteNTTransactResponse(header, (NTTransactSubcommandName)processState.SubcommandID, processState.TransactionSetup, processState.TransactionParameters, processState.TransactionData, share, state, sendQueue);
            }
        }

        internal static SMBCommand GetCompleteNTTransactResponse(SMBHeader header, NTTransactSubcommandName subcommandName, byte[] requestSetup, byte[] requestParameters, byte[] requestData, object share, StateObject state, List<SMBCommand> sendQueue)
        {
            NTTransactSubcommand subcommand = NTTransactSubcommand.GetSubcommandRequest(subcommandName, requestSetup, requestParameters, requestData, header.UnicodeFlag);
            NTTransactSubcommand subcommandResponse = null;

            if (subcommand is NTTransactCreateRequest)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is NTTransactIOCTLRequest)
            {
                subcommandResponse = GetSubcommandResponse(header, (NTTransactIOCTLRequest)subcommand);
            }
            else if (subcommand is NTTransactSetSecurityDescriptor)
            {
                header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
            }
            else if (subcommand is NTTransactNotifyChangeRequest)
            {
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
            NTTransactResponse response = new NTTransactResponse();
            PrepareResponse(response, responseSetup, responseParameters, responseData, state.MaxBufferSize, sendQueue);
            return response;
        }

        private static NTTransactIOCTLResponse GetSubcommandResponse(SMBHeader header, NTTransactIOCTLRequest subcommand)
        {
            const uint FSCTL_CREATE_OR_GET_OBJECT_ID = 0x900C0;

            NTTransactIOCTLResponse response = new NTTransactIOCTLResponse();
            if (subcommand.IsFsctl)
            {
                if (subcommand.FunctionCode == FSCTL_CREATE_OR_GET_OBJECT_ID)
                {
                    ObjectIDBufferType1 objectID = new ObjectIDBufferType1();
                    objectID.ObjectId = Guid.NewGuid();
                    response.Data = objectID.GetBytes();
                    return response;
                }
                else
                {
                    header.Status = NTStatus.STATUS_NOT_IMPLEMENTED;
                    return null;
                }
            }
            else
            {
                // [MS-SMB] If the IsFsctl field is set to zero, the server SHOULD fail the request with STATUS_NOT_SUPPORTED
                header.Status = NTStatus.STATUS_NOT_SUPPORTED;
                return null;
            }
        }

        private static void PrepareResponse(NTTransactResponse response, byte[] responseSetup, byte[] responseParameters, byte[] responseData, int maxBufferSize, List<SMBCommand> sendQueue)
        {
            if (NTTransactResponse.CalculateMessageSize(responseSetup.Length, responseParameters.Length, responseData.Length) <= maxBufferSize)
            {
                response.Setup = responseSetup;
                response.TotalParameterCount = (ushort)responseParameters.Length;
                response.TotalDataCount = (ushort)responseData.Length;
                response.TransParameters = responseParameters;
                response.TransData = responseData;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
