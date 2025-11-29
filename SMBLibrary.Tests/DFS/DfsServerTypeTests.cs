using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsServerTypeTests
    {
        [TestMethod]
        public void NonRoot_HasCorrectValue()
        {
            // MS-DFSC 2.2.4.x: NonRoot = 0x0000
            Assert.AreEqual((ushort)0x0000, (ushort)DfsServerType.NonRoot);
        }

        [TestMethod]
        public void Root_HasCorrectValue()
        {
            // MS-DFSC 2.2.4.x: Root = 0x0001
            Assert.AreEqual((ushort)0x0001, (ushort)DfsServerType.Root);
        }
    }
}
