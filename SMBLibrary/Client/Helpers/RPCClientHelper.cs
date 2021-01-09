/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.RPC;
using SMBLibrary.Services;
using Utilities;

namespace SMBLibrary.Client
{
    public class RPCClientHelper
    {
        const ushort MaxTransmitFragmentSize = 5680;
        const ushort MaxReceiveFragmentSize = 5680;
        public static NTStatus Bind(INTFileStore namedPipeShare, string pipeName, Guid ServiceInterfaceGuid, uint ServiceVersion, out object pipeHandle)
        {
            NTStatus status;
            FileStatus fileStatus;
            status = namedPipeShare.CreateFile(out pipeHandle, out fileStatus, pipeName, (AccessMask)(FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA), 0, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, 0, null);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }
            BindPDU bindPDU = new BindPDU();
            bindPDU.Flags = PacketFlags.FirstFragment | PacketFlags.LastFragment;
            bindPDU.DataRepresentation.CharacterFormat = CharacterFormat.ASCII;
            bindPDU.DataRepresentation.ByteOrder = ByteOrder.LittleEndian;
            bindPDU.DataRepresentation.FloatingPointRepresentation = FloatingPointRepresentation.IEEE;
            bindPDU.MaxTransmitFragmentSize = MaxTransmitFragmentSize;
            bindPDU.MaxReceiveFragmentSize = MaxReceiveFragmentSize;

            ContextElement serverServiceContext = new ContextElement();
            serverServiceContext.AbstractSyntax = new SyntaxID(ServiceInterfaceGuid, ServiceVersion);
            serverServiceContext.TransferSyntaxList.Add(new SyntaxID(RemoteServiceHelper.NDRTransferSyntaxIdentifier, RemoteServiceHelper.NDRTransferSyntaxVersion));

            bindPDU.ContextList.Add(serverServiceContext);

            byte[] input = bindPDU.GetBytes();
            byte[] output;
            status = namedPipeShare.DeviceIOControl(pipeHandle, (uint)IoControlCode.FSCTL_PIPE_TRANSCEIVE, input, out output, 4096);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }
            BindAckPDU bindAckPDU = RPCPDU.GetPDU(output, 0) as BindAckPDU;
            if (bindAckPDU == null)
            {
                status = NTStatus.STATUS_NOT_SUPPORTED;
                return status;
            }
            return status;
        }

        public static NTStatus ExecuteCall<I, O>(INTFileStore namedPipeShare, object pipeHandle, ushort OpNum, I inputArgs, out O outputData) where I : IRPCRequest
        {
            byte[] output;
            NTStatus status;
            outputData = default(O);

            RequestPDU requestPDU = new RequestPDU();
            requestPDU.Flags = PacketFlags.FirstFragment | PacketFlags.LastFragment;
            requestPDU.DataRepresentation.CharacterFormat = CharacterFormat.ASCII;
            requestPDU.DataRepresentation.ByteOrder = ByteOrder.LittleEndian;
            requestPDU.DataRepresentation.FloatingPointRepresentation = FloatingPointRepresentation.IEEE;
            requestPDU.OpNum = OpNum;
            requestPDU.Data = inputArgs.GetBytes();
            requestPDU.AllocationHint = (uint)requestPDU.Data.Length;
            byte[] input = requestPDU.GetBytes();
            int maxOutputLength = MaxTransmitFragmentSize;
            status = namedPipeShare.DeviceIOControl(pipeHandle, (uint)IoControlCode.FSCTL_PIPE_TRANSCEIVE, input, out output, maxOutputLength);
            if (status != NTStatus.STATUS_SUCCESS)
            {
                return status;
            }
            ResponsePDU responsePDU = RPCPDU.GetPDU(output, 0) as ResponsePDU;
            if (responsePDU == null)
            {
                status = NTStatus.STATUS_NOT_SUPPORTED;
                return status;
            }

            byte[] responseData = responsePDU.Data;
            while ((responsePDU.Flags & PacketFlags.LastFragment) == 0)
            {
                status = namedPipeShare.ReadFile(out output, pipeHandle, 0, maxOutputLength);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    return status;
                }
                responsePDU = RPCPDU.GetPDU(output, 0) as ResponsePDU;
                if (responsePDU == null)
                {
                    status = NTStatus.STATUS_NOT_SUPPORTED;
                    return status;
                }
                responseData = ByteUtils.Concatenate(responseData, responsePDU.Data);
            }
            outputData = (O) Activator.CreateInstance(typeof(O), new object[] { responseData });
            return NTStatus.STATUS_SUCCESS;
        }
    }
}
