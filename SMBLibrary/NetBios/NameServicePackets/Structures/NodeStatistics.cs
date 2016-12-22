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

namespace SMBLibrary.NetBios
{
    public class NodeStatistics
    {
        public const int Length = 46;

        public byte[] UnitID; // MAC address, 6 bytes;
        public byte Jumpers;
        public byte TestResult;
        public ushort VersionNumber;
        public ushort PeriodOfStatistics;
        public ushort NumberOfCRCs;
        public ushort NumberOfAlignmentErrors;
        public ushort NumberOfCollisions;
        public ushort NumberOfSendAborts;
        public uint NumberOfGoodSends;
        public uint NumberOfGoodReceives;
        public ushort NumberOfRetransmits;
        public ushort NumberOfNoResourceConditions;
        public ushort NumberOfFreeCommandBlocks;
        public ushort TotalNumberOfCommandBlocks;
        public ushort MaxTotalNumberOfCommandBlocks;
        public ushort NumberOfPendingSessions;
        public ushort MaxNumberOfPendingSessions;
        public ushort MaxTotalsSessionsPossible;
        public ushort SessionDataPacketSize;

        public NodeStatistics()
        {
            UnitID = new byte[6];
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            ByteWriter.WriteBytes(buffer, ref offset, UnitID, 6);
            ByteWriter.WriteByte(buffer, ref offset, Jumpers);
            ByteWriter.WriteByte(buffer, ref offset, TestResult);
            BigEndianWriter.WriteUInt16(buffer, ref offset, VersionNumber);
            BigEndianWriter.WriteUInt16(buffer, ref offset, PeriodOfStatistics);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfCRCs);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfAlignmentErrors);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfCollisions);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfSendAborts);
            BigEndianWriter.WriteUInt32(buffer, ref offset, NumberOfGoodSends);
            BigEndianWriter.WriteUInt32(buffer, ref offset, NumberOfGoodReceives);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfRetransmits);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfNoResourceConditions);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfFreeCommandBlocks);
            BigEndianWriter.WriteUInt16(buffer, ref offset, TotalNumberOfCommandBlocks);
            BigEndianWriter.WriteUInt16(buffer, ref offset, MaxTotalNumberOfCommandBlocks);
            BigEndianWriter.WriteUInt16(buffer, ref offset, NumberOfPendingSessions);
            BigEndianWriter.WriteUInt16(buffer, ref offset, MaxNumberOfPendingSessions);
            BigEndianWriter.WriteUInt16(buffer, ref offset, MaxTotalsSessionsPossible);
            BigEndianWriter.WriteUInt16(buffer, ref offset, SessionDataPacketSize);
        }

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[Length];
            WriteBytes(buffer, 0);
            return buffer;
        }
    }
}
