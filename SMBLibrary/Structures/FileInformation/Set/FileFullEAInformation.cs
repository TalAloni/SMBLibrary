/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
    /// [MS-FSCC] 2.4.15 - FileFullEaInformation
    /// </summary>
    public class FileFullEAInformation : FileInformation
    {
        public const int FixedLength = 8;

        public uint NextEntryOffset;
        public byte Flags;
        private byte EaNameLength;
        private ushort EaValueLength;
        public string EaName; // 8-bit ASCII
        public string EaValue; // 8-bit ASCII

        public FileFullEAInformation()
        {
        }

        public FileFullEAInformation(byte[] buffer, int offset)
        {
            NextEntryOffset = LittleEndianReader.ReadUInt32(buffer, ref offset);
            Flags = ByteReader.ReadByte(buffer, ref offset);
            EaNameLength = ByteReader.ReadByte(buffer, ref offset);
            EaValueLength = LittleEndianReader.ReadUInt16(buffer, ref offset);
            EaName = ByteReader.ReadAnsiString(buffer, ref offset, EaNameLength);
            EaValue = ByteReader.ReadAnsiString(buffer, ref offset, EaValueLength);
        }

        public override void WriteBytes(byte[] buffer, int offset)
        {
            EaNameLength = (byte)EaName.Length;
            EaValueLength = (ushort)EaValue.Length;
            LittleEndianWriter.WriteUInt32(buffer, ref offset, NextEntryOffset);
            ByteWriter.WriteByte(buffer, ref offset, Flags);
            ByteWriter.WriteByte(buffer, ref offset, EaNameLength);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, EaValueLength);
            ByteWriter.WriteAnsiString(buffer, ref offset, EaName);
            ByteWriter.WriteAnsiString(buffer, ref offset, EaValue);
        }

        public override FileInformationClass FileInformationClass
        {
            get
            {
                return FileInformationClass.FileFullEaInformation;
            }
        }

        public override int Length
        {
            get
            {
                return FixedLength + EaName.Length + EaValue.Length;
            }
        }

        public static List<FileFullEAInformation> ReadList(byte[] buffer, int offset)
        {
            List<FileFullEAInformation> result = new List<FileFullEAInformation>();
            FileFullEAInformation entry;
            do
            {
                entry = new FileFullEAInformation(buffer, offset);
                result.Add(entry);
            }
            while (entry.NextEntryOffset != 0);
            return result;
        }

        public static void WriteList(byte[] buffer, int offset, List<FileFullEAInformation> list)
        {
            // When multiple FILE_FULL_EA_INFORMATION data elements are present in the buffer, each MUST be aligned on a 4-byte boundary
            for (int index = 0; index < list.Count; index++)
            {
                FileFullEAInformation entry = list[index];
                entry.WriteBytes(buffer, offset);
                int entryLength = entry.Length;
                offset += entryLength;
                if (index < list.Count - 1)
                {
                    int padding = (4 - (entryLength % 4)) % 4;
                    offset += padding;
                }
            }
        }

        public int GetListLength(List<FileFullEAInformation> list)
        {
            // When multiple FILE_FULL_EA_INFORMATION data elements are present in the buffer, each MUST be aligned on a 4-byte boundary
            int length = 0;
            for (int index = 0; index < list.Count; index++)
            {
                length += list[index].Length;
                if (index < list.Count - 1)
                {
                    int padding = (4 - (length % 4)) % 4;
                    length += padding;
                }
            }
            return length;
        }
    }
}
