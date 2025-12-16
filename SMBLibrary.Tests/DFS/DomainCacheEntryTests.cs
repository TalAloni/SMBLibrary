using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using System;
using System.Collections.Generic;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DomainCacheEntryTests
    {
        [TestMethod]
        public void Ctor_WithDomainName_SetsDomainName()
        {
            // Arrange & Act
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");

            // Assert
            Assert.AreEqual("contoso.com", entry.DomainName);
        }

        [TestMethod]
        public void DcList_Default_IsEmpty()
        {
            // Arrange & Act
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");

            // Assert
            Assert.IsNotNull(entry.DcList);
            Assert.AreEqual(0, entry.DcList.Count);
        }

        [TestMethod]
        public void DcList_AddDcs_ContainsDcs()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");

            // Act
            entry.DcList.Add("DC1.contoso.com");
            entry.DcList.Add("DC2.contoso.com");

            // Assert
            Assert.AreEqual(2, entry.DcList.Count);
            Assert.AreEqual("DC1.contoso.com", entry.DcList[0]);
        }

        [TestMethod]
        public void IsExpired_WhenExpiresUtcInPast_ReturnsTrue()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);

            // Assert
            Assert.IsTrue(entry.IsExpired);
        }

        [TestMethod]
        public void IsExpired_WhenExpiresUtcInFuture_ReturnsFalse()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);

            // Assert
            Assert.IsFalse(entry.IsExpired);
        }

        [TestMethod]
        public void GetDcHint_WithDcs_ReturnsFirstDc()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.DcList.Add("DC1.contoso.com");
            entry.DcList.Add("DC2.contoso.com");

            // Act
            string hint = entry.GetDcHint();

            // Assert
            Assert.AreEqual("DC1.contoso.com", hint);
        }

        [TestMethod]
        public void GetDcHint_NoDcs_ReturnsNull()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");

            // Act
            string hint = entry.GetDcHint();

            // Assert
            Assert.IsNull(hint);
        }

        [TestMethod]
        public void NextDcHint_AdvancesToNextDc()
        {
            // Arrange
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.DcList.Add("DC1.contoso.com");
            entry.DcList.Add("DC2.contoso.com");

            // Act
            entry.NextDcHint();
            string hint = entry.GetDcHint();

            // Assert
            Assert.AreEqual("DC2.contoso.com", hint);
        }
    }
}
