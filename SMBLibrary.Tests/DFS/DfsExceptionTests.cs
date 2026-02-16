using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    /// <summary>
    /// Unit tests for DfsException class for DFS resolution errors.
    /// </summary>
    [TestClass]
    public class DfsExceptionTests
    {
        [TestMethod]
        public void Ctor_WithMessage_SetsMessage()
        {
            // Arrange & Act
            var ex = new DfsException("DFS resolution failed");

            // Assert
            Assert.AreEqual("DFS resolution failed", ex.Message);
        }

        [TestMethod]
        public void Ctor_WithMessageAndInner_SetsMessageAndInnerException()
        {
            // Arrange
            var inner = new InvalidOperationException("inner");

            // Act
            var ex = new DfsException("DFS resolution failed", inner);

            // Assert
            Assert.AreEqual("DFS resolution failed", ex.Message);
            Assert.AreSame(inner, ex.InnerException);
        }

        [TestMethod]
        public void Ctor_WithMessageAndStatus_SetsMessageAndStatus()
        {
            // Arrange & Act
            var ex = new DfsException("DFS resolution failed", NTStatus.STATUS_PATH_NOT_COVERED);

            // Assert
            Assert.AreEqual("DFS resolution failed", ex.Message);
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, ex.Status);
        }

        [TestMethod]
        public void Ctor_WithMessageStatusAndPath_SetsAllProperties()
        {
            // Arrange & Act
            var ex = new DfsException("DFS resolution failed", NTStatus.STATUS_PATH_NOT_COVERED, @"\\domain\share\path");

            // Assert
            Assert.AreEqual("DFS resolution failed", ex.Message);
            Assert.AreEqual(NTStatus.STATUS_PATH_NOT_COVERED, ex.Status);
            Assert.AreEqual(@"\\domain\share\path", ex.Path);
        }

        [TestMethod]
        public void Status_DefaultIsSuccess()
        {
            // Arrange & Act
            var ex = new DfsException("test");

            // Assert
            Assert.AreEqual(NTStatus.STATUS_SUCCESS, ex.Status);
        }

        [TestMethod]
        public void Path_DefaultIsNull()
        {
            // Arrange & Act
            var ex = new DfsException("test");

            // Assert
            Assert.IsNull(ex.Path);
        }

        [TestMethod]
        public void DfsException_IsException()
        {
            // Arrange & Act
            var ex = new DfsException("test");

            // Assert
            Assert.IsInstanceOfType(ex, typeof(Exception));
        }
    }
}
