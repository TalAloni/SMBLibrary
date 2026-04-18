using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsReferralTransportTests
    {
        private class FakeDfsReferralTransport : IDfsReferralTransport
        {
            public NTStatus StatusToReturn;
            public byte[] BufferToReturn;
            public uint OutputCountToReturn;

            public NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount)
            {
                buffer = BufferToReturn;
                outputCount = OutputCountToReturn;
                return StatusToReturn;
            }
        }

        [TestMethod]
        public void TryGetReferrals_WhenConfigured_ReturnsExpectedStatusAndBuffer()
        {
            // Arrange
            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_SUCCESS;
            transport.BufferToReturn = new byte[] { 0x01, 0x02 };
            transport.OutputCountToReturn = 2;

            byte[] buffer;
            uint outputCount;

            // Act
            NTStatus status = transport.TryGetReferrals("server", "\\\\server\\dfs", 4096, out buffer, out outputCount);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, status);
            Assert.IsNotNull(buffer);
            Assert.AreEqual(2u, outputCount);
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(0x01, buffer[0]);
            Assert.AreEqual(0x02, buffer[1]);
        }
    }
}
