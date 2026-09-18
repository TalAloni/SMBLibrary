/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary.SMB2;

namespace SMBLibrary.Tests.SMB2
{
    [TestClass]
    public class CreateContextTests
    {
        [TestMethod]
        public void TimeWarpTokenUsesTheNameFromTheSpecification()
        {
            CreateContext context = CreateContextHelper.CreateTimeWarpToken(DateTime.UtcNow);

            Assert.AreEqual(CreateContextName.TimeWarpToken, context.Name);
        }

        [TestMethod]
        public void TimeWarpTokenCarriesTheTimestampAsAFileTime()
        {
            DateTime timestamp = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

            CreateContext context = CreateContextHelper.CreateTimeWarpToken(timestamp);

            Assert.AreEqual(8, context.Data.Length);
            Assert.AreEqual(timestamp, CreateContextHelper.ReadTimeWarpToken(context));
        }

        [TestMethod]
        public void CreateRequestRoundTripsATimeWarpToken()
        {
            DateTime timestamp = new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
            CreateRequest request = new CreateRequest();
            request.Name = "reports\\q3.xlsx";
            request.CreateContexts.Add(CreateContextHelper.CreateTimeWarpToken(timestamp));

            CreateRequest parsed = new CreateRequest(request.GetBytes(), 0);

            Assert.AreEqual("reports\\q3.xlsx", parsed.Name);
            Assert.AreEqual(1, parsed.CreateContexts.Count);
            Assert.AreEqual(CreateContextName.TimeWarpToken, parsed.CreateContexts[0].Name);
            Assert.AreEqual(timestamp, CreateContextHelper.ReadTimeWarpToken(parsed.CreateContexts[0]));
        }

        [TestMethod]
        public void CreateRequestRoundTripsSeveralCreateContexts()
        {
            CreateRequest request = new CreateRequest();
            request.Name = "reports\\q3.xlsx";
            request.CreateContexts.Add(CreateContextHelper.CreateTimeWarpToken(DateTime.UtcNow));
            request.CreateContexts.Add(new CreateContext() { Name = CreateContextName.QueryMaximalAccess, Data = new byte[0] });

            CreateRequest parsed = new CreateRequest(request.GetBytes(), 0);

            List<string> names = new List<string>();
            foreach (CreateContext context in parsed.CreateContexts)
            {
                names.Add(context.Name);
            }
            CollectionAssert.AreEqual(new List<string> { CreateContextName.TimeWarpToken, CreateContextName.QueryMaximalAccess }, names);
        }
    }
}
