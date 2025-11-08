/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.Security.Cryptography;
using Utilities;

namespace SMBLibrary.SMB1
{
    internal class SMB1Cryptography
    {
        public static ulong CalculateSignature(byte[] signingKey, byte[] challengeResponse, byte[] buffer, int offset, int paddedLength)
        {
            byte[] temp;
            if (challengeResponse != null)
            {
                temp = ByteUtils.Concatenate(ByteUtils.Concatenate(signingKey, challengeResponse), ByteReader.ReadBytes(buffer, offset, paddedLength));
            }
            else
            {
                temp = ByteUtils.Concatenate(signingKey, ByteReader.ReadBytes(buffer, offset, paddedLength));
            }
            byte[] hash = MD5.Create().ComputeHash(temp);
            return LittleEndianConverter.ToUInt64(hash, 0);
        }
    }
}
