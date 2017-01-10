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
    /// SMB_COM_NEGOTIATE Response, NT LAN Manager dialect, Extended Security response
    /// </summary>
    public class NegotiateResponseNTLMExtended : SMB1Command
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
        //byte ChallengeLength; // MUST be set to 0
        // Data:
        public Guid ServerGuid;
        public byte[] SecurityBlob;

        public NegotiateResponseNTLMExtended() : base()
        {
            // MS-SMB Page 129: The server can leave SecurityBlob empty if not configured to send GSS token
            SecurityBlob = new byte[0];
        }

        public NegotiateResponseNTLMExtended(byte[] buffer, int offset) : base(buffer, offset, false)
        {
            throw new NotImplementedException();
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte challengeLength = 0;

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

            this.SMBData = new byte[16 + SecurityBlob.Length];
            LittleEndianWriter.WriteGuidBytes(this.SMBData, 0, ServerGuid);
            ByteWriter.WriteBytes(this.SMBData, 16, SecurityBlob);

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
