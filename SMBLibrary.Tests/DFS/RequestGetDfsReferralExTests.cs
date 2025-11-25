using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using Utilities;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class RequestGetDfsReferralExTests
    {
        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void Ctor_NullBuffer_ThrowsArgumentNullException()
        {
            // Act
            new RequestGetDfsReferralEx(null);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Ctor_BufferTooSmall_ThrowsArgumentException()
        {
            // Arrange - buffer smaller than 8-byte header
            byte[] smallBuffer = new byte[4];

            // Act
            new RequestGetDfsReferralEx(smallBuffer);
        }

        [TestMethod]
        public void GetBytes_WithoutSiteName_ReturnsCorrectBuffer()
        {
            // Arrange
            RequestGetDfsReferralEx request = new RequestGetDfsReferralEx();
            request.MaxReferralLevel = 4;
            request.RequestFileName = @"\contoso.com\dfs";

            // Act
            byte[] buffer = request.GetBytes();

            // Assert - Header is 8 bytes, RequestFileName is Unicode null-terminated
            Assert.IsTrue(buffer.Length >= 8);
            Assert.AreEqual((ushort)4, LittleEndianConverter.ToUInt16(buffer, 0)); // MaxReferralLevel
            Assert.AreEqual((ushort)0, LittleEndianConverter.ToUInt16(buffer, 2)); // RequestFlags (no SiteName)
        }

        [TestMethod]
        public void GetBytes_WithSiteName_SetsRequestFlag()
        {
            // Arrange
            RequestGetDfsReferralEx request = new RequestGetDfsReferralEx();
            request.MaxReferralLevel = 4;
            request.RequestFileName = @"\contoso.com\dfs";
            request.SiteName = "Default-First-Site-Name";

            // Act
            byte[] buffer = request.GetBytes();

            // Assert
            Assert.AreEqual((ushort)0x0001, LittleEndianConverter.ToUInt16(buffer, 2)); // SiteName flag set
        }

        [TestMethod]
        public void Ctor_FromBuffer_ParsesCorrectly()
        {
            // Arrange - Create a request and serialize it
            RequestGetDfsReferralEx original = new RequestGetDfsReferralEx();
            original.MaxReferralLevel = 4;
            original.RequestFileName = @"\contoso.com\dfs";
            byte[] buffer = original.GetBytes();

            // Act - Parse it back
            RequestGetDfsReferralEx parsed = new RequestGetDfsReferralEx(buffer);

            // Assert
            Assert.AreEqual(original.MaxReferralLevel, parsed.MaxReferralLevel);
            Assert.AreEqual(original.RequestFileName, parsed.RequestFileName);
        }

        [TestMethod]
        public void Ctor_FromBufferWithSiteName_ParsesSiteName()
        {
            // Arrange
            RequestGetDfsReferralEx original = new RequestGetDfsReferralEx();
            original.MaxReferralLevel = 4;
            original.RequestFileName = @"\contoso.com\dfs";
            original.SiteName = "TestSite";
            byte[] buffer = original.GetBytes();

            // Act
            RequestGetDfsReferralEx parsed = new RequestGetDfsReferralEx(buffer);

            // Assert
            Assert.AreEqual("TestSite", parsed.SiteName);
        }
    }
}
