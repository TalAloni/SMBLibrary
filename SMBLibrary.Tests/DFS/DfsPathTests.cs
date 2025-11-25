using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;
using System;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsPathTests
    {
        #region Constructor and Parsing

        [TestMethod]
        public void Constructor_ValidUncPath_ParsesComponents()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share\folder\file.txt");

            // Assert
            Assert.AreEqual(4, dfsPath.PathComponents.Count);
            Assert.AreEqual("server", dfsPath.PathComponents[0]);
            Assert.AreEqual("share", dfsPath.PathComponents[1]);
            Assert.AreEqual("folder", dfsPath.PathComponents[2]);
            Assert.AreEqual("file.txt", dfsPath.PathComponents[3]);
        }

        [TestMethod]
        public void Constructor_LeadingSlashes_HandledCorrectly()
        {
            // Arrange & Act - forward slashes should also work
            var dfsPath = new DfsPath(@"//server/share/folder");

            // Assert
            Assert.AreEqual(3, dfsPath.PathComponents.Count);
            Assert.AreEqual("server", dfsPath.PathComponents[0]);
            Assert.AreEqual("share", dfsPath.PathComponents[1]);
            Assert.AreEqual("folder", dfsPath.PathComponents[2]);
        }

        [TestMethod]
        public void Constructor_MixedSlashes_NormalizesCorrectly()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server/share\folder");

            // Assert
            Assert.AreEqual(3, dfsPath.PathComponents.Count);
            Assert.AreEqual("server", dfsPath.PathComponents[0]);
            Assert.AreEqual("share", dfsPath.PathComponents[1]);
            Assert.AreEqual("folder", dfsPath.PathComponents[2]);
        }

        [TestMethod]
        public void Constructor_TrailingSlash_IgnoresEmptyComponent()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share\");

            // Assert
            Assert.AreEqual(2, dfsPath.PathComponents.Count);
            Assert.AreEqual("server", dfsPath.PathComponents[0]);
            Assert.AreEqual("share", dfsPath.PathComponents[1]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullPath_ThrowsArgumentNullException()
        {
            // Act
            var dfsPath = new DfsPath(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_EmptyPath_ThrowsArgumentException()
        {
            // Act
            var dfsPath = new DfsPath(string.Empty);
        }

        [TestMethod]
        public void Constructor_OnlySlashes_ReturnsEmptyComponents()
        {
            // Arrange & Act - degenerate path with only slashes
            var dfsPath = new DfsPath(@"\\\\");

            // Assert - should result in empty components list
            Assert.AreEqual(0, dfsPath.PathComponents.Count);
        }

        [TestMethod]
        public void Constructor_ForwardSlashesOnly_ReturnsEmptyComponents()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"//");

            // Assert
            Assert.AreEqual(0, dfsPath.PathComponents.Count);
        }

        #endregion

        #region HasOnlyOneComponent

        [TestMethod]
        public void HasOnlyOneComponent_SingleComponent_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server");

            // Assert
            Assert.IsTrue(dfsPath.HasOnlyOneComponent);
        }

        [TestMethod]
        public void HasOnlyOneComponent_MultipleComponents_ReturnsFalse()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share");

            // Assert
            Assert.IsFalse(dfsPath.HasOnlyOneComponent);
        }

        #endregion

        #region IsSysVolOrNetLogon

        [TestMethod]
        public void IsSysVolOrNetLogon_SysvolPath_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\domain.com\SYSVOL\folder");

            // Assert
            Assert.IsTrue(dfsPath.IsSysVolOrNetLogon);
        }

        [TestMethod]
        public void IsSysVolOrNetLogon_SysvolPathLowerCase_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\domain.com\sysvol\folder");

            // Assert
            Assert.IsTrue(dfsPath.IsSysVolOrNetLogon);
        }

        [TestMethod]
        public void IsSysVolOrNetLogon_NetlogonPath_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\domain.com\NETLOGON\scripts");

            // Assert
            Assert.IsTrue(dfsPath.IsSysVolOrNetLogon);
        }

        [TestMethod]
        public void IsSysVolOrNetLogon_NetlogonPathLowerCase_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\domain.com\netlogon\scripts");

            // Assert
            Assert.IsTrue(dfsPath.IsSysVolOrNetLogon);
        }

        [TestMethod]
        public void IsSysVolOrNetLogon_RegularShare_ReturnsFalse()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share\folder");

            // Assert
            Assert.IsFalse(dfsPath.IsSysVolOrNetLogon);
        }

        [TestMethod]
        public void IsSysVolOrNetLogon_SingleComponent_ReturnsFalse()
        {
            // Arrange & Act - No share component to check
            var dfsPath = new DfsPath(@"\\server");

            // Assert
            Assert.IsFalse(dfsPath.IsSysVolOrNetLogon);
        }

        #endregion

        #region IsIpc

        [TestMethod]
        public void IsIpc_IpcPath_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\IPC$");

            // Assert
            Assert.IsTrue(dfsPath.IsIpc);
        }

        [TestMethod]
        public void IsIpc_IpcPathLowerCase_ReturnsTrue()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\ipc$");

            // Assert
            Assert.IsTrue(dfsPath.IsIpc);
        }

        [TestMethod]
        public void IsIpc_RegularShare_ReturnsFalse()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share");

            // Assert
            Assert.IsFalse(dfsPath.IsIpc);
        }

        [TestMethod]
        public void IsIpc_SingleComponent_ReturnsFalse()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server");

            // Assert
            Assert.IsFalse(dfsPath.IsIpc);
        }

        #endregion

        #region ToUncPath

        [TestMethod]
        public void ToUncPath_ReturnsValidUncString()
        {
            // Arrange
            var dfsPath = new DfsPath(@"\\server\share\folder\file.txt");

            // Act
            string result = dfsPath.ToUncPath();

            // Assert
            Assert.AreEqual(@"\server\share\folder\file.txt", result);
        }

        [TestMethod]
        public void ToUncPath_SingleComponent_ReturnsValidUncString()
        {
            // Arrange
            var dfsPath = new DfsPath(@"\\server");

            // Act
            string result = dfsPath.ToUncPath();

            // Assert
            Assert.AreEqual(@"\server", result);
        }

        #endregion

        #region ReplacePrefix

        [TestMethod]
        public void ReplacePrefix_ValidPrefix_ReplacesCorrectly()
        {
            // Arrange
            var originalPath = new DfsPath(@"\\contoso.com\dfs\share\folder\file.txt");
            var newPrefix = new DfsPath(@"\\fileserver1\share");

            // Act - Replace "\\contoso.com\dfs\share" with "\\fileserver1\share"
            var result = originalPath.ReplacePrefix(@"\contoso.com\dfs\share", newPrefix);

            // Assert
            Assert.AreEqual(@"\fileserver1\share\folder\file.txt", result.ToUncPath());
        }

        [TestMethod]
        public void ReplacePrefix_PrefixMatchesEntirePath_ReturnsNewPrefix()
        {
            // Arrange
            var originalPath = new DfsPath(@"\\contoso.com\dfs\share");
            var newPrefix = new DfsPath(@"\\fileserver1\share");

            // Act
            var result = originalPath.ReplacePrefix(@"\contoso.com\dfs\share", newPrefix);

            // Assert
            Assert.AreEqual(@"\fileserver1\share", result.ToUncPath());
        }

        [TestMethod]
        public void ReplacePrefix_CaseInsensitive_ReplacesCorrectly()
        {
            // Arrange
            var originalPath = new DfsPath(@"\\CONTOSO.COM\DFS\Share\folder");
            var newPrefix = new DfsPath(@"\\fileserver1\share");

            // Act
            var result = originalPath.ReplacePrefix(@"\contoso.com\dfs\share", newPrefix);

            // Assert
            Assert.AreEqual(@"\fileserver1\share\folder", result.ToUncPath());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ReplacePrefix_PrefixNotMatching_ThrowsArgumentException()
        {
            // Arrange
            var originalPath = new DfsPath(@"\\server\share\folder");
            var newPrefix = new DfsPath(@"\\other\path");

            // Act
            originalPath.ReplacePrefix(@"\different\prefix", newPrefix);
        }

        #endregion

        #region ServerName and ShareName

        [TestMethod]
        public void ServerName_ReturnsFirstComponent()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share\folder");

            // Assert
            Assert.AreEqual("server", dfsPath.ServerName);
        }

        [TestMethod]
        public void ShareName_ReturnsSecondComponent()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server\share\folder");

            // Assert
            Assert.AreEqual("share", dfsPath.ShareName);
        }

        [TestMethod]
        public void ShareName_SingleComponent_ReturnsNull()
        {
            // Arrange & Act
            var dfsPath = new DfsPath(@"\\server");

            // Assert
            Assert.IsNull(dfsPath.ShareName);
        }

        #endregion
    }
}
