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
    public class QueryInfoStandard : QueryInformation
    {
        public const int Length = 22;

        public DateTime CreationDateTime;
        public DateTime LastAccessDateTime;
        public DateTime LastWriteDateTime;
        public uint FileDataSize;
        public uint AllocationSize;
        public FileAttributes Attributes;

        public QueryInfoStandard()
        {
        }

        public QueryInfoStandard(byte[] buffer, int offset)
        {
            CreationDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            LastAccessDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            LastWriteDateTime = SMBHelper.ReadSMBDateTime(buffer, ref offset);
            FileDataSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            AllocationSize = LittleEndianReader.ReadUInt32(buffer, ref offset);
            Attributes = (FileAttributes)LittleEndianReader.ReadUInt16(buffer, ref offset);
        }

        public override byte[] GetBytes()
        {
            byte[] buffer = new byte[Length];
            int offset = 0;
            SMBHelper.WriteSMBDateTime(buffer, ref offset, CreationDateTime);
            SMBHelper.WriteSMBDateTime(buffer, ref offset, LastAccessDateTime);
            SMBHelper.WriteSMBDateTime(buffer, ref offset, LastWriteDateTime);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, FileDataSize);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, AllocationSize);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, (ushort)Attributes);
            return buffer;
        }

        public override QueryInformationLevel InformationLevel
        {
            get
            {
                return QueryInformationLevel.SMB_INFO_STANDARD;
            }
        }
    }
}
