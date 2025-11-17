using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsReferralSelectorTests
    {
        [TestMethod]
        public void SelectResolvedPath_SingleV1Referral_RewritesPathUsingNetworkAddress()
        {
            string originalPath = "\\\\contoso.com\\Public\\folder\\file.txt";
            string dfsPath = "\\\\contoso.com\\Public";
            string networkAddress = "\\\\fs1\\Public";

            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.VersionNumber = 1;
            entry.DfsPath = dfsPath;
            entry.NetworkAddress = networkAddress;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entry);

            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", resolvedPath);
        }

        [TestMethod]
        public void SelectResolvedPath_WhenOriginalEqualsDfsPath_ReturnsNetworkAddress()
        {
            string originalPath = "\\\\contoso.com\\Public";
            string dfsPath = originalPath;
            string networkAddress = "\\\\fs1\\Public";

            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.VersionNumber = 1;
            entry.DfsPath = dfsPath;
            entry.NetworkAddress = networkAddress;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entry);

            Assert.AreEqual(networkAddress, resolvedPath);
        }

        [TestMethod]
        public void SelectResolvedPath_MultiReferral_PicksFirstUsableEntry()
        {
            string originalPath = "\\\\contoso.com\\Public\\folder\\file.txt";
            string dfsPath = "\\\\contoso.com\\Public";
            string firstNetworkAddress = "\\\\fs1\\Public";
            string secondNetworkAddress = "\\\\fs2\\Public";

            DfsReferralEntryV1 first = new DfsReferralEntryV1();
            first.VersionNumber = 1;
            first.DfsPath = dfsPath;
            first.NetworkAddress = firstNetworkAddress;

            DfsReferralEntryV1 second = new DfsReferralEntryV1();
            second.VersionNumber = 1;
            second.DfsPath = dfsPath;
            second.NetworkAddress = secondNetworkAddress;

            DfsReferralEntry[] entries = new DfsReferralEntry[] { first, second };

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entries);

            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", resolvedPath);
        }

        [TestMethod]
        public void SelectResolvedPath_MultiReferral_WhenNoUsableEntry_ReturnsNull()
        {
            string originalPath = "\\\\contoso.com\\Public\\folder\\file.txt";
            string dfsPath = "\\\\contoso.com\\Public";

            DfsReferralEntryV1 first = new DfsReferralEntryV1();
            first.VersionNumber = 1;
            first.DfsPath = dfsPath;
            first.NetworkAddress = null;

            DfsReferralEntryV1 second = new DfsReferralEntryV1();
            second.VersionNumber = 1;
            second.DfsPath = dfsPath;
            second.NetworkAddress = string.Empty;

            DfsReferralEntry[] entries = new DfsReferralEntry[] { first, second };

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entries);

            Assert.IsNull(resolvedPath);
        }
    }
}
