using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Unit tests for DFS event args classes used for observability.
    /// </summary>
    [TestClass]
    public class DfsEventsTests
    {
        #region DfsResolutionStartedEventArgs

        [TestMethod]
        public void DfsResolutionStartedEventArgs_Ctor_SetsPath()
        {
            // Arrange & Act
            var args = new DfsResolutionStartedEventArgs(@"\\domain\share\folder");

            // Assert
            Assert.AreEqual(@"\\domain\share\folder", args.Path);
        }

        [TestMethod]
        public void DfsResolutionStartedEventArgs_IsEventArgs()
        {
            // Arrange & Act
            var args = new DfsResolutionStartedEventArgs(@"\\domain\share");

            // Assert
            Assert.IsInstanceOfType(args, typeof(EventArgs));
        }

        #endregion

        #region DfsReferralRequestedEventArgs

        [TestMethod]
        public void DfsReferralRequestedEventArgs_Ctor_SetsProperties()
        {
            // Arrange & Act
            var args = new DfsReferralRequestedEventArgs(
                @"\\domain\share",
                DfsRequestType.RootReferral,
                "server.domain.com");

            // Assert
            Assert.AreEqual(@"\\domain\share", args.Path);
            Assert.AreEqual(DfsRequestType.RootReferral, args.RequestType);
            Assert.AreEqual("server.domain.com", args.TargetServer);
        }

        [TestMethod]
        public void DfsReferralRequestedEventArgs_TargetServer_CanBeNull()
        {
            // Arrange & Act
            var args = new DfsReferralRequestedEventArgs(
                @"\\domain\share",
                DfsRequestType.DomainReferral,
                null);

            // Assert
            Assert.IsNull(args.TargetServer);
        }

        #endregion

        #region DfsReferralReceivedEventArgs

        [TestMethod]
        public void DfsReferralReceivedEventArgs_Ctor_SetsProperties()
        {
            // Arrange & Act
            var args = new DfsReferralReceivedEventArgs(
                @"\\domain\share",
                NTStatus.STATUS_SUCCESS,
                3,
                300);

            // Assert
            Assert.AreEqual(@"\\domain\share", args.Path);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, args.Status);
            Assert.AreEqual(3, args.ReferralCount);
            Assert.AreEqual(300, args.TtlSeconds);
        }

        [TestMethod]
        public void DfsReferralReceivedEventArgs_WithError_HasZeroReferrals()
        {
            // Arrange & Act
            var args = new DfsReferralReceivedEventArgs(
                @"\\domain\share",
                NTStatus.STATUS_NOT_FOUND,
                0,
                0);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_NOT_FOUND, args.Status);
            Assert.AreEqual(0, args.ReferralCount);
        }

        #endregion

        #region DfsResolutionCompletedEventArgs

        [TestMethod]
        public void DfsResolutionCompletedEventArgs_Ctor_SetsProperties()
        {
            // Arrange & Act
            var args = new DfsResolutionCompletedEventArgs(
                @"\\domain\share\folder",
                @"\\server1\share\folder",
                NTStatus.STATUS_SUCCESS,
                true);

            // Assert
            Assert.AreEqual(@"\\domain\share\folder", args.OriginalPath);
            Assert.AreEqual(@"\\server1\share\folder", args.ResolvedPath);
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, args.Status);
            Assert.IsTrue(args.WasDfsPath);
        }

        [TestMethod]
        public void DfsResolutionCompletedEventArgs_NonDfsPath_SetsWasDfsFalse()
        {
            // Arrange & Act
            var args = new DfsResolutionCompletedEventArgs(
                @"\\server\share",
                @"\\server\share",
                NTStatus.STATUS_SUCCESS,
                false);

            // Assert
            Assert.IsFalse(args.WasDfsPath);
            Assert.AreEqual(args.OriginalPath, args.ResolvedPath);
        }

        [TestMethod]
        public void DfsResolutionCompletedEventArgs_WithError_HasOriginalAsResolved()
        {
            // Arrange & Act
            var args = new DfsResolutionCompletedEventArgs(
                @"\\domain\share",
                @"\\domain\share",
                NTStatus.STATUS_PATH_NOT_COVERED,
                true);

            // Assert
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, args.Status);
        }

        #endregion
    }
}
