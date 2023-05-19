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
    /// [MS-FSCC] 2.4.10 - FileDirectoryInformation
    /// </summary>
    public class FileDirectoryInformation : QueryDirectoryFileInformation
    {
        public const int FixedLength = 64;

        public DateTime CreationTime;
        public DateTime LastAccessTime;
        public DateTime LastWriteTime;
        public DateTime ChangeTime;
        public long EndOfFile;
        public long AllocationSize;
        public FileAttributes FileAttributes;
        private uint FileNameLength;
        public string FileName = String.Empty;

        public FileDirectoryInformation()
        {
        }

        public FileDirectoryInformation(byte[] buffer, int offset) : base(buffer, offset)
        {
            CreationTime = Conversion.ToFileUtcDateTime(LittleEndianConverter.ToInt64(buffer, offset + 8));
            LastAccessTime = Conversion.ToFileUtcDateTime(LittleEndianConverter.ToInt64(buffer, offset + 16));
            LastWriteTime = Conversion.ToFileUtcDateTime(LittleEndianConverter.ToInt64(buffer, offset + 24));
            ChangeTime = Conversion.ToFileUtcDateTime(LittleEndianConverter.ToInt64(buffer, offset + 32));
            EndOfFile = LittleEndianConverter.ToInt64(buffer, offset + 40);
            AllocationSize = LittleEndianConverter.ToInt64(buffer, offset + 48);
            FileAttributes = (FileAttributes)LittleEndianConverter.ToUInt32(buffer, offset + 56);
            FileNameLength = LittleEndianConverter.ToUInt32(buffer, offset + 60);
            FileName = ByteReader.ReadUTF16String(buffer, offset + 64, (int)FileNameLength / 2);
        }

        public override void WriteBytes(byte[] buffer, int offset)
        {
            base.WriteBytes(buffer, offset);
            FileNameLength = (uint)(FileName.Length * 2);
            LittleEndianWriter.WriteInt64(buffer, offset + 8, CreationTime.ToFileTimeUtc());
            LittleEndianWriter.WriteInt64(buffer, offset + 16, LastAccessTime.ToFileTimeUtc());
            LittleEndianWriter.WriteInt64(buffer, offset + 24, LastWriteTime.ToFileTimeUtc());
            LittleEndianWriter.WriteInt64(buffer, offset + 32, ChangeTime.ToFileTimeUtc());
            LittleEndianWriter.WriteInt64(buffer, offset + 40, EndOfFile);
            LittleEndianWriter.WriteInt64(buffer, offset + 48, AllocationSize);
            LittleEndianWriter.WriteUInt32(buffer, offset + 56, (uint)FileAttributes);
            LittleEndianWriter.WriteUInt32(buffer, offset + 60, FileNameLength);
            ByteWriter.WriteUTF16String(buffer, offset + 64, FileName);
        }

        public override FileInformationClass FileInformationClass
        {
            get
            {
                return FileInformationClass.FileDirectoryInformation;
            }
        }

        public override int Length
        {
            get
            {
                return FixedLength + FileName.Length * 2;
            }
        }
    }
}
