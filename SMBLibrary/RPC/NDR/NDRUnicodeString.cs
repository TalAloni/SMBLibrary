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
        // string are not in any case null terminated (RPC_UNICODE_STRING for example)
        // this flag is present to disable the Null Terminator check
        public bool IgnoreNullTerminator;

        public NDRUnicodeString()
        {
            Value = String.Empty;
        }

        public NDRUnicodeString(string value, bool ignoreNullTerminator = false)
        {
            Value = value;
            IgnoreNullTerminator = ignoreNullTerminator;
        }

        public NDRUnicodeString(NDRParser parser, bool ignoreNullTerminator = false)
        {
            IgnoreNullTerminator = ignoreNullTerminator;
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
            int max = IgnoreNullTerminator ? (int) actualCount : (int)actualCount - 1;
            for (int position = 0; position < max; position++)
            {
                builder.Append((char)parser.ReadUInt16());
            }
            this.Value = builder.ToString();
            if (!IgnoreNullTerminator)
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
            uint actualCount = (uint)(length + (IgnoreNullTerminator ? 0 : 1));
            writer.WriteUInt32(actualCount);
            for (int position = 0; position < length; position++)
            {
                writer.WriteUInt16((ushort)Value[position]);
            }
            if (!IgnoreNullTerminator)
            {
                writer.WriteUInt16(0); // null terminator
            }
        }
    }
}
