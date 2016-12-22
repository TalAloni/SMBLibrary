/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.RPC
{
    /// <summary>
    /// rpcconn_request_hdr_t
    /// </summary>
    public class RequestPDU : RPCPDU
    {
        public uint AllocationHint; // alloc_hint
        public ushort ContextID;
        public ushort OpNum;
        public Guid ObjectGuid; // Optional field
        public byte[] Data;
        public byte[] AuthVerifier;

        public RequestPDU() : base()
        {
            PacketType = PacketTypeName.Request;
            AuthVerifier = new byte[0];
        }

        public RequestPDU(byte[] buffer) : base(buffer)
        {
            int offset = RPCPDU.CommonFieldsLength;
            AllocationHint = LittleEndianReader.ReadUInt32(buffer, ref offset);
            ContextID = LittleEndianReader.ReadUInt16(buffer, ref offset);
            OpNum = LittleEndianReader.ReadUInt16(buffer, ref offset);
            if ((Flags & PacketFlags.ObjectUUID) > 0)
            {
                ObjectGuid = LittleEndianReader.ReadGuid(buffer, ref offset);
            }
            int dataLength = FragmentLength - AuthLength - offset;
            Data = ByteReader.ReadBytes(buffer, ref offset, dataLength);
            AuthVerifier = ByteReader.ReadBytes(buffer, offset, AuthLength);
        }

        public override byte[] GetBytes()
        {
            AuthLength = (ushort)AuthVerifier.Length;
            FragmentLength = (ushort)(RPCPDU.CommonFieldsLength + 8 + Data.Length + AuthVerifier.Length);
            if ((Flags & PacketFlags.ObjectUUID) > 0)
            {
                FragmentLength += 16;
            }
            byte[] buffer = new byte[FragmentLength];
            WriteCommonFieldsBytes(buffer);
            int offset = RPCPDU.CommonFieldsLength;
            LittleEndianWriter.WriteUInt32(buffer, ref offset, AllocationHint);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, ContextID);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, OpNum);
            if ((Flags & PacketFlags.ObjectUUID) > 0)
            {
                LittleEndianWriter.WriteGuidBytes(buffer, ref offset, ObjectGuid);
            }
            ByteWriter.WriteBytes(buffer, ref offset, Data);
            ByteWriter.WriteBytes(buffer, ref offset, AuthVerifier);
            return buffer;
        }


    }
}
