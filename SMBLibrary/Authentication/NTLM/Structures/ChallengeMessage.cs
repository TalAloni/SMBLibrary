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

namespace SMBLibrary.Authentication.NTLM
{
    /// <summary>
    /// [MS-NLMP] CHALLENGE_MESSAGE (Type 2 Message)
    /// </summary>
    public class ChallengeMessage
    {
        public string Signature; // 8 bytes
        public MessageTypeName MessageType;
        public string TargetName;
        public NegotiateFlags NegotiateFlags;
        public byte[] ServerChallenge; // 8 bytes
        // Reserved - 8 bytes
        public byte[] TargetInfo; // sequence of AV_PAIR structures
        public NTLMVersion Version;

        public ChallengeMessage()
        {
            Signature = AuthenticateMessage.ValidSignature;
            MessageType = MessageTypeName.Challenge;
        }

        public ChallengeMessage(byte[] buffer)
        {
            Signature = ByteReader.ReadAnsiString(buffer, 0, 8);
            MessageType = (MessageTypeName)LittleEndianConverter.ToUInt32(buffer, 8);
            TargetName = AuthenticationMessageUtils.ReadUnicodeStringBufferPointer(buffer, 12);
            NegotiateFlags = (NegotiateFlags)LittleEndianConverter.ToUInt32(buffer, 20);
            ServerChallenge = ByteReader.ReadBytes(buffer, 24, 8);
            // Reserved
            TargetInfo = AuthenticationMessageUtils.ReadBufferPointer(buffer, 40);
            if ((NegotiateFlags & NegotiateFlags.Version) > 0)
            {
                Version = new NTLMVersion(buffer, 48);
            }
        }

        public byte[] GetBytes()
        {
            int fixedLength = 48;
            if ((NegotiateFlags & NegotiateFlags.Version) > 0)
            {
                fixedLength += 8;
            }

            int payloadLength = TargetName.Length * 2 + TargetInfo.Length;
            byte[] buffer = new byte[fixedLength + payloadLength];
            ByteWriter.WriteAnsiString(buffer, 0, AuthenticateMessage.ValidSignature, 8);
            LittleEndianWriter.WriteUInt32(buffer, 8, (uint)MessageType);
            LittleEndianWriter.WriteUInt32(buffer, 20, (uint)NegotiateFlags);
            ByteWriter.WriteBytes(buffer, 24, ServerChallenge);
            if ((NegotiateFlags & NegotiateFlags.Version) > 0)
            {
                Version.WriteBytes(buffer, 48);
            }

            int offset = fixedLength;
            AuthenticationMessageUtils.WriteBufferPointer(buffer, 12, (ushort)(TargetName.Length * 2), (uint)offset);
            ByteWriter.WriteUTF16String(buffer, ref offset, TargetName);
            AuthenticationMessageUtils.WriteBufferPointer(buffer, 40, (ushort)TargetInfo.Length, (uint)offset);
            ByteWriter.WriteBytes(buffer, ref offset, TargetInfo);

            return buffer;
        }
    }
}
