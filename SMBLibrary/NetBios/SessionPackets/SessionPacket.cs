/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.NetBios
{
    /// <summary>
    /// [RFC 1002] 4.3.1. SESSION PACKET
    /// </summary>
    public abstract class SessionPacket
    {
        public const int MaxSessionPacketLength = 131075;

        public SessionPacketTypeName Type;
        public byte Flags;
        public int Length; // 2 bytes + length extension bit
        public byte[] Trailer;

        public SessionPacket()
        {
        }

        public SessionPacket(byte[] buffer, int offset)
        {
            Type = (SessionPacketTypeName)ByteReader.ReadByte(buffer, offset + 0);
            Flags = ByteReader.ReadByte(buffer, offset + 1);
            Length = (Flags & 0x01) << 16 | BigEndianConverter.ToUInt16(buffer, offset + 2);

            this.Trailer = ByteReader.ReadBytes(buffer, offset + 4, Length);
        }

        public virtual byte[] GetBytes()
        {
            Length = this.Trailer.Length;
            if (Length > 0x1FFFF)
            {
                throw new ArgumentException("Invalid NBT packet length");
            }

            Flags = Convert.ToByte(Length > 0xFFFF);

            byte[] buffer = new byte[4 + Trailer.Length];
            ByteWriter.WriteByte(buffer, 0, (byte)this.Type);
            ByteWriter.WriteByte(buffer, 1, Flags);
            BigEndianWriter.WriteUInt16(buffer, 2, (ushort)(Length & 0xFFFF));
            ByteWriter.WriteBytes(buffer, 4, this.Trailer);

            return buffer;
        }

        public static SessionPacket GetSessionPacket(byte[] buffer, int offset)
        {
            SessionPacketTypeName type = (SessionPacketTypeName)ByteReader.ReadByte(buffer, offset);
            switch (type)
            {
                case SessionPacketTypeName.SessionMessage:
                    return new SessionMessagePacket(buffer, offset);
                case SessionPacketTypeName.SessionRequest:
                    return new SessionRequestPacket(buffer, offset);
                case SessionPacketTypeName.PositiveSessionResponse:
                    return new PositiveSessionResponsePacket(buffer, offset);
                case SessionPacketTypeName.NegativeSessionResponse:
                    return new NegativeSessionResponsePacket(buffer, offset);
                case SessionPacketTypeName.RetargetSessionResponse:
                    return new SessionRetargetResponsePacket(buffer, offset);
                case SessionPacketTypeName.SessionKeepAlive:
                    return new SessionKeepAlivePacket(buffer, offset);
                default:
                    throw new InvalidRequestException("Invalid NetBIOS Session Packet");
            }
        }
    }
}
