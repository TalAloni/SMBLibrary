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
    /// SMB_INFO_STANDARD
    /// </summary>
    public class FindInfoStandard : FindInformation
    {
        public const int FixedLength = 23;

        public uint ResumeKey; // Optional
        public DateTime CreationDateTime;
        public DateTime LastAccessDateTime;
        public DateTime LastWriteDateTime;
        public uint FileDataSize;
        public uint AllocationSize;
        public FileAttributes Attributes;
        //byte FileNameLength;
        public string FileName; // SMB_STRING

        public FindInfoStandard(bool returnResumeKeys) : base(returnResumeKeys)
        {
        }

        public FindInfoStandard(byte[] buffer, ref int offset, bool isUnicode, bool returnResumeKeys) : base(returnResumeKeys)
        {
            if (returnResumeKeys)
            {
                ResumeKey = LittleEndianReader.ReadUInt32(buffer, ref offset);
            }
            CreationDateTime = SMB1Helper.ReadSMBDateTime(buffer, ref offset);
            LastAccessDateTime = SMB1Helper.ReadSMBDateTime(buffer, ref offset);
            LastWriteDateTime = SMB1Helper.ReadSMBDateTime(buffer, ref offset);
            FileDataSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            AllocationSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            Attributes = (FileAttributes)LittleEndianReader.ReadUInt16(buffer, ref offset);
            byte fileNameLength = ByteReader.ReadByte(buffer, ref offset);
            FileName = SMB1Helper.ReadSMBString(buffer, ref offset, isUnicode);
        }

        public override void WriteBytes(byte[] buffer, ref int offset, bool isUnicode)
        {
            byte fileNameLength = (byte)(isUnicode ? FileName.Length * 2 : FileName.Length);

            if (ReturnResumeKeys)
            {
                LittleEndianWriter.WriteUInt32(buffer, ref offset, ResumeKey);
            }
            SMB1Helper.WriteSMBDateTime(buffer, ref offset, CreationDateTime);
            SMB1Helper.WriteSMBDateTime(buffer, ref offset, LastAccessDateTime);
            SMB1Helper.WriteSMBDateTime(buffer, ref offset, LastWriteDateTime);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, FileDataSize);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, AllocationSize);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, (ushort)Attributes);
            ByteWriter.WriteByte(buffer, ref offset, fileNameLength);
            SMB1Helper.WriteSMBString(buffer, ref offset, isUnicode, FileName);
        }

        public override int GetLength(bool isUnicode)
        {
            int length = FixedLength;
            if (ReturnResumeKeys)
            {
                length += 4;
            }

            if (isUnicode)
            {
                length += FileName.Length * 2 + 2;
            }
            else
            {
                length += FileName.Length + 1;
            }
            return length;
        }
    }
}
