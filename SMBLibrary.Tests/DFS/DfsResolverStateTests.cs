using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Unit tests for DfsResolverState class that tracks state through the
    /// 14-step DFS resolution algorithm per MS-DFSC.
    /// </summary>
    [TestClass]
    public class DfsResolverStateTests
    {
        [TestMethod]
        public void Ctor_WithOriginalPath_SetsOriginalPath()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share\folder", "ctx");

            // Assert
            Assert.AreEqual(@"\\domain.com\share\folder", state.OriginalPath);
        }

        [TestMethod]
        public void Ctor_WithOriginalPath_SetsCurrentPathToOriginal()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share\folder", "ctx");

            // Assert
            Assert.AreEqual(@"\\domain.com\share\folder", state.CurrentPath);
        }

        [TestMethod]
        public void Ctor_WithContext_SetsContext()
        {
            // Arrange & Act
            var context = new object();
            var state = new DfsResolverState<object>(@"\\domain.com\share", context);

            // Assert
            Assert.AreSame(context, state.Context);
        }

        [TestMethod]
        public void CurrentPath_CanBeUpdated()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share\folder", "ctx");

            // Act
            state.CurrentPath = @"\\server1\share\folder";

            // Assert
            Assert.AreEqual(@"\\server1\share\folder", state.CurrentPath);
            Assert.AreEqual(@"\\domain.com\share\folder", state.OriginalPath); // unchanged
        }

        [TestMethod]
        public void RequestType_DefaultIsNone()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Assert
            Assert.IsFalse(state.RequestType.HasValue);
        }

        [TestMethod]
        public void RequestType_CanBeSet()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Act
            state.RequestType = DfsRequestType.RootReferral;

            // Assert
            Assert.AreEqual(DfsRequestType.RootReferral, state.RequestType);
        }

        [TestMethod]
        public void IsComplete_DefaultIsFalse()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Assert
            Assert.IsFalse(state.IsComplete);
        }

        [TestMethod]
        public void IsComplete_CanBeSetToTrue()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Act
            state.IsComplete = true;

            // Assert
            Assert.IsTrue(state.IsComplete);
        }

        [TestMethod]
        public void IsDfsPath_DefaultIsFalse()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Assert
            Assert.IsFalse(state.IsDfsPath);
        }

        [TestMethod]
        public void IsDfsPath_CanBeSetToTrue()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Act
            state.IsDfsPath = true;

            // Assert
            Assert.IsTrue(state.IsDfsPath);
        }

        [TestMethod]
        public void CachedEntry_DefaultIsNull()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Assert
            Assert.IsNull(state.CachedEntry);
        }

        [TestMethod]
        public void CachedEntry_CanBeSet()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");
            var entry = new ReferralCacheEntry(@"\\domain.com\share");

            // Act
            state.CachedEntry = entry;

            // Assert
            Assert.AreSame(entry, state.CachedEntry);
        }

        [TestMethod]
        public void LastStatus_DefaultIsSuccess()
        {
            // Arrange & Act
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, state.LastStatus);
        }

        [TestMethod]
        public void LastStatus_CanBeSet()
        {
            // Arrange
            var state = new DfsResolverState<string>(@"\\domain.com\share", "ctx");

            // Act
            state.LastStatus = NTStatus.STATUS_PATH_NOT_COVERED;

            // Assert
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, state.LastStatus);
        }
    }
}
