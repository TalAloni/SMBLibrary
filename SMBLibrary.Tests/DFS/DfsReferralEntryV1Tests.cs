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

        [TestMethod]
        public void GetBytes_RoundTrip_PreservesAllFields()
        {
            // Arrange
            DfsReferralEntryV1 original = new DfsReferralEntryV1();
            original.ServerType = DfsServerType.Root;
            original.ReferralEntryFlags = DfsReferralEntryFlags.None;
            original.ShareName = @"\\server\share";

            // Act - serialize and parse back
            byte[] buffer = original.GetBytes();
            int offset = 0;
            DfsReferralEntryV1 parsed = new DfsReferralEntryV1(buffer, ref offset);

            // Assert
            Assert.AreEqual(original.VersionNumber, parsed.VersionNumber);
            Assert.AreEqual(original.ServerType, parsed.ServerType);
            Assert.AreEqual(original.ReferralEntryFlags, parsed.ReferralEntryFlags);
            Assert.AreEqual(original.ShareName, parsed.ShareName);
            Assert.AreEqual(buffer.Length, offset); // Consumed entire buffer
        }

        [TestMethod]
        public void Length_CalculatesCorrectly()
        {
            // Arrange - V1 FixedLength = 8, plus ShareName in UTF-16 with null terminator
            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.ShareName = "test"; // 4 chars + null = 5 * 2 = 10 bytes

            // Assert: 8 + 10 = 18
            Assert.AreEqual(18, entry.Length);
        }
    }
}
