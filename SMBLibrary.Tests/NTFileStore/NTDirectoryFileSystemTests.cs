/* Copyright (C) 2019-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
