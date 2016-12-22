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

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_FEA
    /// </summary>
    public class FullExtendedAttribute
    {
        public ExtendedAttributeFlag ExtendedAttributeFlag;
        //byte AttributeNameLengthInBytes;
        //ushort AttributeValueLengthInBytes;
        public string AttributeName; // ANSI, AttributeNameLengthInBytes + 1 byte null termination
        public string AttributeValue; // ANSI

        public FullExtendedAttribute(byte[] buffer, int offset)
        {
            ExtendedAttributeFlag = (ExtendedAttributeFlag)ByteReader.ReadByte(buffer, offset);
            byte attributeNameLengthInBytes = ByteReader.ReadByte(buffer, offset + 1);
            ushort attributeValueLengthInBytes = LittleEndianConverter.ToUInt16(buffer, offset + 2);
            AttributeName = ByteReader.ReadAnsiString(buffer, offset + 4, attributeNameLengthInBytes);
            AttributeValue = ByteReader.ReadAnsiString(buffer, offset + 4 + attributeNameLengthInBytes + 1, attributeValueLengthInBytes);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            byte attributeNameLengthInBytes = (byte)AttributeName.Length;
            ushort attributeValueLengthInBytes = (ushort)AttributeValue.Length;
            ByteWriter.WriteByte(buffer, offset, (byte)ExtendedAttributeFlag);
            ByteWriter.WriteByte(buffer, offset + 1, attributeNameLengthInBytes);
            LittleEndianWriter.WriteUInt16(buffer, offset + 2, attributeValueLengthInBytes);
            ByteWriter.WriteAnsiString(buffer, offset + 4, AttributeName, AttributeValue.Length);
            ByteWriter.WriteAnsiString(buffer, offset + 4 + attributeNameLengthInBytes + 1, AttributeValue, AttributeValue.Length);
        }

        public int Length
        {
            get
            {
                return 4 + AttributeName.Length + 1 + AttributeValue.Length;
            }
        }
    }
}
