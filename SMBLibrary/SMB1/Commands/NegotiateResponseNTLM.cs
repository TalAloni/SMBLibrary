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

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_COM_NEGOTIATE Response, NT LAN Manager dialect
    /// </summary>
    public class NegotiateResponseNTLM : SMB1Command
    {
        public const int ParametersLength = 34;
        // Parameters:
        public ushort DialectIndex;
        public SecurityMode SecurityMode;
        public ushort MaxMpxCount;
        public ushort MaxNumberVcs;
        public uint MaxBufferSize;
        public uint MaxRawSize;
        public uint SessionKey;
        public ServerCapabilities Capabilities;
        public DateTime SystemTime;
        public short ServerTimeZone;
        //byte ChallengeLength;
        // Data:
        public byte[] Challenge;
        public string DomainName; // SMB_STRING (If Unicode, this field MUST be aligned to start on a 2-byte boundary from the start of the SMB header)
        public string ServerName; // SMB_STRING (this field WILL be aligned to start on a 2-byte boundary from the start of the SMB header)

        public NegotiateResponseNTLM() : base()
        {
            Challenge = new byte[0];
            DomainName = String.Empty;
            ServerName = String.Empty;
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte challengeLength = (byte)this.Challenge.Length;

            this.SMBParameters = new byte[ParametersLength];
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 0, DialectIndex);
            ByteWriter.WriteByte(this.SMBParameters, 2, (byte)SecurityMode);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 3, MaxMpxCount);
            LittleEndianWriter.WriteUInt16(this.SMBParameters, 5, MaxNumberVcs);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 7, MaxBufferSize);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 11, MaxRawSize);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 15, SessionKey);
            LittleEndianWriter.WriteUInt32(this.SMBParameters, 19, (uint)Capabilities);
            LittleEndianWriter.WriteInt64(this.SMBParameters, 23, SystemTime.ToFileTimeUtc());
            LittleEndianWriter.WriteInt16(this.SMBParameters, 31, ServerTimeZone);
            ByteWriter.WriteByte(this.SMBParameters, 33, challengeLength);

            int padding = 0;
            if (isUnicode)
            {
                padding = Challenge.Length % 2;
                this.SMBData = new byte[Challenge.Length + padding + DomainName.Length * 2 + ServerName.Length * 2 + 4];
            }
            else
            {
                this.SMBData = new byte[Challenge.Length + DomainName.Length + ServerName.Length + 2];
            }
            int offset = 0;
            ByteWriter.WriteBytes(this.SMBData, ref offset, Challenge);
            offset += padding;
            SMBHelper.WriteSMBString(this.SMBData, ref offset, isUnicode, DomainName);
            SMBHelper.WriteSMBString(this.SMBData, ref offset, isUnicode, ServerName);

            return base.GetBytes(isUnicode);
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_NEGOTIATE;
            }
        }
    }
}
