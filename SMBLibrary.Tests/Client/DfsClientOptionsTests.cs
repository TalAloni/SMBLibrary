using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.Client
{
    [TestClass]
    public class DfsClientOptionsTests
    {
        [TestMethod]
        public void Ctor_Default_DisablesDfs()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert
            Assert.IsFalse(options.Enabled);
        }

        [TestMethod]
        public void When_EnabledIsSetToTrue_ShouldReflectValue()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions();

            // Act
            options.Enabled = true;

            // Assert
            Assert.IsTrue(options.Enabled);
        }

        [TestMethod]
        public void Ctor_Default_DisablesDomainCache()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert
            Assert.IsFalse(options.EnableDomainCache);
        }

        [TestMethod]
        public void Ctor_Default_DisablesFullResolution()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert
            Assert.IsFalse(options.EnableFullResolution);
        }

        [TestMethod]
        public void Ctor_Default_DisablesCrossServerSessions()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert
            Assert.IsFalse(options.EnableCrossServerSessions);
        }

        [TestMethod]
        public void Ctor_Default_SetsReferralCacheTtlTo300Seconds()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert - Default TTL of 5 minutes (300 seconds)
            Assert.AreEqual(300, options.ReferralCacheTtlSeconds);
        }

        [TestMethod]
        public void Ctor_Default_SetsDomainCacheTtlTo300Seconds()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert - Default TTL of 5 minutes (300 seconds)
            Assert.AreEqual(300, options.DomainCacheTtlSeconds);
        }

        [TestMethod]
        public void Ctor_Default_SetsMaxRetriesToThree()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert - Reasonable default for retries
            Assert.AreEqual(3, options.MaxRetries);
        }

        [TestMethod]
        public void Ctor_Default_SetsSiteNameToNull()
        {
            // Arrange / Act
            DfsClientOptions options = new DfsClientOptions();

            // Assert - No site name by default
            Assert.IsNull(options.SiteName);
        }

        [TestMethod]
        public void SiteName_CanBeSet()
        {
            // Arrange
            DfsClientOptions options = new DfsClientOptions();

            // Act
            options.SiteName = "Default-First-Site-Name";

            // Assert
            Assert.AreEqual("Default-First-Site-Name", options.SiteName);
        }
    }
}
