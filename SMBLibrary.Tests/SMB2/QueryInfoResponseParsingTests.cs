﻿/* Copyright (C) 2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.SMB2
{
    [TestClass]
    public class QueryInfoResponseParsingTests
    {
        [TestMethod]
        public void ParseQueryInfoResponseWithSecurityInfo()
        {
            byte[] queryInfoResponseCommandBytes = GetQueryInfoResponseWithSecurityInfo();
            QueryInfoResponse queryInfoResponse = new QueryInfoResponse(queryInfoResponseCommandBytes, 0);
            SecurityDescriptor securityDescriptor = queryInfoResponse.GetSecurityInformation();
            Assert.AreEqual(2, securityDescriptor.Dacl.Count);
            Assert.IsInstanceOfType(securityDescriptor.Dacl[1], typeof(AccessAllowedACE));
            Assert.AreEqual(1, ((AccessAllowedACE)securityDescriptor.Dacl[1]).Sid.Revision);
        }


        private static byte[] GetQueryInfoResponseWithSecurityInfo()
        {
            return new byte[]
            {
                0xfe, 0x53, 0x4d, 0x42, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x01, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x82, 0xe7, 0x2d, 0x2c,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x09, 0x00, 0x48, 0x00, 0x5c, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x80, 0x14, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x05, 0x20, 0x00, 0x00, 0x00, 0x20, 0x02, 0x00, 0x00, 0x02, 0x00, 0x38, 0x00,
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00, 0xff, 0x01, 0x1f, 0x00, 0x01, 0x01, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b, 0x18, 0x00,
                0x00, 0x00, 0x00, 0x10, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };
        }
    }
}
