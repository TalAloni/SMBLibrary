using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryFlagsTests
    {
        [TestMethod]
        public void NameListReferral_HasCorrectValue()
        {
            // MS-DFSC 2.2.4.x: NameListReferral = 0x0002
            Assert.AreEqual((ushort)0x0002, (ushort)DfsReferralEntryFlags.NameListReferral);
        }

        [TestMethod]
        public void TargetSetBoundary_HasCorrectValue()
        {
            // MS-DFSC 2.2.4.4: TargetSetBoundary = 0x0004
            Assert.AreEqual((ushort)0x0004, (ushort)DfsReferralEntryFlags.TargetSetBoundary);
        }

        [TestMethod]
        public void CombinedFlags_CanBeParsed()
        {
            // Arrange
            ushort rawValue = 0x0006; // NameListReferral + TargetSetBoundary

            // Act
            DfsReferralEntryFlags flags = (DfsReferralEntryFlags)rawValue;

            // Assert
            Assert.IsTrue((flags & DfsReferralEntryFlags.NameListReferral) == DfsReferralEntryFlags.NameListReferral);
            Assert.IsTrue((flags & DfsReferralEntryFlags.TargetSetBoundary) == DfsReferralEntryFlags.TargetSetBoundary);
        }
    }
}
