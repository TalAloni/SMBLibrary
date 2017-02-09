/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-FSCC] 2.4.40 - FileStreamInformation
    /// </summary>
    public class FileStreamInformation : FileInformation
    {
        public const int FixedLength = 24;

        public uint NextEntryOffset;
        private uint StreamNameLength;
        public ulong StreamSize;
        public ulong StreamAllocationSize;
        public string StreamName = String.Empty;

        public FileStreamInformation()
        {
        }

        public FileStreamInformation(byte[] buffer, int offset)
        {
            NextEntryOffset = LittleEndianConverter.ToUInt32(buffer, offset + 0);
            StreamNameLength = LittleEndianConverter.ToUInt32(buffer, offset + 4);
            StreamSize = LittleEndianConverter.ToUInt64(buffer, offset + 8);
            StreamAllocationSize = LittleEndianConverter.ToUInt64(buffer, offset + 16);
            ByteReader.ReadUTF16String(buffer, offset + 24, (int)StreamNameLength / 2);

        }

        public override void WriteBytes(byte[] buffer, int offset)
        {
            StreamNameLength = (uint)(StreamName.Length * 2);
            LittleEndianWriter.WriteUInt32(buffer, offset + 0, NextEntryOffset);
            LittleEndianWriter.WriteUInt32(buffer, offset + 4, StreamNameLength);
            LittleEndianWriter.WriteUInt64(buffer, offset + 8, StreamSize);
            LittleEndianWriter.WriteUInt64(buffer, offset + 16, StreamAllocationSize);
            ByteWriter.WriteUTF16String(buffer, offset + 24, StreamName);
        }

        public override FileInformationClass FileInformationClass
        {
            get
            {
                return FileInformationClass.FileStreamInformation;
            }
        }

        public override int Length
        {
            get
            {
                return FixedLength + StreamName.Length * 2;
            }
        }
    }
}
