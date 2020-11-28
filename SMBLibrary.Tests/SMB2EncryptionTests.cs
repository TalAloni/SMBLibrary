/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.SMB2;
using Utilities;

namespace SMBLibrary.Tests
{
    [TestClass]
    // https://docs.microsoft.com/en-us/archive/blogs/openspecification/encryption-in-smb-3-0-a-protocol-perspective
    public class SMB2EncryptionTests
    {
        [TestMethod]
        public void TestEncryptionKeyGeneration()
        {
            byte[] sessionKey = new byte[]{ 0xB4, 0x54, 0x67, 0x71, 0xB5, 0x15, 0xF7, 0x66, 0xA8, 0x67, 0x35, 0x53, 0x2D, 0xD6, 0xC4, 0xF0};

            byte[] expectedEncryptionKey = new byte[] { 0x26, 0x1B, 0x72, 0x35, 0x05, 0x58, 0xF2, 0xE9, 0xDC, 0xF6, 0x13, 0x07, 0x03, 0x83, 0xED, 0xBF };
            
            byte[] encryptionKey = SMB2Cryptography.GenerateClientEncryptionKey(sessionKey, SMB2Dialect.SMB300, null);

            Assert.IsTrue(ByteUtils.AreByteArraysEqual(expectedEncryptionKey, encryptionKey));
        }

        [TestMethod]
        public void TestDecryptionKeyGeneration()
        {
            byte[] sessionKey = new byte[] { 0xB4, 0x54, 0x67, 0x71, 0xB5, 0x15, 0xF7, 0x66, 0xA8, 0x67, 0x35, 0x53, 0x2D, 0xD6, 0xC4, 0xF0 };

            byte[] decryptionKey = SMB2Cryptography.GenerateClientDecryptionKey(sessionKey, SMB2Dialect.SMB300, null);

            byte[] expectedDecryptionKey = new byte[] { 0x8F, 0xE2, 0xB5, 0x7E, 0xC3, 0x4D, 0x2D, 0xB5, 0xB1, 0xA9, 0x72, 0x7F, 0x52, 0x6B, 0xBD, 0xB5 };

            Assert.IsTrue(ByteUtils.AreByteArraysEqual(expectedDecryptionKey, decryptionKey));
        }

        public void TestAll()
        {
            TestEncryptionKeyGeneration();
            TestDecryptionKeyGeneration();
        }
    }
}
