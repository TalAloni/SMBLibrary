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
        public void ParseResponseGetDfsReferral_SingleReferralV2_ParsesHeaderFields()
        {
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 16);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 600);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV2 entry = response.ReferralEntries[0] as DfsReferralEntryV2;
            Assert.IsNotNull(entry);
            Assert.AreEqual((ushort)2, entry.VersionNumber);
            Assert.AreEqual((ushort)16, entry.Size);
            Assert.AreEqual((uint)600, entry.TimeToLive);
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
            Assert.AreEqual(DfsReferralEntryFlags.NameListReferral, entry.ReferralEntryFlags);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_SingleReferralV3_ParsesHeaderFields()
        {
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 16);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0004);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 900);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            Assert.IsNotNull(entry);
            Assert.AreEqual((ushort)3, entry.VersionNumber);
            Assert.AreEqual((ushort)16, entry.Size);
            Assert.AreEqual((uint)900, entry.TimeToLive);
            Assert.AreEqual((DfsServerType)2, entry.ServerType);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry.ReferralEntryFlags);
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
        public void ParseResponseGetDfsReferral_SingleReferralV2_ParsesStringFields()
        {
            string dfsPath = "\\\\contoso.com\\Public";
            string dfsAlternatePath = "\\\\contoso.com\\PublicAlt";
            string networkAddress = "\\\\fs2\\Public";

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] dfsAlternatePathBytes = System.Text.Encoding.Unicode.GetBytes(dfsAlternatePath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int entryOffset = 8;
            int entrySize = 18;
            int dfsPathOffset = entrySize;
            int dfsAlternatePathOffset = entrySize + dfsPathBytes.Length;
            int networkAddressOffset = entrySize + dfsPathBytes.Length + dfsAlternatePathBytes.Length;

            int totalSize = 8 + entrySize + dfsPathBytes.Length + dfsAlternatePathBytes.Length + networkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 600);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, (ushort)dfsPathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 14, (ushort)dfsAlternatePathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 16, (ushort)networkAddressOffset);

            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsPathOffset, dfsPathBytes.Length);
            Array.Copy(dfsAlternatePathBytes, 0, buffer, entryOffset + dfsAlternatePathOffset, dfsAlternatePathBytes.Length);
            Array.Copy(networkAddressBytes, 0, buffer, entryOffset + networkAddressOffset, networkAddressBytes.Length);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV2 entry = response.ReferralEntries[0] as DfsReferralEntryV2;
            Assert.IsNotNull(entry);
            Assert.AreEqual(dfsPath, entry.DfsPath);
            Assert.AreEqual(dfsAlternatePath, entry.DfsAlternatePath);
            Assert.AreEqual(networkAddress, entry.NetworkAddress);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_SingleReferralV3_ParsesStringFields()
        {
            string dfsPath = "\\\\contoso.com\\Public";
            string dfsAlternatePath = "\\\\contoso.com\\PublicAltV3";
            string networkAddress = "\\\\fs3\\Public";

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] dfsAlternatePathBytes = System.Text.Encoding.Unicode.GetBytes(dfsAlternatePath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int entryOffset = 8;
            int entrySize = 18;
            int dfsPathOffset = entrySize;
            int dfsAlternatePathOffset = entrySize + dfsPathBytes.Length;
            int networkAddressOffset = entrySize + dfsPathBytes.Length + dfsAlternatePathBytes.Length;

            int totalSize = 8 + entrySize + dfsPathBytes.Length + dfsAlternatePathBytes.Length + networkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0004);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 900);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, (ushort)dfsPathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 14, (ushort)dfsAlternatePathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 16, (ushort)networkAddressOffset);

            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsPathOffset, dfsPathBytes.Length);
            Array.Copy(dfsAlternatePathBytes, 0, buffer, entryOffset + dfsAlternatePathOffset, dfsAlternatePathBytes.Length);
            Array.Copy(networkAddressBytes, 0, buffer, entryOffset + networkAddressOffset, networkAddressBytes.Length);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            Assert.IsNotNull(entry);
            Assert.AreEqual(dfsPath, entry.DfsPath);
            Assert.AreEqual(dfsAlternatePath, entry.DfsAlternatePath);
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV2EntrySizeExceedsBuffer_ThrowsArgumentException()
        {
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 256);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV2DfsPathOffsetOutsideBuffer_ThrowsArgumentException()
        {
            byte[] buffer = new byte[8 + 18];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 18);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 600);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, 1024);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV3EntrySizeExceedsBuffer_ThrowsArgumentException()
        {
            byte[] buffer = new byte[8 + 16];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 256);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WhenV3DfsPathOffsetOutsideBuffer_ThrowsArgumentException()
        {
            byte[] buffer = new byte[8 + 18];
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 6);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            int entryOffset = 8;
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, 18);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 2);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 900);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, 1024);

            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);
        }

        #region V3 NameListReferral Tests

        [TestMethod]
        public void ParseResponseGetDfsReferral_V3NameListReferral_ParsesServiceSiteGuid()
        {
            // Arrange - V3 NameListReferral with ServiceSiteGuid
            // MS-DFSC 2.2.4.3: When NameListReferral flag is set, structure is different:
            // Offset 12-27: ServiceSiteGuid (16 bytes)
            // Offset 28-29: NumberOfExpandedNames
            // Offset 30-31: ExpandedNameOffset (from entry start)
            // String area: SpecialName + ExpandedNames

            string specialName = "contoso.com";
            string expandedName1 = "DC1.contoso.com";
            string expandedName2 = "DC2.contoso.com";

            byte[] specialNameBytes = System.Text.Encoding.Unicode.GetBytes(specialName + "\0");
            byte[] expandedName1Bytes = System.Text.Encoding.Unicode.GetBytes(expandedName1 + "\0");
            byte[] expandedName2Bytes = System.Text.Encoding.Unicode.GetBytes(expandedName2 + "\0");

            int entryOffset = 8;
            int entryHeaderSize = 32; // V3 NameListReferral header is 32 bytes
            int specialNameOffset = entryHeaderSize;
            int expandedNamesOffset = entryHeaderSize + specialNameBytes.Length;

            int entrySize = entryHeaderSize + specialNameBytes.Length + expandedName1Bytes.Length + expandedName2Bytes.Length;
            int totalSize = 8 + entrySize;
            byte[] buffer = new byte[totalSize];

            // Response header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 22); // PathConsumed
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);  // NumberOfReferrals
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);  // ReferralHeaderFlags

            // V3 entry header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3); // VersionNumber
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize); // Size
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1); // ServerType = Root
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002); // ReferralEntryFlags = NameListReferral
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 600); // TimeToLive

            // ServiceSiteGuid at offset 12 (16 bytes)
            Guid testGuid = new Guid("12345678-1234-1234-1234-123456789ABC");
            byte[] guidBytes = testGuid.ToByteArray();
            Array.Copy(guidBytes, 0, buffer, entryOffset + 12, 16);

            // NumberOfExpandedNames at offset 28
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 28, 2);

            // ExpandedNameOffset at offset 30 (offset from entry start to expanded names)
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 30, (ushort)expandedNamesOffset);

            // SpecialName (string area starts at offset 32)
            Array.Copy(specialNameBytes, 0, buffer, entryOffset + specialNameOffset, specialNameBytes.Length);

            // ExpandedNames
            Array.Copy(expandedName1Bytes, 0, buffer, entryOffset + expandedNamesOffset, expandedName1Bytes.Length);
            Array.Copy(expandedName2Bytes, 0, buffer, entryOffset + expandedNamesOffset + expandedName1Bytes.Length, expandedName2Bytes.Length);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            Assert.AreEqual((ushort)1, response.NumberOfReferrals);
            Assert.AreEqual(1, response.ReferralEntries.Count);

            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            Assert.IsNotNull(entry, "Entry should be DfsReferralEntryV3");
            Assert.IsTrue(entry.IsNameListReferral, "Entry should be a NameListReferral");
            Assert.AreEqual(testGuid, entry.ServiceSiteGuid, "ServiceSiteGuid should match");
            Assert.AreEqual(specialName, entry.SpecialName, "SpecialName should match");
            Assert.IsNotNull(entry.ExpandedNames, "ExpandedNames should not be null");
            Assert.AreEqual(2, entry.ExpandedNames.Count, "Should have 2 expanded names");
            Assert.AreEqual(expandedName1, entry.ExpandedNames[0], "First expanded name should match");
            Assert.AreEqual(expandedName2, entry.ExpandedNames[1], "Second expanded name should match");
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_V3NameListReferral_SingleExpandedName()
        {
            // Arrange - V3 NameListReferral with single DC
            string specialName = "test.local";
            string expandedName = "DC1.test.local";

            byte[] specialNameBytes = System.Text.Encoding.Unicode.GetBytes(specialName + "\0");
            byte[] expandedNameBytes = System.Text.Encoding.Unicode.GetBytes(expandedName + "\0");

            int entryOffset = 8;
            int entryHeaderSize = 32;
            int specialNameOffset = entryHeaderSize;
            int expandedNamesOffset = entryHeaderSize + specialNameBytes.Length;

            int entrySize = entryHeaderSize + specialNameBytes.Length + expandedNameBytes.Length;
            int totalSize = 8 + entrySize;
            byte[] buffer = new byte[totalSize];

            // Response header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 20);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            // V3 entry header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1); // Root
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002); // NameListReferral
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 300);

            // ServiceSiteGuid (all zeros)
            // NumberOfExpandedNames = 1
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 28, 1);
            // ExpandedNameOffset
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 30, (ushort)expandedNamesOffset);

            // Strings
            Array.Copy(specialNameBytes, 0, buffer, entryOffset + specialNameOffset, specialNameBytes.Length);
            Array.Copy(expandedNameBytes, 0, buffer, entryOffset + expandedNamesOffset, expandedNameBytes.Length);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            Assert.IsNotNull(entry);
            Assert.IsTrue(entry.IsNameListReferral);
            Assert.AreEqual(specialName, entry.SpecialName);
            Assert.AreEqual(1, entry.ExpandedNames.Count);
            Assert.AreEqual(expandedName, entry.ExpandedNames[0]);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_V4NameListReferral_ParsesCorrectly()
        {
            // Arrange - V4 NameListReferral (same structure as V3)
            string specialName = "corp.local";
            string expandedName = "DC1.corp.local";

            byte[] specialNameBytes = System.Text.Encoding.Unicode.GetBytes(specialName + "\0");
            byte[] expandedNameBytes = System.Text.Encoding.Unicode.GetBytes(expandedName + "\0");

            int entryOffset = 8;
            int entryHeaderSize = 32;
            int specialNameOffset = entryHeaderSize;
            int expandedNamesOffset = entryHeaderSize + specialNameBytes.Length;

            int entrySize = entryHeaderSize + specialNameBytes.Length + expandedNameBytes.Length;
            int totalSize = 8 + entrySize;
            byte[] buffer = new byte[totalSize];

            // Response header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 20);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            // V4 entry header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 4); // Version 4
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002); // NameListReferral
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 600);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 28, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 30, (ushort)expandedNamesOffset);

            Array.Copy(specialNameBytes, 0, buffer, entryOffset + specialNameOffset, specialNameBytes.Length);
            Array.Copy(expandedNameBytes, 0, buffer, entryOffset + expandedNamesOffset, expandedNameBytes.Length);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            DfsReferralEntryV4 entry = response.ReferralEntries[0] as DfsReferralEntryV4;
            Assert.IsNotNull(entry, "V4 entry should be DfsReferralEntryV4");
            Assert.IsTrue(entry.IsNameListReferral);
            Assert.AreEqual(specialName, entry.SpecialName);
            Assert.AreEqual(1, entry.ExpandedNames.Count);
        }

        [TestMethod]
        public void ParseResponseGetDfsReferral_V3NameListReferral_ZeroExpandedNames()
        {
            // Arrange - Edge case: NameListReferral with zero expanded names
            string specialName = "empty.local";

            byte[] specialNameBytes = System.Text.Encoding.Unicode.GetBytes(specialName + "\0");

            int entryOffset = 8;
            int entryHeaderSize = 32;
            int specialNameOffset = entryHeaderSize;

            int entrySize = entryHeaderSize + specialNameBytes.Length;
            int totalSize = 8 + entrySize;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, 24);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 3);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0x0002); // NameListReferral
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 300);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 28, 0); // Zero expanded names
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 30, (ushort)entryHeaderSize);

            Array.Copy(specialNameBytes, 0, buffer, entryOffset + specialNameOffset, specialNameBytes.Length);

            // Act
            ResponseGetDfsReferral response = new ResponseGetDfsReferral(buffer);

            // Assert
            DfsReferralEntryV3 entry = response.ReferralEntries[0] as DfsReferralEntryV3;
            Assert.IsNotNull(entry);
            Assert.IsTrue(entry.IsNameListReferral);
            Assert.AreEqual(specialName, entry.SpecialName);
            Assert.IsNotNull(entry.ExpandedNames);
            Assert.AreEqual(0, entry.ExpandedNames.Count);
        }

        #endregion
    }
}
