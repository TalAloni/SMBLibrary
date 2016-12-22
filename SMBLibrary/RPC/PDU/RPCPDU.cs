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
    /// See DCE 1.1: Remote Procedure Call, Chapter 12.6 - Connection-oriented RPC PDUs
    /// </summary>
    public abstract class RPCPDU
    {
        public const int CommonFieldsLength = 16;

        // The common header fields, which appear in all (connection oriented) PDU types:
        public byte VersionMajor; // rpc_vers
        public byte VersionMinor; // rpc_vers_minor
        public PacketTypeName PacketType;
        public PacketFlags Flags;
        public DataRepresentationFormat DataRepresentation;
        public ushort FragmentLength;
        public ushort AuthLength;
        public uint CallID;

        public RPCPDU()
        {
            VersionMajor = 5;
            VersionMinor = 0;
        }

        public RPCPDU(byte[] buffer)
        {
            VersionMajor = ByteReader.ReadByte(buffer, 0);
            VersionMinor = ByteReader.ReadByte(buffer, 1);
            PacketType = (PacketTypeName)ByteReader.ReadByte(buffer, 2);
            Flags = (PacketFlags)ByteReader.ReadByte(buffer, 3);
            DataRepresentation = new DataRepresentationFormat(buffer, 4);
            FragmentLength = LittleEndianConverter.ToUInt16(buffer, 8);
            AuthLength = LittleEndianConverter.ToUInt16(buffer, 10);
            CallID = LittleEndianConverter.ToUInt32(buffer, 12);
        }

        public abstract byte[] GetBytes();

        public void WriteCommonFieldsBytes(byte[] buffer)
        {
            ByteWriter.WriteByte(buffer, 0, VersionMajor);
            ByteWriter.WriteByte(buffer, 1, VersionMinor);
            ByteWriter.WriteByte(buffer, 2, (byte)PacketType);
            ByteWriter.WriteByte(buffer, 3, (byte)Flags);
            DataRepresentation.WriteBytes(buffer, 4);
            LittleEndianWriter.WriteUInt16(buffer, 8, FragmentLength);
            LittleEndianWriter.WriteUInt16(buffer, 10, AuthLength);
            LittleEndianWriter.WriteUInt32(buffer, 12, CallID);
        }

        public static RPCPDU GetPDU(byte[] buffer)
        {
            PacketTypeName packetType = (PacketTypeName)ByteReader.ReadByte(buffer, 2);
            switch (packetType)
            {
                case PacketTypeName.Request:
                    return new RequestPDU(buffer);
                case PacketTypeName.Response:
                    return new ResponsePDU(buffer);
                case PacketTypeName.Fault:
                    return new FaultPDU(buffer);
                case PacketTypeName.Bind:
                    return new BindPDU(buffer);
                case PacketTypeName.BindAck:
                    return new BindAckPDU(buffer);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
