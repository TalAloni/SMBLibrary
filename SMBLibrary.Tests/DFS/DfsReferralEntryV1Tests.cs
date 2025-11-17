using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV1Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            // Arrange
            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.VersionNumber = 1;
            entry.Size = 32;
            entry.TimeToLive = 300;
            entry.DfsPath = @"\\\\contoso.com\\Public";
            entry.NetworkAddress = @"\\\\fs1\\Public";

            // Assert
            Assert.AreEqual((ushort)1, entry.VersionNumber);
            Assert.AreEqual((ushort)32, entry.Size);
            Assert.AreEqual((uint)300, entry.TimeToLive);
            Assert.AreEqual(@"\\\\contoso.com\\Public", entry.DfsPath);
            Assert.AreEqual(@"\\\\fs1\\Public", entry.NetworkAddress);

            // And it should be usable via the abstract base type
            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }
    }
}
