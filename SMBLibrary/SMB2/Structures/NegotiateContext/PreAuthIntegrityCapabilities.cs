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
    public class PreAuthIntegrityCapabilities
    {
        // ushort HashAlgorithmCount;
        // ushort SaltLength;
        public List<HashAlgorithm> HashAlgorithms = new List<HashAlgorithm>();
        public byte[] Salt;

        public PreAuthIntegrityCapabilities()
        {
        }

        public PreAuthIntegrityCapabilities(byte[] buffer, int offset)
        {
            ushort hashAlgorithmCount = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            ushort saltLength = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            for (int index = 0; index < hashAlgorithmCount; index++)
            {
                HashAlgorithms.Add((HashAlgorithm)LittleEndianConverter.ToUInt16(buffer, offset + 4 + index * 2));
            }
            Salt = ByteReader.ReadBytes(buffer, offset + 4 + hashAlgorithmCount * 2, saltLength);
        }

        public byte[] GetBytes()
        {
            int length = 4 + HashAlgorithms.Count * 2 + Salt.Length;

            byte[] buffer = new byte[length];
            LittleEndianWriter.WriteUInt16(buffer, 0, (ushort)HashAlgorithms.Count);
            LittleEndianWriter.WriteUInt16(buffer, 2, (ushort)Salt.Length);
            for (int index = 0; index < HashAlgorithms.Count; index++)
            {
                LittleEndianWriter.WriteUInt16(buffer, 4 + index * 2, (ushort)HashAlgorithms[index]);
            }
            ByteWriter.WriteBytes(buffer, 4 + HashAlgorithms.Count * 2, Salt);

            return buffer;
        }
    }
}
