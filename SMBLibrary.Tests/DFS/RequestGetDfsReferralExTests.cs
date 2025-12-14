using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;
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

        /// <summary>
        /// Test parsing a real REQ_GET_DFS_REFERRAL_EX captured from Windows 11 client.
        /// This validates the structure includes RequestFileNameLength and SiteNameLength fields.
        /// Captured via Wireshark from: dir \\LAB.LOCAL\Files\Sales
        /// </summary>
        [TestMethod]
        public void Ctor_FromCapturedBuffer_ParsesCorrectly()
        {
            // Arrange - Real captured REQ_GET_DFS_REFERRAL_EX from Windows 11
            // Structure per MS-DFSC 2.2.3:
            //   MaxReferralLevel: 2 bytes
            //   RequestFlags: 2 bytes
            //   RequestDataLength: 4 bytes
            //   RequestData (per MS-DFSC 2.2.3.1):
            //     RequestFileNameLength: 2 bytes (0x22 = 34)
            //     RequestFileName: variable, Unicode null-terminated
            //     SiteNameLength: 2 bytes (0x30 = 48) - only if SiteName flag set
            //     SiteName: variable, Unicode null-terminated
            byte[] capturedBuffer = new byte[]
            {
                0x04, 0x00,                         // MaxReferralLevel = 4
                0x01, 0x00,                         // RequestFlags = 1 (SiteName present)
                0x56, 0x00, 0x00, 0x00,             // RequestDataLength = 86 bytes
                // RequestData starts here:
                0x22, 0x00,                         // RequestFileNameLength = 34 bytes
                0x5C, 0x00, 0x4C, 0x00, 0x41, 0x00, 0x42, 0x00, 0x2E, 0x00, // \LAB.
                0x4C, 0x00, 0x4F, 0x00, 0x43, 0x00, 0x41, 0x00, 0x4C, 0x00, // LOCAL
                0x5C, 0x00, 0x46, 0x00, 0x69, 0x00, 0x6C, 0x00, 0x65, 0x00, // \File
                0x73, 0x00, 0x00, 0x00,             // s + null terminator
                0x30, 0x00,                         // SiteNameLength = 48 bytes
                0x44, 0x00, 0x65, 0x00, 0x66, 0x00, 0x61, 0x00, 0x75, 0x00, // Defau
                0x6C, 0x00, 0x74, 0x00, 0x2D, 0x00, 0x46, 0x00, 0x69, 0x00, // lt-Fi
                0x72, 0x00, 0x73, 0x00, 0x74, 0x00, 0x2D, 0x00, 0x53, 0x00, // rst-S
                0x69, 0x00, 0x74, 0x00, 0x65, 0x00, 0x2D, 0x00, 0x4E, 0x00, // ite-N
                0x61, 0x00, 0x6D, 0x00, 0x65, 0x00, 0x00, 0x00,             // ame + null
                0x00                                // padding
            };

            // Act
            RequestGetDfsReferralEx parsed = new RequestGetDfsReferralEx(capturedBuffer);

            // Assert
            Assert.AreEqual((ushort)4, parsed.MaxReferralLevel);
            Assert.AreEqual(@"\LAB.LOCAL\Files", parsed.RequestFileName);
            Assert.AreEqual("Default-First-Site-Name", parsed.SiteName);
        }

        /// <summary>
        /// Test that GetBytes produces a buffer that matches the wire format with length fields.
        /// </summary>
        [TestMethod]
        public void GetBytes_WithSiteName_IncludesLengthFields()
        {
            // Arrange
            RequestGetDfsReferralEx request = new RequestGetDfsReferralEx();
            request.MaxReferralLevel = 4;
            request.RequestFileName = @"\LAB.LOCAL\Files";
            request.SiteName = "Default-First-Site-Name";

            // Act
            byte[] buffer = request.GetBytes();

            // Assert - Verify structure includes length fields per MS-DFSC 2.2.3.1
            // Header: MaxReferralLevel(2) + RequestFlags(2) + RequestDataLength(4) = 8 bytes
            Assert.IsTrue(buffer.Length >= 8, "Buffer too small for header");
            
            // Check header
            Assert.AreEqual((ushort)4, LittleEndianConverter.ToUInt16(buffer, 0)); // MaxReferralLevel
            Assert.AreEqual((ushort)1, LittleEndianConverter.ToUInt16(buffer, 2)); // RequestFlags (SiteName=1)
            
            // RequestData at offset 8 should start with RequestFileNameLength
            ushort requestFileNameLength = LittleEndianConverter.ToUInt16(buffer, 8);
            Assert.AreEqual((ushort)34, requestFileNameLength, "RequestFileNameLength should be 34 bytes");
        }
    }
}
