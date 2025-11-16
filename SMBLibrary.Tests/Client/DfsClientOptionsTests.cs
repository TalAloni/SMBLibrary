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
    }
}
