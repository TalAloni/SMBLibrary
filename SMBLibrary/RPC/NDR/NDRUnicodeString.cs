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

namespace SMBLibrary.RPC
{
    public class NDRUnicodeString : INDRStructure
    {
        public string Value;

        public NDRUnicodeString()
        {
            Value = String.Empty;
        }

        public NDRUnicodeString(string value)
        {
            Value = value;
        }

        public NDRUnicodeString(NDRParser parser)
        {
            Read(parser);
        }

        // 14.3.4.2 - Conformant and Varying Strings
        public void Read(NDRParser parser)
        {
            uint maxCount = parser.ReadUInt32();
            // the offset from the first index of the string to the first index of the actual subset being passed
            uint index = parser.ReadUInt32();
            // actualCount includes the null terminator
            uint actualCount = parser.ReadUInt32();
            StringBuilder builder = new StringBuilder();
            for (int position = 0; position < actualCount - 1; position++)
            {
                builder.Append((char)parser.ReadUInt16());
            }
            this.Value = builder.ToString();
            parser.ReadUInt16(); // null terminator
        }

        public void Write(NDRWriter writer)
        {
            int length = 0;
            if (Value != null)
            {
                length = Value.Length;
            }

            // maxCount includes the null terminator
            uint maxCount = (uint)(length + 1);
            writer.WriteUInt32(maxCount);
            // the offset from the first index of the string to the first index of the actual subset being passed
            uint index = 0;
            writer.WriteUInt32(index);
            // actualCount includes the null terminator
            uint actualCount = (uint)(length + 1);
            writer.WriteUInt32(actualCount);
            for (int position = 0; position < length; position++)
            {
                writer.WriteUInt16((ushort)Value[position]);
            }
            writer.WriteUInt16(0); // null terminator
        }
    }
}
