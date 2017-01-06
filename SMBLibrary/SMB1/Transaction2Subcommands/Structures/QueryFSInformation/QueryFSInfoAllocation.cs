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
    /// SMB_INFO_ALLOCATION
    /// </summary>
    public class QueryFSInfoAllocation : QueryFSInformation
    {
        public const int Length = 18;

        public uint FileSystemID; // File system identifier, Windows Server will set it to 0
        public uint SectorUnit; // Number of sectors per allocation unit
        public uint UnitsTotal; // Total number of allocation units
        public uint UnitsAvailable; // Total number of available allocation units
        public ushort Sector; // Number of bytes per sector

        public QueryFSInfoAllocation()
        {
        }

        public QueryFSInfoAllocation(byte[] buffer, int offset)
        {
            FileSystemID = LittleEndianConverter.ToUInt32(buffer, offset + 0);
            SectorUnit = LittleEndianConverter.ToUInt32(buffer, offset + 4);
            UnitsTotal = LittleEndianConverter.ToUInt32(buffer, offset + 8);
            UnitsAvailable = LittleEndianConverter.ToUInt32(buffer, offset + 12);
            Sector = LittleEndianConverter.ToUInt16(buffer, offset + 16);
        }

        public override byte[] GetBytes(bool isUnicode)
        {
            byte[] buffer = new byte[Length];
            LittleEndianWriter.WriteUInt32(buffer, 0, FileSystemID);
            LittleEndianWriter.WriteUInt32(buffer, 4, SectorUnit);
            LittleEndianWriter.WriteUInt32(buffer, 8, UnitsTotal);
            LittleEndianWriter.WriteUInt32(buffer, 12, UnitsAvailable);
            LittleEndianWriter.WriteUInt16(buffer, 16, Sector);
            return buffer;
        }
    }
}
