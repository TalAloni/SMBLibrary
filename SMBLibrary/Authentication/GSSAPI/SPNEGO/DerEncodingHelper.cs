/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Authentication.GSSAPI
{
    public enum DerEncodingTag : byte
    {
        ByteArray = 0x04,
        ObjectIdentifier = 0x06,
        Enum = 0x0A,
        Sequence = 0x30,
    }

    public class DerEncodingHelper
    {
        public static int ReadLength(byte[] buffer, ref int offset)
        {
            int length = ByteReader.ReadByte(buffer, ref offset);
            if (length >= 0x80)
            {
                int lengthFieldSize = (length & 0x7F);
                byte[] lengthField = ByteReader.ReadBytes(buffer, ref offset, lengthFieldSize);
                length = 0;
                foreach (byte value in lengthField)
                {
                    length *= 256;
                    length += value;
                }
            }
            return length;
        }

        public static void WriteLength(byte[] buffer, ref int offset, int length)
        {
            if (length >= 0x80)
            {
                List<byte> values = new List<byte>();
                do
                {
                    byte value = (byte)(length % 256);
                    values.Add(value);
                    length = value / 256;
                }
                while (length > 0);
                values.Reverse();
                byte[] lengthField = values.ToArray();
                ByteWriter.WriteByte(buffer, ref offset, (byte)(0x80 | lengthField.Length));
                ByteWriter.WriteBytes(buffer, ref offset, lengthField);
            }
            else
            {
                ByteWriter.WriteByte(buffer, ref offset, (byte)length);
            }
        }

        public static int GetLengthFieldSize(int length)
        {
            if (length >= 0x80)
            {
                int result = 1;
                do
                {
                    length = length / 256;
                    result++;
                }
                while(length > 0);
                return result;
            }
            else
            {
                return 1;
            }
        }
    }
}
