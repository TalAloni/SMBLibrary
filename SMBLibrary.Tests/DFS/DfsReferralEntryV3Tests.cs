using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV3Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            entry.VersionNumber = 3;
            entry.Size = 64;
            entry.TimeToLive = 900;
            entry.ServerType = 2;
            entry.ReferralEntryFlags = 0x0004;
            entry.DfsPath = @"\\\\contoso.com\\Public";
            entry.DfsAlternatePath = @"\\\\contoso.com\\PublicAltV3";
            entry.NetworkAddress = @"\\\\fs3\\Public";

            Assert.AreEqual((ushort)3, entry.VersionNumber);
            Assert.AreEqual((ushort)64, entry.Size);
            Assert.AreEqual((uint)900, entry.TimeToLive);
            Assert.AreEqual((ushort)2, entry.ServerType);
            Assert.AreEqual((ushort)0x0004, entry.ReferralEntryFlags);
            Assert.AreEqual(@"\\\\contoso.com\\Public", entry.DfsPath);
            Assert.AreEqual(@"\\\\contoso.com\\PublicAltV3", entry.DfsAlternatePath);
            Assert.AreEqual(@"\\\\fs3\\Public", entry.NetworkAddress);

            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }
    }
}
