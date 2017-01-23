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
    public class TokenInitEntry
    {
        public List<byte[]> MechanismTypeList; // Optional
        // reqFlags - Optional, RECOMMENDED to be left out
        public byte[] MechanismToken; // Optional
        public byte[] MechanismListMIC; // Optional
    }

    /// <summary>
    /// RFC 4178 - negTokenInit
    /// </summary>
    public class SimpleProtectedNegotiationTokenInit : SimpleProtectedNegotiationToken
    {
        public const byte NegTokenInitTag = 0xA0;
        public const byte MechanismTypeListTag = 0xA0;
        public const byte RequiredFlagsTag = 0xA1;
        public const byte MechanismTokenTag = 0xA2;
        public const byte MechanismListMICTag = 0xA3;

        public List<TokenInitEntry> Tokens = new List<TokenInitEntry>();

        public SimpleProtectedNegotiationTokenInit()
        {
        }

        /// <param name="offset">The offset following the NegTokenInit tag</param>
        public SimpleProtectedNegotiationTokenInit(byte[] buffer, int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            int sequenceEndOffset = offset + constructionLength;
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Sequence)
            {
                throw new InvalidDataException();
            }
            while (offset < sequenceEndOffset)
            {
                int entryLength = DerEncodingHelper.ReadLength(buffer, ref offset);
                int entryEndOffset = offset + entryLength;
                TokenInitEntry entry = new TokenInitEntry();
                while (offset < entryEndOffset)
                {
                    tag = ByteReader.ReadByte(buffer, ref offset);
                    if (tag == MechanismTypeListTag)
                    {
                        entry.MechanismTypeList = ReadMechanismTypeList(buffer, ref offset);
                    }
                    else if (tag == RequiredFlagsTag)
                    {
                        throw new NotImplementedException("negTokenInit.ReqFlags is not implemented");
                    }
                    else if (tag == MechanismTokenTag)
                    {
                        entry.MechanismToken = ReadMechanismToken(buffer, ref offset);
                    }
                    else if (tag == MechanismListMICTag)
                    {
                        entry.MechanismListMIC = ReadMechanismListMIC(buffer, ref offset);
                    }
                    else
                    {
                        throw new InvalidDataException("Invalid negTokenInit structure");
                    }
                }
                Tokens.Add(entry);
            }
        }

        public override byte[] GetBytes()
        {
            int sequenceLength = 0;
            foreach (TokenInitEntry token in Tokens)
            {
                int entryLength = GetEntryLength(token);
                sequenceLength += DerEncodingHelper.GetLengthFieldSize(entryLength) + entryLength;
            }
            int constructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(1 + sequenceLength);
            int bufferSize = 1 + constructionLengthFieldSize + 1 + sequenceLength;
            byte[] buffer = new byte[bufferSize];
            int offset = 0;
            ByteWriter.WriteByte(buffer, ref offset, NegTokenInitTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + sequenceLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Sequence);
            foreach (TokenInitEntry token in Tokens)
            {
                int entryLength = GetEntryLength(token);
                DerEncodingHelper.WriteLength(buffer, ref offset, entryLength);
                if (token.MechanismTypeList != null)
                {
                    WriteMechanismTypeList(buffer, ref offset, token.MechanismTypeList);
                }
                if (token.MechanismToken != null)
                {
                    WriteMechanismToken(buffer, ref offset, token.MechanismToken);
                }
                if (token.MechanismListMIC != null)
                {
                    WriteMechanismListMIC(buffer, ref offset, token.MechanismListMIC);
                }
            }
            return buffer;
        }

        public int GetEntryLength(TokenInitEntry token)
        {
            int result = 0;
            if (token.MechanismTypeList != null)
            {
                int typeListSequenceLength = GetSequenceLength(token.MechanismTypeList);
                int constructionLenthFieldSize = DerEncodingHelper.GetLengthFieldSize(1 + typeListSequenceLength);
                int typeListLength = 1 + constructionLenthFieldSize + 1 + typeListSequenceLength;
                result += typeListLength;
            }
            if (token.MechanismToken != null)
            {
                int byteArrayFieldSize = DerEncodingHelper.GetLengthFieldSize(token.MechanismToken.Length);
                int constructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(1 + byteArrayFieldSize + token.MechanismToken.Length);
                int tokenLength = 1 + constructionLengthFieldSize + 1 + byteArrayFieldSize + token.MechanismToken.Length;
                result += tokenLength;
            }
            return result;
        }

        private static List<byte[]> ReadMechanismTypeList(byte[] buffer, ref int offset)
        {
            List<byte[]> result = new List<byte[]>();
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            int sequenceEndOffset = offset + constructionLength;
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Sequence)
            {
                throw new InvalidDataException();
            }
            while (offset < sequenceEndOffset)
            {
                int entryLength = DerEncodingHelper.ReadLength(buffer, ref offset);
                int entryEndOffset = offset + entryLength;
                tag = ByteReader.ReadByte(buffer, ref offset);
                if (tag != (byte)DerEncodingTag.ObjectIdentifier)
                {
                    throw new InvalidDataException();
                }
                int mechanismTypeLength = DerEncodingHelper.ReadLength(buffer, ref offset);
                byte[] mechanismType = ByteReader.ReadBytes(buffer, ref offset, mechanismTypeLength);
                result.Add(mechanismType);
            }
            return result;
        }

        private static byte[] ReadMechanismToken(byte[] buffer, ref int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.ByteArray)
            {
                throw new InvalidDataException();
            }
            int mechanismTokenLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte[] token = ByteReader.ReadBytes(buffer, ref offset, mechanismTokenLength);
            return token;
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

        private static int GetSequenceLength(List<byte[]> mechanismTypeList)
        {
            int sequenceLength = 0;
            foreach (byte[] mechanismType in mechanismTypeList)
            {
                int lengthFieldSize = DerEncodingHelper.GetLengthFieldSize(mechanismType.Length);
                int entryLength = 1 + lengthFieldSize + mechanismType.Length;
                sequenceLength += DerEncodingHelper.GetLengthFieldSize(entryLength) + entryLength;
            }
            return sequenceLength;
        }

        private static void WriteMechanismTypeList(byte[] buffer, ref int offset, List<byte[]> mechanismTypeList)
        {
            int sequenceLength = GetSequenceLength(mechanismTypeList);
            ByteWriter.WriteByte(buffer, ref offset, MechanismTypeListTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, 1 + sequenceLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Sequence);
            foreach (byte[] mechanismType in mechanismTypeList)
            {
                int lengthFieldSize = DerEncodingHelper.GetLengthFieldSize(mechanismType.Length);
                int entryLength = 1 + lengthFieldSize + mechanismType.Length;

                DerEncodingHelper.WriteLength(buffer, ref offset, entryLength);
                ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ObjectIdentifier);
                DerEncodingHelper.WriteLength(buffer, ref offset, mechanismType.Length);
                ByteWriter.WriteBytes(buffer, ref offset, mechanismType);
            }
        }

        private static void WriteMechanismToken(byte[] buffer, ref int offset, byte[] mechanismToken)
        {
            int constructionLength = 1 + DerEncodingHelper.GetLengthFieldSize(mechanismToken.Length) + mechanismToken.Length;
            ByteWriter.WriteByte(buffer, ref offset, MechanismTokenTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, constructionLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.ByteArray);
            DerEncodingHelper.WriteLength(buffer, ref offset, mechanismToken.Length);
            ByteWriter.WriteBytes(buffer, ref offset, mechanismToken);
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
