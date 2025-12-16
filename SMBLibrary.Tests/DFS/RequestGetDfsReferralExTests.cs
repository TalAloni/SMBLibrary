using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class RequestGetDfsReferralExTests
    {
        [TestMethod]
        public void ParseRequestGetDfsReferralEx()
        {
            byte[] buffer = new byte[]
            {
                0x04, 0x00, 0x01, 0x00, 0x56, 0x00, 0x00, 0x00, 0x22, 0x00, 0x5c, 0x00, 0x4c, 0x00, 0x41, 0x00,
                0x42, 0x00, 0x2e, 0x00, 0x4c, 0x00, 0x4f, 0x00, 0x43, 0x00, 0x41, 0x00, 0x4c, 0x00, 0x5c, 0x00,
                0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x73, 0x00, 0x00, 0x00, 0x30, 0x00, 0x44, 0x00,
                0x65, 0x00, 0x66, 0x00, 0x61, 0x00, 0x75, 0x00, 0x6c, 0x00, 0x74, 0x00, 0x2d, 0x00, 0x46, 0x00,
                0x69, 0x00, 0x72, 0x00, 0x73, 0x00, 0x74, 0x00, 0x2d, 0x00, 0x53, 0x00, 0x69, 0x00, 0x74, 0x00,
                0x65, 0x00, 0x2d, 0x00, 0x4e, 0x00, 0x61, 0x00, 0x6d, 0x00, 0x65, 0x00, 0x00, 0x00, 0x00
            };

            RequestGetDfsReferralEx request = new RequestGetDfsReferralEx(buffer);
            Assert.AreEqual(4, request.MaxReferralLevel);
            Assert.AreEqual(RequestGetDfsReferralExFlags.SiteName, request.Flags);
            Assert.AreEqual(@"\LAB.LOCAL\Files", request.RequestFileName);
            Assert.AreEqual("Default-First-Site-Name", request.SiteName);
        }

        [TestMethod]
        public void Parse_RequestGetDfsReferralEx_GetBytes()
        {
            RequestGetDfsReferralEx request = new RequestGetDfsReferralEx();
            request.MaxReferralLevel = 4;
            request.Flags = RequestGetDfsReferralExFlags.SiteName;
            request.RequestFileName = @"\LAB.LOCAL\Files";
            request.SiteName = @"Default-First-Site-Name";

            request = new RequestGetDfsReferralEx(request.GetBytes());
            Assert.AreEqual(4, request.MaxReferralLevel);
            Assert.AreEqual(RequestGetDfsReferralExFlags.SiteName, request.Flags);
            Assert.AreEqual(@"\LAB.LOCAL\Files", request.RequestFileName);
            Assert.AreEqual("Default-First-Site-Name", request.SiteName);
        }
    }
}
