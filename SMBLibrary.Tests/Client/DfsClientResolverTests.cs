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
        public void Resolve_WhenDfsEnabledAndTransportReturnsSuccessWithSingleV1Referral_ReturnsSuccessAndRewrittenPath()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            string dfsPath = "\\\\contoso.com\\Public";
            string originalPath = dfsPath + "\\folder\\file.txt";
            string networkAddress = "\\\\fs1\\Public";

            // Build a minimal v1 DFSC buffer matching the DFSC tests
            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] networkAddressBytes = System.Text.Encoding.Unicode.GetBytes(networkAddress + "\0");

            int entryOffset = 8;
            int entrySize = 16;
            int dfsPathOffset = entrySize;
            int networkAddressOffset = entrySize + dfsPathBytes.Length;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);
            int totalSize = 8 + entrySize + dfsPathBytes.Length + networkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, pathConsumed);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 1);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 12, (ushort)dfsPathOffset);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset + 14, (ushort)networkAddressOffset);

            Array.Copy(dfsPathBytes, 0, buffer, entryOffset + dfsPathOffset, dfsPathBytes.Length);
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

        [TestMethod]
        public void Resolve_WhenDfsEnabledAndTransportReturnsSuccessWithMultipleV1Referrals_PicksFirstUsableEntry()
        {
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            string dfsPath = "\\\\contoso.com\\Public";
            string originalPath = dfsPath + "\\folder\\file.txt";
            string firstNetworkAddress = "\\\\fs1\\Public";
            string secondNetworkAddress = "\\\\fs2\\Public";

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            byte[] firstNetworkAddressBytes = System.Text.Encoding.Unicode.GetBytes(firstNetworkAddress + "\0");
            byte[] secondNetworkAddressBytes = System.Text.Encoding.Unicode.GetBytes(secondNetworkAddress + "\0");

            int entrySize = 16;
            int entryOffset1 = 8;
            int entryOffset2 = entryOffset1 + entrySize;

            int stringAreaOffset = entryOffset2 + entrySize;
            int dfsPathStart = stringAreaOffset;
            int firstNetworkStart = dfsPathStart + dfsPathBytes.Length;
            int secondNetworkStart = firstNetworkStart + firstNetworkAddressBytes.Length;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);
            int totalSize = secondNetworkStart + secondNetworkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, pathConsumed);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 2);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset1 + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 12, (ushort)(dfsPathStart - entryOffset1));
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 14, (ushort)(firstNetworkStart - entryOffset1));

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset2 + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 12, (ushort)(dfsPathStart - entryOffset2));
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 14, (ushort)(secondNetworkStart - entryOffset2));

            Array.Copy(dfsPathBytes, 0, buffer, dfsPathStart, dfsPathBytes.Length);
            Array.Copy(firstNetworkAddressBytes, 0, buffer, firstNetworkStart, firstNetworkAddressBytes.Length);
            Array.Copy(secondNetworkAddressBytes, 0, buffer, secondNetworkStart, secondNetworkAddressBytes.Length);

            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_SUCCESS;
            transport.BufferToReturn = buffer;
            transport.OutputCountToReturn = (uint)buffer.Length;

            IDfsClientResolver resolver = new DfsClientResolver(transport);

            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            Assert.AreEqual(DfsResolutionStatus.Success, result.Status);
            Assert.AreEqual("\\\\fs1\\Public\\folder\\file.txt", result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }

        [TestMethod]
        public void Resolve_WhenDfsEnabledAndTransportReturnsSuccessButNoUsableV1Referral_ReturnsErrorAndOriginalPath()
        {
            DfsClientOptions options = new DfsClientOptions();
            options.Enabled = true;

            string dfsPath = "\\\\contoso.com\\Public";
            string originalPath = dfsPath + "\\folder\\file.txt";

            byte[] dfsPathBytes = System.Text.Encoding.Unicode.GetBytes(dfsPath + "\0");
            // Empty network address strings (just null terminators)
            byte[] emptyNetworkAddressBytes = System.Text.Encoding.Unicode.GetBytes("\0");

            int entrySize = 16;
            int entryOffset1 = 8;
            int entryOffset2 = entryOffset1 + entrySize;

            int stringAreaOffset = entryOffset2 + entrySize;
            int dfsPathStart = stringAreaOffset;
            int firstNetworkStart = dfsPathStart + dfsPathBytes.Length;
            int secondNetworkStart = firstNetworkStart + emptyNetworkAddressBytes.Length;

            ushort pathConsumed = (ushort)(dfsPath.Length * 2);
            int totalSize = secondNetworkStart + emptyNetworkAddressBytes.Length;
            byte[] buffer = new byte[totalSize];

            Utilities.LittleEndianWriter.WriteUInt16(buffer, 0, pathConsumed);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, 2, 2);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, 4, 0);

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset1 + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 12, (ushort)(dfsPathStart - entryOffset1));
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset1 + 14, (ushort)(firstNetworkStart - entryOffset1));

            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 0, 1);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 2, (ushort)entrySize);
            Utilities.LittleEndianWriter.WriteUInt32(buffer, entryOffset2 + 8, 300);
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 12, (ushort)(dfsPathStart - entryOffset2));
            Utilities.LittleEndianWriter.WriteUInt16(buffer, entryOffset2 + 14, (ushort)(secondNetworkStart - entryOffset2));

            Array.Copy(dfsPathBytes, 0, buffer, dfsPathStart, dfsPathBytes.Length);
            Array.Copy(emptyNetworkAddressBytes, 0, buffer, firstNetworkStart, emptyNetworkAddressBytes.Length);
            Array.Copy(emptyNetworkAddressBytes, 0, buffer, secondNetworkStart, emptyNetworkAddressBytes.Length);

            FakeDfsReferralTransport transport = new FakeDfsReferralTransport();
            transport.StatusToReturn = NTStatus.STATUS_SUCCESS;
            transport.BufferToReturn = buffer;
            transport.OutputCountToReturn = (uint)buffer.Length;

            IDfsClientResolver resolver = new DfsClientResolver(transport);

            DfsResolutionResult result = resolver.Resolve(options, originalPath);

            Assert.AreEqual(DfsResolutionStatus.Error, result.Status);
            Assert.AreEqual(originalPath, result.ResolvedPath);
            Assert.AreEqual(originalPath, result.OriginalPath);
        }
    }
}
