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

namespace SMBLibrary
{
    /// <summary>
    /// [MS-FSCC] FILE_FULL_EA_INFORMATION data element
    /// </summary>
    public class FileFullEAInformation
    {
        public uint NextEntryOffset;
        public byte Flags;
        //byte EaNameLength;
        //ushort EaValueLength;
        public string EaName; // ASCII
        public string EaValue; // ASCII

        public FileFullEAInformation()
        {
        }

        public FileFullEAInformation(byte[] buffer, int offset)
        {
            NextEntryOffset = LittleEndianReader.ReadUInt32(buffer, ref offset);
            Flags = ByteReader.ReadByte(buffer, ref offset);
            byte eaNameLength = ByteReader.ReadByte(buffer, ref offset);
            ushort eaValueLength = LittleEndianReader.ReadUInt16(buffer, ref offset);
            EaName = ByteReader.ReadAnsiString(buffer, ref offset, eaNameLength);
            EaValue = ByteReader.ReadAnsiString(buffer, ref offset, eaValueLength);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            byte eaNameLength = (byte)EaName.Length;
            ushort eaValueLength = (ushort)EaValue.Length;
            LittleEndianWriter.WriteUInt32(buffer, ref offset, NextEntryOffset);
            ByteWriter.WriteByte(buffer, ref offset, Flags);
            ByteWriter.WriteByte(buffer, ref offset, eaNameLength);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, eaValueLength);
            ByteWriter.WriteAnsiString(buffer, ref offset, EaName);
            ByteWriter.WriteAnsiString(buffer, ref offset, EaValue);
        }

        public int Length
        {
            get
            {
                return 8 + EaName.Length + EaValue.Length;
            }
        }
    }
}
