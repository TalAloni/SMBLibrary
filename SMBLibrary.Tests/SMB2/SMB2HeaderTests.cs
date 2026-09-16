/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.SMB2
{
    [TestClass]
    public class SMB2HeaderTests
    {
        [TestMethod]
        public void WriteAndReadSyncHeader_ChannelSequenceAndReservedRoundtrip()
        {
            // Arrange
            SMB2Header header = new SMB2Header(SMB2CommandName.Read);
            header.MessageID = 42;
            header.SessionID = 1234;
            header.TreeID = 7;
            header.ChannelSequence = 0x0203;
            header.Reserved = 0x0405;

            byte[] buffer = new byte[SMB2Header.Length];

            // Act
            header.WriteBytes(buffer, 0);
            SMB2Header rewritten = new SMB2Header(buffer, 0);

            // Assert
            Assert.AreEqual((ushort)0x0203, rewritten.ChannelSequence);
            Assert.AreEqual((ushort)0x0405, rewritten.Reserved);
            Assert.AreEqual((uint)7, rewritten.TreeID);
            Assert.AreEqual((ulong)1234, rewritten.SessionID);
        }

        [TestMethod]
        public void WriteAndReadSyncHeader_ChannelSequenceOccupiesLowerTwoBytesOfLegacyReservedField()
        {
            // Arrange - a header carrying only what used to be a single 32-bit "Reserved" value
            // (e.g. produced by code that has not been updated to set ChannelSequence) must still
            // round-trip: the legacy 4-byte value 0x00040203 splits into ChannelSequence=0x0203
            // (low 16 bits, written/read first at the same offset) and Reserved=0x0004 (high 16 bits).
            SMB2Header header = new SMB2Header(SMB2CommandName.Read);
            header.ChannelSequence = 0x0203;
            header.Reserved = 0x0004;

            byte[] buffer = new byte[SMB2Header.Length];

            // Act
            header.WriteBytes(buffer, 0);

            // Assert - byte 32/33 hold ChannelSequence (little-endian), byte 34/35 hold Reserved
            Assert.AreEqual(0x03, buffer[32]);
            Assert.AreEqual(0x02, buffer[33]);
            Assert.AreEqual(0x04, buffer[34]);
            Assert.AreEqual(0x00, buffer[35]);
        }
    }
}
