using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Unit tests for DfsRequestType enum per MS-DFSC referral request types.
    /// </summary>
    [TestClass]
    public class DfsRequestTypeTests
    {
        [TestMethod]
        public void DomainReferral_HasCorrectValue()
        {
            // Per MS-DFSC: Domain referral request (\\DomainName)
            Assert.AreEqual(0, (int)DfsRequestType.DomainReferral);
        }

        [TestMethod]
        public void DcReferral_HasCorrectValue()
        {
            // Per MS-DFSC: DC referral request (domain controller list)
            Assert.AreEqual(1, (int)DfsRequestType.DcReferral);
        }

        [TestMethod]
        public void RootReferral_HasCorrectValue()
        {
            // Per MS-DFSC: Root referral request (\\Server\Share)
            Assert.AreEqual(2, (int)DfsRequestType.RootReferral);
        }

        [TestMethod]
        public void SysvolReferral_HasCorrectValue()
        {
            // Per MS-DFSC: SYSVOL/NETLOGON referral request
            Assert.AreEqual(3, (int)DfsRequestType.SysvolReferral);
        }

        [TestMethod]
        public void LinkReferral_HasCorrectValue()
        {
            // Per MS-DFSC: Link referral request (DFS folder link)
            Assert.AreEqual(4, (int)DfsRequestType.LinkReferral);
        }

        [TestMethod]
        public void AllValues_AreParseable()
        {
            // Verify all enum values can be cast from integers
            Assert.AreEqual(DfsRequestType.DomainReferral, (DfsRequestType)0);
            Assert.AreEqual(DfsRequestType.DcReferral, (DfsRequestType)1);
            Assert.AreEqual(DfsRequestType.RootReferral, (DfsRequestType)2);
            Assert.AreEqual(DfsRequestType.SysvolReferral, (DfsRequestType)3);
            Assert.AreEqual(DfsRequestType.LinkReferral, (DfsRequestType)4);
        }
    }
}
