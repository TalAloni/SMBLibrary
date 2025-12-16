using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using System;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ReferralCacheTests
    {
        [TestMethod]
        public void Lookup_ExactMatch_ReturnsEntry()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            ReferralCacheEntry result = cache.Lookup(@"\\contoso.com\dfs");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(@"\\contoso.com\dfs", result.DfsPathPrefix);
        }

        [TestMethod]
        public void Lookup_PrefixMatch_ReturnsLongestPrefix()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            
            ReferralCacheEntry rootEntry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            rootEntry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(rootEntry);
            
            ReferralCacheEntry linkEntry = new ReferralCacheEntry(@"\\contoso.com\dfs\projects");
            linkEntry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(linkEntry);

            // Act - lookup a path under \dfs\projects
            ReferralCacheEntry result = cache.Lookup(@"\\contoso.com\dfs\projects\alpha\readme.txt");

            // Assert - should return the longest matching prefix
            Assert.IsNotNull(result);
            Assert.AreEqual(@"\\contoso.com\dfs\projects", result.DfsPathPrefix);
        }

        [TestMethod]
        public void Lookup_NoMatch_ReturnsNull()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            ReferralCacheEntry result = cache.Lookup(@"\\other.com\share");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Lookup_ExpiredEntry_ReturnsNull()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10); // expired
            cache.Add(entry);

            // Act
            ReferralCacheEntry result = cache.Lookup(@"\\contoso.com\dfs");

            // Assert - expired entries should not be returned
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ClearExpired_RemovesOnlyExpired()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            
            ReferralCacheEntry expiredEntry = new ReferralCacheEntry(@"\\contoso.com\expired");
            expiredEntry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);
            cache.Add(expiredEntry);
            
            ReferralCacheEntry validEntry = new ReferralCacheEntry(@"\\contoso.com\valid");
            validEntry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(validEntry);

            // Act
            cache.ClearExpired();

            // Assert
            Assert.IsNull(cache.Lookup(@"\\contoso.com\expired"));
            Assert.IsNotNull(cache.Lookup(@"\\contoso.com\valid"));
        }

        [TestMethod]
        public void Remove_ExistingEntry_RemovesIt()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act
            bool removed = cache.Remove(@"\\contoso.com\dfs");

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(cache.Lookup(@"\\contoso.com\dfs"));
        }

        [TestMethod]
        public void Remove_NonExistingEntry_ReturnsFalse()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();

            // Act
            bool removed = cache.Remove(@"\\contoso.com\dfs");

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Clear_RemovesAllEntries()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            cache.Add(new ReferralCacheEntry(@"\\contoso.com\dfs") { ExpiresUtc = DateTime.UtcNow.AddMinutes(5) });
            cache.Add(new ReferralCacheEntry(@"\\contoso.com\other") { ExpiresUtc = DateTime.UtcNow.AddMinutes(5) });

            // Act
            cache.Clear();

            // Assert
            Assert.IsNull(cache.Lookup(@"\\contoso.com\dfs"));
            Assert.IsNull(cache.Lookup(@"\\contoso.com\other"));
        }

        [TestMethod]
        public void Lookup_CaseInsensitive_FindsEntry()
        {
            // Arrange
            ReferralCache cache = new ReferralCache();
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\CONTOSO.COM\DFS");
            entry.ExpiresUtc = DateTime.UtcNow.AddMinutes(5);
            cache.Add(entry);

            // Act - lookup with different case
            ReferralCacheEntry result = cache.Lookup(@"\\contoso.com\dfs");

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
