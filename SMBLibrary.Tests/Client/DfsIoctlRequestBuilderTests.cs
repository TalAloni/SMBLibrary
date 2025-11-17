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
    }
}
