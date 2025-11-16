using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsClientResolverTests
    {
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
        }
    }
}
