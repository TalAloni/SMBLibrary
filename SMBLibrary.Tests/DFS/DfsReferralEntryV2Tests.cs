using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

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
    }
}
