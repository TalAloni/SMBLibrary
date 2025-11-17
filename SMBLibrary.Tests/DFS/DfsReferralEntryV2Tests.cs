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
            entry.TimeToLive = 600;
            entry.ServerType = 1;
            entry.ReferralEntryFlags = 0x0002;
            entry.DfsPath = @"\\\\contoso.com\\Public";
            entry.DfsAlternatePath = @"\\\\contoso.com\\PublicAlt";
            entry.NetworkAddress = @"\\\\fs2\\Public";

            Assert.AreEqual((ushort)2, entry.VersionNumber);
            Assert.AreEqual((ushort)48, entry.Size);
            Assert.AreEqual((uint)600, entry.TimeToLive);
            Assert.AreEqual((ushort)1, entry.ServerType);
            Assert.AreEqual((ushort)0x0002, entry.ReferralEntryFlags);
            Assert.AreEqual(@"\\\\contoso.com\\Public", entry.DfsPath);
            Assert.AreEqual(@"\\\\contoso.com\\PublicAlt", entry.DfsAlternatePath);
            Assert.AreEqual(@"\\\\fs2\\Public", entry.NetworkAddress);

            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }
    }
}
