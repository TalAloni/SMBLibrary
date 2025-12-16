using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class TargetSetEntryTests
    {
        [TestMethod]
        public void Ctor_WithTargetPath_SetsTargetPath()
        {
            // Arrange & Act
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Assert
            Assert.AreEqual(@"\\server\share", entry.TargetPath);
        }

        [TestMethod]
        public void Priority_Default_IsZero()
        {
            // Arrange & Act
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Assert
            Assert.AreEqual(0, entry.Priority);
        }

        [TestMethod]
        public void Priority_SetValue_ReturnsCorrectValue()
        {
            // Arrange
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Act
            entry.Priority = 5;

            // Assert
            Assert.AreEqual(5, entry.Priority);
        }

        [TestMethod]
        public void IsTargetSetBoundary_Default_IsFalse()
        {
            // Arrange & Act
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Assert
            Assert.IsFalse(entry.IsTargetSetBoundary);
        }

        [TestMethod]
        public void IsTargetSetBoundary_SetTrue_ReturnsTrue()
        {
            // Arrange
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Act
            entry.IsTargetSetBoundary = true;

            // Assert
            Assert.IsTrue(entry.IsTargetSetBoundary);
        }

        [TestMethod]
        public void ServerType_Default_IsNonRoot()
        {
            // Arrange & Act
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Assert
            Assert.AreEqual(DfsServerType.NonRoot, entry.ServerType);
        }

        [TestMethod]
        public void ServerType_SetRoot_ReturnsRoot()
        {
            // Arrange
            TargetSetEntry entry = new TargetSetEntry(@"\\server\share");

            // Act
            entry.ServerType = DfsServerType.Root;

            // Assert
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
        }
    }
}
