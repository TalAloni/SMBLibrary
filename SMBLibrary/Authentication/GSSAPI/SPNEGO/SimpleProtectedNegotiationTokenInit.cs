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

namespace SMBLibrary.Authentication.GSSAPI
{
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

        public List<byte[]> MechanismTypeList; // Optional
        // reqFlags - Optional, RECOMMENDED to be left out
        public byte[] MechanismToken; // Optional
        public byte[] MechanismListMIC; // Optional

        public SimpleProtectedNegotiationTokenInit()
        {
        }

        /// <param name="offset">The offset following the NegTokenInit tag</param>
        public SimpleProtectedNegotiationTokenInit(byte[] buffer, int offset)
        {
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Sequence)
            {
                throw new InvalidDataException();
            }
            int sequenceLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            int sequenceEndOffset = offset + sequenceLength;
            while (offset < sequenceEndOffset)
            {
                tag = ByteReader.ReadByte(buffer, ref offset);
                if (tag == MechanismTypeListTag)
                {
                    MechanismTypeList = ReadMechanismTypeList(buffer, ref offset);
                }
                else if (tag == RequiredFlagsTag)
                {
                    throw new NotImplementedException("negTokenInit.ReqFlags is not implemented");
                }
                else if (tag == MechanismTokenTag)
                {
                    MechanismToken = ReadMechanismToken(buffer, ref offset);
                }
                else if (tag == MechanismListMICTag)
                {
                    MechanismListMIC = ReadMechanismListMIC(buffer, ref offset);
                }
                else
                {
                    throw new InvalidDataException("Invalid negTokenInit structure");
                }
            }
        }

        public override byte[] GetBytes()
        {
            int sequenceLength = GetTokenFieldsLength();
            int sequenceLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(sequenceLength);
            int constructionLength = 1 + sequenceLengthFieldSize + sequenceLength;
            int constructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(constructionLength);
            int bufferSize = 1 + constructionLengthFieldSize + 1 + sequenceLengthFieldSize + sequenceLength;
            byte[] buffer = new byte[bufferSize];
            int offset = 0;
            ByteWriter.WriteByte(buffer, ref offset, NegTokenInitTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, constructionLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Sequence);
            DerEncodingHelper.WriteLength(buffer, ref offset, sequenceLength);
            if (MechanismTypeList != null)
            {
                WriteMechanismTypeList(buffer, ref offset, MechanismTypeList);
            }
            if (MechanismToken != null)
            {
                WriteMechanismToken(buffer, ref offset, MechanismToken);
            }
            if (MechanismListMIC != null)
            {
                WriteMechanismListMIC(buffer, ref offset, MechanismListMIC);
            }
            return buffer;
        }

        private int GetTokenFieldsLength()
        {
            int result = 0;
            if (MechanismTypeList != null)
            {
                int typeListSequenceLength = GetSequenceLength(MechanismTypeList);
                int typeListSequenceLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(typeListSequenceLength);
                int typeListConstructionLength = 1 + typeListSequenceLengthFieldSize + typeListSequenceLength;
                int typeListConstructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(typeListConstructionLength);
                int typeListLength = 1 + typeListConstructionLengthFieldSize + 1 + typeListSequenceLengthFieldSize + typeListSequenceLength;
                result += typeListLength;
            }
            if (MechanismToken != null)
            {
                int mechanismTokenBytesFieldSize = DerEncodingHelper.GetLengthFieldSize(MechanismToken.Length);
                int mechanismTokenConstructionLength = 1 + mechanismTokenBytesFieldSize + MechanismToken.Length;
                int mechanismTokenConstructionLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(mechanismTokenConstructionLength);
                int tokenLength = 1 + mechanismTokenConstructionLengthFieldSize + 1 + mechanismTokenBytesFieldSize + MechanismToken.Length;
                result += tokenLength;
            }
            return result;
        }

        private static List<byte[]> ReadMechanismTypeList(byte[] buffer, ref int offset)
        {
            List<byte[]> result = new List<byte[]>();
            int constructionLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            byte tag = ByteReader.ReadByte(buffer, ref offset);
            if (tag != (byte)DerEncodingTag.Sequence)
            {
                throw new InvalidDataException();
            }
            int sequenceLength = DerEncodingHelper.ReadLength(buffer, ref offset);
            int sequenceEndOffset = offset + sequenceLength;
            while (offset < sequenceEndOffset)
            {
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
                sequenceLength += entryLength;
            }
            return sequenceLength;
        }

        private static void WriteMechanismTypeList(byte[] buffer, ref int offset, List<byte[]> mechanismTypeList)
        {
            int sequenceLength = GetSequenceLength(mechanismTypeList);
            int sequenceLengthFieldSize = DerEncodingHelper.GetLengthFieldSize(sequenceLength);
            int constructionLength = 1 + sequenceLengthFieldSize + sequenceLength;
            ByteWriter.WriteByte(buffer, ref offset, MechanismTypeListTag);
            DerEncodingHelper.WriteLength(buffer, ref offset, constructionLength);
            ByteWriter.WriteByte(buffer, ref offset, (byte)DerEncodingTag.Sequence);
            DerEncodingHelper.WriteLength(buffer, ref offset, sequenceLength);
            foreach (byte[] mechanismType in mechanismTypeList)
            {
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
