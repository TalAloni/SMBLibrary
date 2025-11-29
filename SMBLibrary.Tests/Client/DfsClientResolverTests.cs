using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsClientResolverTests
    {
        private class FakeDfsReferralTransport : IDfsReferralTransport
        {
            public NTStatus StatusToReturn;
            public byte[] BufferToReturn;
            public uint OutputCountToReturn;
            public int CallCount;

            public NTStatus TryGetReferrals(string serverName, string dfsPath, uint maxOutputSize, out byte[] buffer, out uint outputCount)
            {
                CallCount++;
                buffer = BufferToReturn;
                outputCount = OutputCountToReturn;
                return StatusToReturn;
            }
        }

        [TestMethod]
        public void Resolve_WhenDfsDisabled_ReturnsNotApplicableAndOriginalPath()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions(); // Enabled is false by default
            string originalPath = @"\\server\\share\\path";
            IDfsClientResolver resolver = new DfsClientResolver();

            // Act
            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            // Assert
            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(originalPath, result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Resolve_WhenOptionsIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string originalPath = @"\\server\\share\\path";
            IDfsClientResolver resolver = new DfsClientResolver();

            // Act
            resolver.Resolve(null, originalPath);
        }

        [TestMethod]
        public void Resolve_WhenDfsEnabledButNotImplemented_ReturnsErrorAndOriginalPath()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;
            string originalPath = @"\\server\\share\\path";
            IDfsClientResolver resolver = new DfsClientResolver();

            // Act
            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            // Assert
            Assert.AreEqual(DfsResolutionStatus.Error, result.Status);
            Assert.AreEqual(originalPath, result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        [TestMethod]
        public void Resolve_WhenDfsEnabledAndTransportReturnsStatusFsDriverRequired_ReturnsNotApplicableAndOriginalPath()
        {
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;
            string originalPath = "\\\\server\\share\\path";

            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_FS_DRIVER_REQUIRED;

            IDfsClientResolver resolver = new DfsClientResolver(transport);

            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            Assert.AreEqual(DfsResolutionStatus.NotApplicable, result.Status);
            Assert.AreEqual(originalPath, result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        [TestMethod]
        public void Resolve_WhenDfsEnabledAndTransportReturnsErrorStatus_ReturnsErrorAndOriginalPath()
        {
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;
            string originalPath = "\\\\server\\share\\path";

            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_INVALID_PARAMETER;

            IDfsClientResolver resolver = new DfsClientResolver(transport);

            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            Assert.AreEqual(DfsResolutionStatus.Error, result.Status);
            Assert.AreEqual(originalPath, result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        [TestMethod]
        public void Resolve_WhenDfsEnabledAndTransportReturnsSuccessWithSingleV2Referral_ReturnsSuccessAndRewrittenPath()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            string dfsPath = "\\\\contoso.com\\Public";
            string originalPath = dfsPath + "\\folder\\file.txt";
            string networkAddress = "\\\\fs1\\Public";

            // Build a V2 DFSC buffer (V2 has proper offsets for strings)
            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int entryOffset = 8;
            int v2FixedLength = 22; // V2 fixed header size
            int dfsPathOffset = v2FixedLength;
            int dfsAlternatePathOffset = v2FixedLength + dfsPathBytes.Length;
            int networkAddressOffset = dfsAlternatePathOffset + dfsPathBytes.Length;
            int entrySize = networkAddressOffset + networkAddressBytes.Length;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);
            int totalSize = 8 + entrySize;
            byte[] buffer = new byte[totalSize];

            // Response header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, pathConsumed);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1); // NumberOfReferrals
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0); // ReferralHeaderFlags

            // V2 entry header
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 2); // VersionNumber
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 4, 0); // ServerType
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 6, 0); // ReferralEntryFlags
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 0); // Proximity
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 12, 300); // TimeToLive
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 16, (ushort)dfsPathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 18, (ushort)dfsAlternatePathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 20, (ushort)networkAddressOffset);

            // Strings
            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsPathOffset, dfsPathBytes.Length);
            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsAlternatePathOffset, dfsPathBytes.Length);
            Array.Copy(networkAddressBytes, 0, buffer, entryOffset + networkAddressOffset, networkAddressBytes.Length);

            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_SUCCESS;
            transport.BufferToReturn = buffer;
            transport.OutputCountToReturn = (uint)buffer.Length;

            IDfsClientResolver resolver = new DfsClientResolver(transport);

            // Act
            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            // Assert
            Assert.AreEqual(DfsResolutionStatus.Success, result.Status);
            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        // TODO: Add tests for multiple V2 referrals and empty referrals when needed
    }
}
