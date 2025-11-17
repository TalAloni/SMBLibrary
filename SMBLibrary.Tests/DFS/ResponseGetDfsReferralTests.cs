using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ResponseGetDfsReferralTests
    {
        [TestMethod]
        public void ParseResponseGetDfsReferral_HeaderOnly_SetsFields()
        {
            // Arrange
            // PathConsumed = 6, NumberOfReferrals = 0, ReferralHeaderFlags = 0x10
            byte[] buffer = new byte[8];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 0);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0x10);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual((ushort)6, response.PathConsumed);
            Assert.AreEqual((ushort)0, response.NumberOfReferrals);
            Assert.AreEqual((uint)0x10, response.ReferralHeaderFlags);
            Assert.IsNotNull(response.ReferralEntries);
            Assert.AreEqual(0, response.ReferralEntries.Count);
            Assert.IsNotNull(response.StringBuffer);
            Assert.AreEqual(0, response.StringBuffer.Count);
        }

        [TestMethod]
        public void GetBytes_ForHeaderOnlyResponse_ProducesExpectedBuffer()
        {
            // Arrange
            ResponseGetDfsReferral response = new ResponseGetDfsReferral();
            response.PathConsumed = 6;
            response.NumberOfReferrals = 0;
            response.ReferralHeaderFlags = 0x10;

            // Act
            byte[] buffer = response.GetBytes();

            // Assert
            Assert.AreEqual(8, buffer.Length);
            Assert.AreEqual((ushort)6, Utilities.LittleEndianConverter.ToUInt16(buffer, 0));
            Assert.AreEqual((ushort)0, Utilities.LittleEndianConverter.ToUInt16(buffer, 2));
            Assert.AreEqual((uint)0x10, Utilities.LittleEndianConverter.ToUInt32(buffer, 4));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_WhenBufferIsNull_ThrowsArgumentNullException()
        {
            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenBufferTooSmall_ThrowsArgumentException()
        {
            // Arrange
            byte[] buffer = new byte[7];

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenNumberOfReferralsPositiveButNoPayload_ThrowsArgumentException()
        {
            // Arrange
            byte[] buffer = new byte[8];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0x10);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_SingleReferral_PopulatesReferralEntries()
        {
            // Arrange
            byte[] buffer = new byte[24];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            // Minimal v1 entry header: VersionNumber = 1, Size = 16, TimeToLive = 0.
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 16);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual((ushort)6, response.PathConsumed);
            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_SingleReferralV1_ParsesHeaderFields()
        {
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 16);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 300);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV1 entry = response.ReferralEntries[0] as DfsReferralEntryV1;
            Assert.IsNotNull(entry);
            Assert.AreEqual((ushort)1, entry.VersionNumber);
            Assert.AreEqual((ushort)16, entry.Size);
            Assert.AreEqual((uint)300, entry.TimeToLive);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_SingleReferralV1_ParsesStringFields()
        {
            string dfsPath = "\\\\contoso.com\\Public";
            string networkAddress = "\\\\fs1\\Public";

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int entryOffset = 8;
            int entrySize = 16;
            int dfsPathOffset = entrySize;
            int networkAddressOffset = entrySize + dfsPathBytes.Length;

            int totalSize = 8 + entrySize + dfsPathBytes.Length + networkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, (ushort)dfsPathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 14, (ushort)networkAddressOffset);

            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsPathOffset, dfsPathBytes.Length);
            Array.Copy(networkAddressBytes, 0, buffer, entryOffset + networkAddressOffset, networkAddressBytes.Length);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV1 entry = response.ReferralEntries[0] as DfsReferralEntryV1;
            Assert.IsNotNull(entry);
            Assert.AreEqual(dfsPath, entry.DfsPath);
            Assert.AreEqual(networkAddress, entry.NetworkAddress);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV1EntrySizeExceedsBuffer_ThrowsArgumentException()
        {
            // Arrange
            // Header indicates one referral; entry declares a size that extends beyond the buffer.
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 256); // Size larger than remaining buffer

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV1DfsPathOffsetOutsideBuffer_ThrowsArgumentException()
        {
            // Arrange
            // Header indicates one referral; v1 entry has a DFSPathOffset that points beyond the buffer.
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 16);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, 1024); // DFSPathOffset far beyond buffer

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }
    }
}
