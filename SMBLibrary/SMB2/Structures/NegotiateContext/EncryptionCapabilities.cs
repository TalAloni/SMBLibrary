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
    /// [MS-SMB2] 2.2.3.1.2 SMB2_ENCRYPTION_CAPABILITIES
    /// </summary>
    public class EncryptionCapabilities : NegotiateContext
    {
        // ushort CipherCount;
        public List<CipherAlgorithm> Ciphers = new List<CipherAlgorithm>();

        public EncryptionCapabilities()
        {
        }

        public EncryptionCapabilities(byte[] buffer, int offset) : base(buffer, offset)
        {
            ushort cipherCount = LittleEndianConverter.ToUInt16(Data, 0);
            for (int index = 0; index < cipherCount; index++)
            {
                Ciphers.Add((CipherAlgorithm)LittleEndianConverter.ToUInt16(Data, index * 2));
            }
        }

        public override void WriteData()
        {
            Data = new byte[DataLength];
            LittleEndianWriter.WriteUInt16(Data, 0, (ushort)Ciphers.Count);
            for (int index = 0; index < Ciphers.Count; index++)
            {
                LittleEndianWriter.WriteUInt16(Data, 2 + index * 2, (ushort)Ciphers[index]);
            }
        }

        public override int DataLength
        {
            get
            {
                return 2 + Ciphers.Count * 2;
            }
        }

        public override NegotiateContextType ContextType => NegotiateContextType.SMB2_ENCRYPTION_CAPABILITIES;
    }
}
