using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV2Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            DfsReferralEntryV2 entry = new DfsReferralEntryV2();
            entry.VersionNumber = 2;
            entry.Size = 48;
            entry.ServerType = DfsServerType.Root;
            entry.ReferralEntryFlags = DfsReferralEntryFlags.NameListReferral;
            entry.Proximity = 100;
            entry.TimeToLive = 600;
            entry.DfsPath = @"\\contoso.com\Public";
            entry.DfsAlternatePath = @"\\contoso.com\PublicAlt";
            entry.NetworkAddress = @"\\fs2\Public";

            Assert.AreEqual((ushort)2, entry.VersionNumber);
            Assert.AreEqual((ushort)48, entry.Size);
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
            Assert.AreEqual(DfsReferralEntryFlags.NameListReferral, entry.ReferralEntryFlags);
            Assert.AreEqual((uint)100, entry.Proximity);
            Assert.AreEqual((uint)600, entry.TimeToLive);
            Assert.AreEqual(@"\\contoso.com\Public", entry.DfsPath);
            Assert.AreEqual(@"\\contoso.com\PublicAlt", entry.DfsAlternatePath);
            Assert.AreEqual(@"\\fs2\Public", entry.NetworkAddress);

            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }

        [TestMethod]
        public void Proximity_ShouldPreserveValue()
        {
            // Arrange - V2 has Proximity per MS-DFSC 2.2.4.2
            DfsReferralEntryV2 entry = new DfsReferralEntryV2();

            // Act
            entry.Proximity = 0x12345678;

            // Assert
            Assert.AreEqual((uint)0x12345678, entry.Proximity);
        }

        [TestMethod]
        public void GetBytes_RoundTrip_PreservesAllFields()
        {
            // Arrange
            DfsReferralEntryV2 original = new DfsReferralEntryV2();
            original.ServerType = DfsServerType.NonRoot;
            original.ReferralEntryFlags = DfsReferralEntryFlags.None;
            original.Proximity = 100;
            original.TimeToLive = 600;
            original.DfsPath = @"\\domain\namespace";
            original.DfsAlternatePath = @"\\domain\namespace";
            original.NetworkAddress = @"\\server\share";

            // Act - serialize and parse back
            byte[] buffer = original.GetBytes();
            int offset = 0;
            DfsReferralEntryV2 parsed = new DfsReferralEntryV2(buffer, ref offset);

            // Assert
            Assert.AreEqual(original.VersionNumber, parsed.VersionNumber);
            Assert.AreEqual(original.ServerType, parsed.ServerType);
            Assert.AreEqual(original.ReferralEntryFlags, parsed.ReferralEntryFlags);
            Assert.AreEqual(original.Proximity, parsed.Proximity);
            Assert.AreEqual(original.TimeToLive, parsed.TimeToLive);
            Assert.AreEqual(original.DfsPath, parsed.DfsPath);
            Assert.AreEqual(original.DfsAlternatePath, parsed.DfsAlternatePath);
            Assert.AreEqual(original.NetworkAddress, parsed.NetworkAddress);
            Assert.AreEqual(buffer.Length, offset);
        }

        [TestMethod]
        public void Length_CalculatesCorrectly()
        {
            // Arrange - V2 FixedLength = 22
            DfsReferralEntryV2 entry = new DfsReferralEntryV2();
            entry.DfsPath = "a";           // 1 + null = 2 * 2 = 4 bytes
            entry.DfsAlternatePath = "b";  // 4 bytes
            entry.NetworkAddress = "c";    // 4 bytes

            // Assert: 22 + 4 + 4 + 4 = 34
            Assert.AreEqual(34, entry.Length);
        }
    }
}
