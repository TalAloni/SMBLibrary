using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV1Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            // Arrange - V1 uses ShareName (not DfsPath/NetworkAddress) per MS-DFSC 2.2.5.1
            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.VersionNumber = 1;
            entry.Size = 32;
            entry.ShareName = @"\\fs1\Public";

            // Assert
            Assert.AreEqual((ushort)1, entry.VersionNumber);
            Assert.AreEqual((ushort)32, entry.Size);
            Assert.AreEqual(@"\\fs1\Public", entry.ShareName);

            // And it should be usable via the abstract base type
            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }

        [TestMethod]
        public void ServerType_ShouldPreserveValue()
        {
            // Arrange - V1 has ServerType per MS-DFSC 2.2.4.1
            DfsReferralEntryV1 entry = new DfsReferralEntryV1();

            // Act
            entry.ServerType = DfsServerType.Root;

            // Assert
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
        }

        [TestMethod]
        public void ReferralEntryFlags_ShouldPreserveValue()
        {
            // Arrange - V1 has ReferralEntryFlags per MS-DFSC 2.2.4.1
            DfsReferralEntryV1 entry = new DfsReferralEntryV1();

            // Act
            entry.ReferralEntryFlags = DfsReferralEntryFlags.NameListReferral;

            // Assert
            Assert.AreEqual(DfsReferralEntryFlags.NameListReferral, entry.ReferralEntryFlags);
        }
    }
}
