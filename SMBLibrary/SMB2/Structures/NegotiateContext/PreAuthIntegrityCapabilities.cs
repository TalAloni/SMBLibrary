/* Copyright (C) 2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.SMB2
{
    /// <summary>
    /// [MS-SMB2] 2.2.3.1.1 - SMB2_PREAUTH_INTEGRITY_CAPABILITIES
    /// </summary>
    public class PreAuthIntegrityCapabilities : NegotiateContext
    {
        // ushort HashAlgorithmCount;
        // ushort SaltLength;
        public List<HashAlgorithm> HashAlgorithms = new List<HashAlgorithm>();
        public byte[] Salt;

        public PreAuthIntegrityCapabilities()
        {
        }

        public PreAuthIntegrityCapabilities(byte[] buffer, int offset) : base(buffer, offset)
        {
            ushort hashAlgorithmCount = LittleEndianConverter.ToUInt16(Data, 0);
            ushort saltLength = LittleEndianConverter.ToUInt16(Data, 2);
            for (int index = 0; index < hashAlgorithmCount; index++)
            {
                HashAlgorithms.Add((HashAlgorithm)LittleEndianConverter.ToUInt16(Data, 4 + index * 2));
            }
            Salt = ByteReader.ReadBytes(Data, 4 + hashAlgorithmCount * 2, saltLength);
        }

        public override void WriteData()
        {
            Data = new byte[DataLength];
            LittleEndianWriter.WriteUInt16(Data, 0, (ushort)HashAlgorithms.Count);
            LittleEndianWriter.WriteUInt16(Data, 2, (ushort)Salt.Length);
            for (int index = 0; index < HashAlgorithms.Count; index++)
            {
                LittleEndianWriter.WriteUInt16(Data, 4 + index * 2, (ushort)HashAlgorithms[index]);
            }
            ByteWriter.WriteBytes(Data, 4 + HashAlgorithms.Count * 2, Salt);
        }

        public override int DataLength
        {
            get
            {
                return 4 + HashAlgorithms.Count * 2 + Salt.Length;
            }
        }

        public override NegotiateContextType ContextType => NegotiateContextType.SMB2_PREAUTH_INTEGRITY_CAPABILITIES;
    }
}
