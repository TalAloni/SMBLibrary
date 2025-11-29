using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.DFS;
using System;
using System.Collections.Generic;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DfsReferralEntryV3Tests
    {
        [TestMethod]
        public void Ctor_AndFields_ShouldPreserveValues()
        {
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            entry.VersionNumber = 3;
            entry.Size = 64;
            entry.ServerType = DfsServerType.Root;
            entry.ReferralEntryFlags = DfsReferralEntryFlags.TargetSetBoundary;
            entry.TimeToLive = 900;
            entry.DfsPath = @"\\contoso.com\Public";
            entry.DfsAlternatePath = @"\\contoso.com\PublicAltV3";
            entry.NetworkAddress = @"\\fs3\Public";

            Assert.AreEqual((ushort)3, entry.VersionNumber);
            Assert.AreEqual((ushort)64, entry.Size);
            Assert.AreEqual(DfsServerType.Root, entry.ServerType);
            Assert.AreEqual(DfsReferralEntryFlags.TargetSetBoundary, entry.ReferralEntryFlags);
            Assert.AreEqual((uint)900, entry.TimeToLive);
            Assert.AreEqual(@"\\contoso.com\Public", entry.DfsPath);
            Assert.AreEqual(@"\\contoso.com\PublicAltV3", entry.DfsAlternatePath);
            Assert.AreEqual(@"\\fs3\Public", entry.NetworkAddress);

            DfsReferralEntry asBase = entry;
            Assert.IsNotNull(asBase);
        }

        [TestMethod]
        public void IsNameListReferral_WhenFlagSet_ReturnsTrue()
        {
            // Arrange - V3 supports NameListReferral per MS-DFSC 2.2.4.3
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            entry.ReferralEntryFlags = DfsReferralEntryFlags.NameListReferral;

            // Assert
            Assert.IsTrue(entry.IsNameListReferral);
        }

        [TestMethod]
        public void IsNameListReferral_WhenFlagNotSet_ReturnsFalse()
        {
            // Arrange
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            entry.ReferralEntryFlags = DfsReferralEntryFlags.None;

            // Assert
            Assert.IsFalse(entry.IsNameListReferral);
        }

        [TestMethod]
        public void ServiceSiteGuid_ShouldPreserveValue()
        {
            // Arrange - NameListReferral entries have ServiceSiteGuid
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            Guid testGuid = new Guid("12345678-1234-1234-1234-123456789abc");

            // Act
            entry.ServiceSiteGuid = testGuid;

            // Assert
            Assert.AreEqual(testGuid, entry.ServiceSiteGuid);
        }

        [TestMethod]
        public void SpecialName_ShouldPreserveValue()
        {
            // Arrange - NameListReferral entries have SpecialName (domain name)
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();

            // Act
            entry.SpecialName = "contoso.com";

            // Assert
            Assert.AreEqual("contoso.com", entry.SpecialName);
        }

        [TestMethod]
        public void ExpandedNames_ShouldPreserveValue()
        {
            // Arrange - NameListReferral entries have ExpandedNames (DC list)
            DfsReferralEntryV3 entry = new DfsReferralEntryV3();
            List<string> dcList = new List<string> { "DC1.contoso.com", "DC2.contoso.com" };

            // Act
            entry.ExpandedNames = dcList;

            // Assert
            Assert.AreEqual(2, entry.ExpandedNames.Count);
            Assert.AreEqual("DC1.contoso.com", entry.ExpandedNames[0]);
            Assert.AreEqual("DC2.contoso.com", entry.ExpandedNames[1]);
        }
    }
}
