using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralHeaderFlagsTests
    {
        [TestMethod]
        public void ReferalServers_HasCorrectValue()
        {
            // MS-DFSC 2.2.4: R bit = 0x00000001
            Assert.AreEqual((uint)0x00000001, (uint)DfsReferralHeaderFlags.ReferalServers);
        }

        [TestMethod]
        public void StorageServers_HasCorrectValue()
        {
            // MS-DFSC 2.2.4: S bit = 0x00000002
            Assert.AreEqual((uint)0x00000002, (uint)DfsReferralHeaderFlags.StorageServers);
        }

        [TestMethod]
        public void TargetFailback_HasCorrectValue()
        {
            // MS-DFSC 2.2.4: T bit = 0x00000004
            Assert.AreEqual((uint)0x00000004, (uint)DfsReferralHeaderFlags.TargetFailback);
        }

        [TestMethod]
        public void CombinedFlags_CanBeParsed()
        {
            // Arrange
            uint rawValue = 0x00000007; // R + S + T

            // Act
            DfsReferralHeaderFlags flags = (DfsReferralHeaderFlags)rawValue;

            // Assert
            Assert.IsTrue((flags & DfsReferralHeaderFlags.ReferalServers) == DfsReferralHeaderFlags.ReferalServers);
            Assert.IsTrue((flags & DfsReferralHeaderFlags.StorageServers) == DfsReferralHeaderFlags.StorageServers);
            Assert.IsTrue((flags & DfsReferralHeaderFlags.TargetFailback) == DfsReferralHeaderFlags.TargetFailback);
        }
    }
}
