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
    /// SMB_INFO_QUERY_EAS_FROM_LIST
    /// </summary>
    public class FindInfoQueryExtendedAttributesFromList : FindInformationEntry
    {
        public uint ResumeKey; // Optional
        public DateTime CreationDateTime;
        public DateTime LastAccessDateTime;
        public DateTime LastWriteDateTime;
        public uint FileDataSize;
        public uint AllocationSize;
        public FileAttributes Attributes;
        public FullExtendedAttributeList ExtendedAttributeList;
        //byte FileNameLength; // In bytes, MUST exclude the null termination.
        public string FileName; // OEM / Unicode character array. MUST be written as SMB_STRING, and read as fixed length string.

        public FindInfoQueryExtendedAttributesFromList(bool returnResumeKeys) : base(returnResumeKeys)
        {
        }

        public FindInfoQueryExtendedAttributesFromList(byte[] buffer, ref int offset, bool isUnicode, bool returnResumeKeys) : base(returnResumeKeys)
        {
            if (returnResumeKeys)
            {
                ResumeKey = LittleEndianReader.ReadUInt32(buffer, ref offset);
            }
            CreationDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            LastAccessDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            LastWriteDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            FileDataSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            AllocationSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            Attributes = (FileAttributes)LittleEndianReader.ReadUInt16(buffer, ref offset);
            ExtendedAttributeList = new FullExtendedAttributeList(buffer, offset);
            byte fileNameLength = ByteReader.ReadByte(buffer, ref offset);
            FileName = SMBHelper.ReadFixedLengthString(buffer, ref offset, isUnicode, fileNameLength);
        }

        public override void WriteBytes(byte[] buffer, ref int offset, bool isUnicode)
        {
            byte fileNameLength = (byte)(isUnicode ? FileName.Length * 2 : FileName.Length);

            if (ReturnResumeKeys)
            {
                LittleEndianWriter.WriteUInt32(buffer, ref offset, ResumeKey);
            }
            SMBHelper.WriteSMBDateTime(buffer, ref offset, CreationDateTime);
            SMBHelper.WriteSMBDateTime(buffer, ref offset, LastAccessDateTime);
            SMBHelper.WriteSMBDateTime(buffer, ref offset, LastWriteDateTime);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, FileDataSize);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, AllocationSize);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, (ushort)Attributes);
            ExtendedAttributeList.WriteBytes(buffer, ref offset);
            ByteWriter.WriteByte(buffer, ref offset, fileNameLength);
            SMBHelper.WriteSMBString(buffer, ref offset, isUnicode, FileName);
        }

        public override int GetLength(bool isUnicode)
        {
            int length = 27 + ExtendedAttributeList.Length;
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
