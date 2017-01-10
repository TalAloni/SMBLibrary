/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace SMBLibrary.Authentication
{
    public enum NegState : byte
    {
        AcceptCompleted = 0x00,
        AcceptIncomplete = 0x01,
        Reject = 0x02,
        RequestMic = 0x03,
    }

    public class TokenResponseEntry
    {
        public NegState? NegState; // Optional
        public byte[] SupportedMechanism; // Optional
        public byte[] ResponseToken; // Optional
        public byte[] MechanismListMIC; // Optional
    }

    /// <summary>
    /// RFC 4178 - negTokenResp
    /// </summary>
    public class SimpleProtectedNegotiationTokenResponse : SimpleProtectedNegotiationToken
    {
        public const byte NegTokenRespTag = 0xA1;
        public const byte NegStateTag = 0xA0;
        public const byte SupportedMechanismTag = 0xA1;
        public const byte ResponseTokenTag = 0xA2;
        public const byte MechanismListMICTag = 0xA3;

        public List<TokenResponseEntry> Tokens = new List<TokenResponseEntry>();

        public SimpleProtectedNegotiationTokenResponse()
        {
        }

        public SimpleProtectedNegotiationTokenResponse(byte[] buffer, int offset)
        {
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != NegTokenRespTag)
            {
                throw new InvalidDataException();
            }
            int constuctionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            int sequenceEndOffset = offset + constuctionLength;
            tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Sequence)
            {
                throw new InvalidDataException();
            }
            while (offset < sequenceEndOffset)
            {
                int entryLength = DerEncodingHelper.ReadLength(buffer, ref offset);
                int entryEndOffset = offset + entryLength;
                TokenResponseEntry entry = new TokenResponseEntry();
                while (offset < entryEndOffset)
                {
                    tag = ByteReader.ReadByte(buffer, ref offset);
                    if (tag == NegStateTag)
                    {
                        entry.NegState = ReadNegState(buffer, ref offset);
                    }
                    else if (tag == SupportedMechanismTag)
                    {
                        entry.SupportedMechanism = ReadSupportedMechanism(buffer, ref offset);
                    }
                    else if (tag == ResponseTokenTag)
                    {
                        entry.ResponseToken = ReadResponseToken(buffer, ref offset);
                    }
                    else if (tag == MechanismListMICTag)
                    {
                        entry.MechanismListMIC = ReadMechanismListMIC(buffer, ref offset);
                    }
                    else
                    {
                        throw new InvalidDataException("Invalid negTokenResp structure");
                    }
                }
                Tokens.Add(entry);
            }
        }

        public override byte[] GetBytes()
        {
            int sequenceLength = 0;
            foreach (TokenResponseEntry token in Tokens)
            {
                int entryLength = GetEntryLength(token);
                sequenceLength += DerEncodingHelper.GetLengthFieldSize(entryLength) + entryLength;
            }
            int constructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(1 + sequenceLength);
            int bufferSize = 1 + constructionLengthFieldSize + 1 + sequenceLength;
            byte[] buffer = new byte[bufferSize];
            int offset = 0;
            ByteWriter.WriteByte(buffer, ref offset, NegTokenRespTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + sequenceLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Sequence);
            foreach (TokenResponseEntry token in Tokens)
            {
                int entryLength = GetEntryLength(token);
                DerEncodingHelper.WriteLength(buffer, ref offset, entryLength);
                if (token.NegState.HasValue)
                {
                    WriteNegState(buffer, ref offset, token.NegState.Value);
                }
                if (token.SupportedMechanism != null)
                {
                    WriteSupportedMechanism(buffer, ref offset, token.SupportedMechanism);
                }
                if (token.ResponseToken != null)
                {
                    WriteResponseToken(buffer, ref offset, token.ResponseToken);
                }
                if (token.MechanismListMIC != null)
                {
                    WriteMechanismListMIC(buffer, ref offset, token.MechanismListMIC);
                }
            }
            return buffer;
        }

        private static NegState ReadNegState(byte[] buffer, ref int offset)
        {
            int length = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Enum)
            {
                throw new InvalidDataException();
            }
            length = DerEncodingHelper.ReadLength(buffer, ref offset);
            return (NegState)ByteReader.ReadByte(buffer, ref offset);
        }

        private static byte[] ReadSupportedMechanism(byte[] buffer, ref int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.ObjectIdentifier)
            {
                throw new InvalidDataException();
            }
            int length = DerEncodingHelper.ReadLength(buffer, ref offset);
            return ByteReader.ReadBytes(buffer, ref offset, length);
        }

        private static byte[] ReadResponseToken(byte[] buffer, ref int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.ByteArray)
            {
                throw new InvalidDataException();
            }
            int length = DerEncodingHelper.ReadLength(buffer, ref offset);
            return ByteReader.ReadBytes(buffer, ref offset, length);
        }

        private static byte[] ReadMechanismListMIC(byte[] buffer, ref int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.ByteArray)
            {
                throw new InvalidDataException();
            }
            int length = DerEncodingHelper.ReadLength(buffer, ref offset);
            return ByteReader.ReadBytes(buffer, ref offset, length);
        }

        private static int GetEntryLength(TokenResponseEntry token)
        {
            int result = 0;
            if (token.NegState.HasValue)
            {
                int negStateLength = 5;
                result += negStateLength;
            }
            if (token.SupportedMechanism != null)
            {
                int supportedMechanismLength2FieldSize = DerEncodingHelper.GetLengthFieldSize(token.SupportedMechanism.Length);
                int supportedMechanismLength1FieldSize = DerEncodingHelper.GetLengthFieldSize(1 + supportedMechanismLength2FieldSize + token.SupportedMechanism.Length);
                int supportedMechanismLength = 1 + supportedMechanismLength1FieldSize + 1 + supportedMechanismLength2FieldSize + token.SupportedMechanism.Length;
                result += supportedMechanismLength;
            }
            if (token.ResponseToken != null)
            {
                int responseToken2FieldSize = DerEncodingHelper.GetLengthFieldSize(token.ResponseToken.Length);
                int responseToken1FieldSize = DerEncodingHelper.GetLengthFieldSize(1 + responseToken2FieldSize + token.ResponseToken.Length);
                int responseTokenLength = 1 + responseToken1FieldSize + 1 + responseToken2FieldSize + token.ResponseToken.Length;
                result += responseTokenLength;
            }
            return result;
        }

        private static void WriteNegState(byte[] buffer, ref int offset, NegState negState)
        {
            ByteWriter.WriteByte(buffer, ref offset, NegStateTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 3);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Enum);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1);
            ByteWriter.WriteByte(buffer, ref offset, (byte)negState);
        }

        private static void WriteSupportedMechanism(byte[] buffer, ref int offset, byte[] supportedMechanism)
        {
            int supportedMechanismLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(supportedMechanism.Length);
            ByteWriter.WriteByte(buffer, ref offset, SupportedMechanismTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + supportedMechanismLengthFieldSize + supportedMechanism.Length);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ObjectIdentifier);
            DerEncodingHelper.WriteLength(buffer, ref offset, supportedMechanism.Length);
            ByteWriter.WriteBytes(buffer, ref offset, supportedMechanism);
        }

        private static void WriteResponseToken(byte[] buffer, ref int offset, byte[] responseToken)
        {
            int responseTokenLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(responseToken.Length);
            ByteWriter.WriteByte(buffer, ref offset, ResponseTokenTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + responseTokenLengthFieldSize + responseToken.Length);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ByteArray);
            DerEncodingHelper.WriteLength(buffer, ref offset, responseToken.Length);
            ByteWriter.WriteBytes(buffer, ref offset, responseToken);
        }

        private static void WriteMechanismListMIC(byte[] buffer, ref int offset, byte[] mechanismListMIC)
        {
            int mechanismListMICLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(mechanismListMIC.Length);
            ByteWriter.WriteByte(buffer, ref offset, MechanismListMICTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + mechanismListMICLengthFieldSize + mechanismListMIC.Length);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ByteArray);
            DerEncodingHelper.WriteLength(buffer, ref offset, mechanismListMIC.Length);
            ByteWriter.WriteBytes(buffer, ref offset, mechanismListMIC);
        }
    }
}
