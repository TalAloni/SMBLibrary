using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV4Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            // Arrange & Act
            DfsReferralEntryV4 entry = new DfsReferralEntryV4();
            entry.VersionNumber = 4;
            entry.Size = 64;
            entry.ServerType = DfsServerType.Root;
            entry.ReferralEntryFlags = DfsReferralEntryFlags.TargetSetBoundary;
            entry.TimeToLive = 1800;
            entry.DfsPath = @"\\contoso.com\Public";
            entry.NetworkAddress = @"\\fs4\Public";

            // Assert
            Assert.AreEqual((ushort)4, entry.VersionNumber);
            Assert.AreEqual((ushort)64, entry.Size);
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry.ReferralEntryFlags);
            Assert.AreEqual((uint)1800, entry.TimeToLive);
            Assert.AreEqual(@"\\contoso.com\Public", entry.DfsPath);
            Assert.AreEqual(@"\\fs4\Public", entry.NetworkAddress);
        }

        [TestMethod]
        public void IsTargetSetBoundary_WhenFlagSet_ReturnsTrue()
        {
            // Arrange - V4 supports TargetSetBoundary per MS-DFSC 2.2.4.4
            DfsReferralEntryV4 entry = new DfsReferralEntryV4();
            entry.ReferralEntryFlags = DfsReferralEntryFlags.TargetSetBoundary;

            // Assert
            Assert.IsTrue(entry.IsTargetSetBoundary);
        }

        [TestMethod]
        public void IsTargetSetBoundary_WhenFlagNotSet_ReturnsFalse()
        {
            // Arrange
            DfsReferralEntryV4 entry = new DfsReferralEntryV4();
            entry.ReferralEntryFlags = DfsReferralEntryFlags.None;

            // Assert
            Assert.IsFalse(entry.IsTargetSetBoundary);
        }

        [TestMethod]
        public void V4_InheritsV3Properties()
        {
            // Arrange - V4 extends V3
            DfsReferralEntryV4 entry = new DfsReferralEntryV4();
            entry.ReferralEntryFlags = DfsReferralEntryFlags.NameListReferral;

            // Assert - V4 should have V3's IsNameListReferral
            Assert.IsTrue(entry.IsNameListReferral);

            // V4 is usable via V3 base type
            DfsReferralEntryV3 asV3 = entry;
            Assert.IsNotNull(asV3);
        }
    }
}
