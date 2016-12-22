/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utilities;

namespace SMBLibrary.NetBios
{
    /// <summary>
    /// [RFC 1002] 4.2.1.2. QUESTION SECTION
    /// </summary>
    public class QuestionSection
    {
        public string Name;
        public NameRecordType Type = NameRecordType.NB; // NB
        public ushort Class = 0x0001; // IN

        public QuestionSection()
        {
        }

        public QuestionSection(byte[] buffer, ref int offset)
        {
            Name = NetBiosUtils.DecodeName(buffer, ref offset);
            Type = (NameRecordType)BigEndianReader.ReadUInt16(buffer, ref offset);
            Class = BigEndianReader.ReadUInt16(buffer, ref offset);
        }

        public void WriteBytes(Stream stream)
        {
            byte[] encodedName = NetBiosUtils.EncodeName(Name, String.Empty);
            ByteWriter.WriteBytes(stream, encodedName);
            BigEndianWriter.WriteUInt16(stream, (ushort)Type);
            BigEndianWriter.WriteUInt16(stream, Class);
        }
    }
}
