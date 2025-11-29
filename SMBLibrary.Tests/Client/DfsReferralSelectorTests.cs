using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsReferralSelectorTests
    {
        [TestMethod]
        public void SelectResolvedPath_SingleV1Referral_RewritesPathUsingShareName()
        {
            // V1 uses ShareName instead of NetworkAddress
            string originalPath = "\\\\contoso.com\\Public\\folder\\file.txt";
            string dfsPath = "\\\\contoso.com\\Public";
            string shareName = "\\\\fs1\\Public";

            DfsReferralEntryV1 entry = new DfsReferralEntryV1();
            entry.ShareName = shareName;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entry);

            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", resolvedPath);
        }

        [TestMethod]
        public void SelectResolvedPath_V2Referral_RewritesPathUsingNetworkAddress()
        {
            string originalPath = "\\\\contoso.com\\Public\\folder\\file.txt";
            string dfsPath = "\\\\contoso.com\\Public";
            string networkAddress = "\\\\fs1\\Public";

            DfsReferralEntryV2 entry = new DfsReferralEntryV2();
            entry.DfsPath = dfsPath;
            entry.DfsAlternatePath = dfsPath;
            entry.NetworkAddress = networkAddress;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entry);

            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", resolvedPath);
        }

        [TestMethod]
        public void SelectResolvedPath_V3Referral_RewritesPathUsingNetworkAddress()
        {
            string originalPath = "\\\\contoso.com\\Public";
            string dfsPath = originalPath;
            string networkAddress = "\\\\fs1\\Public";

            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            entry.DfsPath = dfsPath;
            entry.DfsAlternatePath = dfsPath;
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

            DfsReferralEntryV2 first = new DfsReferralEntryV2();
            first.DfsPath = dfsPath;
            first.DfsAlternatePath = dfsPath;
            first.NetworkAddress = firstNetworkAddress;

            DfsReferralEntryV2 second = new DfsReferralEntryV2();
            second.DfsPath = dfsPath;
            second.DfsAlternatePath = dfsPath;
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

            DfsReferralEntryV2 first = new DfsReferralEntryV2();
            first.DfsPath = dfsPath;
            first.DfsAlternatePath = dfsPath;
            first.NetworkAddress = null;

            DfsReferralEntryV2 second = new DfsReferralEntryV2();
            second.DfsPath = dfsPath;
            second.DfsAlternatePath = dfsPath;
            second.NetworkAddress = string.Empty;

            DfsReferralEntry[] entries = new DfsReferralEntry[] { first, second };

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);

            string resolvedPath = DfsReferralSelector.SelectResolvedPath(originalPath, pathConsumed, entries);

            Assert.IsNull(resolvedPath);
        }
    }
}
