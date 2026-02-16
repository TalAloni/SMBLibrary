using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsIoctlRequestBuilderTests
    {
        [TestMethod]
        public void CreateDfsReferralRequest_SetsCtlCodeFileIdAndFlags()
        {
            // Arrange
            string dfsPath = @"\\server\\namespace\\path";
            uint maxOutputResponse = 4096;

            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(dfsPath, maxOutputResponse);

            // Assert
            Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS, request.CtlCode);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, request.FileId.Persistent);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, request.FileId.Volatile);
            Assert.IsTrue(request.IsFSCtl);
            Assert.AreEqual(maxOutputResponse, request.MaxOutputResponse);
            Assert.IsNotNull(request.Input);
            Assert.IsTrue(request.Input.Length > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void CreateDfsReferralRequest_WhenPathIsNull_ThrowsArgumentNullException()
        {
            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(null, 4096);
        }

        [TestMethod]
        public void CreateDfsReferralRequest_SetsMaxReferralLevelToFour()
        {
            // Arrange
            string dfsPath = @"\\server\namespace";
            uint maxOutputResponse = 4096;

            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequest(dfsPath, maxOutputResponse);

            // Assert - Parse the input buffer to verify MaxReferralLevel
            // The first 2 bytes of the REQ_GET_DFS_REFERRAL buffer are MaxReferralLevel (LE)
            ushort maxReferralLevel = (ushort)(request.Input[0] | (request.Input[1] << 8));
            Assert.AreEqual((ushort)4, maxReferralLevel, "MaxReferralLevel should be 4 for maximum V4 referral support");
        }

        [TestMethod]
        public void CreateDfsReferralRequestEx_SetsCtlCodeFileIdAndFlags()
        {
            // Arrange
            string dfsPath = @"\\server\namespace\path";
            uint maxOutputResponse = 4096;

            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(dfsPath, null, maxOutputResponse);

            // Assert
            Assert.AreEqual((uint)IoControlCode.FSCTL_DFS_GET_REFERRALS_EX, request.CtlCode);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, request.FileId.Persistent);
            Assert.AreEqual(0xFFFFFFFFFFFFFFFFUL, request.FileId.Volatile);
            Assert.IsTrue(request.IsFSCtl);
            Assert.AreEqual(maxOutputResponse, request.MaxOutputResponse);
            Assert.IsNotNull(request.Input);
            Assert.IsTrue(request.Input.Length > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void CreateDfsReferralRequestEx_WhenPathIsNull_ThrowsArgumentNullException()
        {
            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(null, null, 4096);
        }

        [TestMethod]
        public void CreateDfsReferralRequestEx_WithSiteName_IncludesSiteNameInRequest()
        {
            // Arrange
            string dfsPath = @"\\server\namespace";
            string siteName = "Default-First-Site-Name";
            uint maxOutputResponse = 4096;

            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(dfsPath, siteName, maxOutputResponse);

            // Assert - Request should be larger when site name is included
            Assert.IsNotNull(request.Input);
            Assert.IsTrue(request.Input.Length > 0);
            // The RequestFlags field should indicate site name is present
            // RequestFlags is at offset 4 in REQ_GET_DFS_REFERRAL_EX (after MaxReferralLevel[2] and RequestFlags[2])
            ushort requestFlags = (ushort)(request.Input[2] | (request.Input[3] << 8));
            Assert.AreEqual((ushort)1, requestFlags, "RequestFlags should be 1 when SiteName is specified");
        }

        [TestMethod]
        public void CreateDfsReferralRequestEx_WithoutSiteName_HasZeroRequestFlags()
        {
            // Arrange
            string dfsPath = @"\\server\namespace";
            uint maxOutputResponse = 4096;

            // Act
            IOCtlRequest request = DfsIoctlRequestBuilder.CreateDfsReferralRequestEx(dfsPath, null, maxOutputResponse);

            // Assert
            Assert.IsNotNull(request.Input);
            // RequestFlags at offset 2-3 should be 0 when no site name
            ushort requestFlags = (ushort)(request.Input[2] | (request.Input[3] << 8));
            Assert.AreEqual((ushort)0, requestFlags, "RequestFlags should be 0 when no SiteName is specified");
        }
    }
}
