using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Win32;

namespace SMBLibrary.Tests
{
    [TestClass]
    public class NTDirectoryFileSystemTests : NTFileStoreTests
    {
        private static readonly string TestDirectoryPath = @"C:\Tests";

        static NTDirectoryFileSystemTests()
        {
            if (!Directory.Exists(TestDirectoryPath))
            {
                Directory.CreateDirectory(TestDirectoryPath);
            }
        }

        public NTDirectoryFileSystemTests() : base(new NTDirectoryFileSystem(TestDirectoryPath))
        {
        }
    }
}
