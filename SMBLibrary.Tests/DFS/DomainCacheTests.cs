using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using System;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class DomainCacheTests
    {
        [TestMethod]
        public void Lookup_ExistingDomain_ReturnsEntry()
        {
            // Arrange
            DomainCache cache = new DomainCache();
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            DomainCacheEntry result = cache.Lookup("contoso.com");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("contoso.com", result.DomainName);
        }

        [TestMethod]
        public void Lookup_ExpiredEntry_ReturnsNull()
        {
            // Arrange
            DomainCache cache = new DomainCache();
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);
            cache.Add(entry);

            // Act
            DomainCacheEntry result = cache.Lookup("contoso.com");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Lookup_NoMatch_ReturnsNull()
        {
            // Arrange
            DomainCache cache = new DomainCache();
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            DomainCacheEntry result = cache.Lookup("fabrikam.com");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ClearExpired_RemovesOnlyExpired()
        {
            // Arrange
            DomainCache cache = new DomainCache();

            DomainCacheEntry expired = new DomainCacheEntry("expired.com");
            expired.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);
            cache.Add(expired);

            DomainCacheEntry valid = new DomainCacheEntry("valid.com");
            valid.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(valid);

            // Act
            cache.ClearExpired();

            // Assert
            Assert.IsNull(cache.Lookup("expired.com"));
            Assert.IsNotNull(cache.Lookup("valid.com"));
        }

        [TestMethod]
        public void Remove_ExistingDomain_RemovesIt()
        {
            // Arrange
            DomainCache cache = new DomainCache();
            DomainCacheEntry entry = new DomainCacheEntry("contoso.com");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            bool removed = cache.Remove("contoso.com");

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(cache.Lookup("contoso.com"));
        }

        [TestMethod]
        public void Remove_NonExistingDomain_ReturnsFalse()
        {
            // Arrange
            DomainCache cache = new DomainCache();

            // Act
            bool removed = cache.Remove("contoso.com");

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Clear_RemovesAllEntries()
        {
            // Arrange
            DomainCache cache = new DomainCache();

            DomainCacheEntry entry1 = new DomainCacheEntry("contoso.com");
            entry1.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry1);

            DomainCacheEntry entry2 = new DomainCacheEntry("fabrikam.com");
            entry2.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry2);

            // Act
            cache.Clear();

            // Assert
            Assert.IsNull(cache.Lookup("contoso.com"));
            Assert.IsNull(cache.Lookup("fabrikam.com"));
        }

        [TestMethod]
        public void Lookup_CaseInsensitive_FindsEntry()
        {
            // Arrange
            DomainCache cache = new DomainCache();
            DomainCacheEntry entry = new DomainCacheEntry("CONTOSO.COM");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            DomainCacheEntry result = cache.Lookup("contoso.com");

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
