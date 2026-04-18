using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using SMBLibrary.Client.DFS;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ReferralCacheTreeTests
    {
        #region Add and Lookup Tests

        [TestMethod]
        public void Add_ThenLookup_ReturnsEntry()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = CreateEntry(@"\\domain\share", 300);

            // Act
            cache.Add(entry);
            var result = cache.Lookup(@"\\domain\share");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(entry.DfsPathPrefix, result.DfsPathPrefix);
        }

        [TestMethod]
        public void Lookup_WithSubpath_ReturnsLongestMatch()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var rootEntry = CreateEntry(@"\\domain\dfs", 300);
            var linkEntry = CreateEntry(@"\\domain\dfs\folder", 300);

            cache.Add(rootEntry);
            cache.Add(linkEntry);

            // Act
            var result = cache.Lookup(@"\\domain\dfs\folder\subdir\file.txt");

            // Assert - should return the longer match
            Assert.IsNotNull(result);
            Assert.AreEqual(@"\\domain\dfs\folder", result.DfsPathPrefix);
        }

        [TestMethod]
        public void Lookup_WithPartialPath_ReturnsRootMatch()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var rootEntry = CreateEntry(@"\\domain\dfs", 300);
            var linkEntry = CreateEntry(@"\\domain\dfs\folder", 300);

            cache.Add(rootEntry);
            cache.Add(linkEntry);

            // Act - lookup path that only matches root
            var result = cache.Lookup(@"\\domain\dfs\other\file.txt");

            // Assert - should return root match
            Assert.IsNotNull(result);
            Assert.AreEqual(@"\\domain\dfs", result.DfsPathPrefix);
        }

        [TestMethod]
        public void Lookup_WithNoMatch_ReturnsNull()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = CreateEntry(@"\\domain\share", 300);
            cache.Add(entry);

            // Act
            var result = cache.Lookup(@"\\other\path");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Lookup_WhenExpired_ReturnsNull()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = new ReferralCacheEntry(@"\\domain\share");
            entry.TtlSeconds = 1;
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10); // Already expired
            entry.TargetList.Add(new TargetSetEntry(@"\\server\share"));

            cache.Add(entry);

            // Act
            var result = cache.Lookup(@"\\domain\share");

            // Assert
            Assert.IsNull(result, "Expired entries should not be returned");
        }

        [TestMethod]
        public void Lookup_WithExpiredParent_ReturnsValidChild()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            
            // Expired parent
            var parentEntry = new ReferralCacheEntry(@"\\domain\dfs");
            parentEntry.TtlSeconds = 1;
            parentEntry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);
            parentEntry.TargetList.Add(new TargetSetEntry(@"\\server\dfs"));

            // Valid child
            var childEntry = CreateEntry(@"\\domain\dfs\folder", 300);

            cache.Add(parentEntry);
            cache.Add(childEntry);

            // Act
            var result = cache.Lookup(@"\\domain\dfs\folder\file.txt");

            // Assert - should skip expired parent, return valid child
            Assert.IsNotNull(result);
            Assert.AreEqual(@"\\domain\dfs\folder", result.DfsPathPrefix);
        }

        #endregion

        #region Remove Tests

        [TestMethod]
        public void Remove_ExistingEntry_ReturnsTrue()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = CreateEntry(@"\\domain\share", 300);
            cache.Add(entry);

            // Act
            bool removed = cache.Remove(@"\\domain\share");

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNull(cache.Lookup(@"\\domain\share"));
            Assert.AreEqual(0, cache.Count);
        }

        [TestMethod]
        public void Remove_NonExistingEntry_ReturnsFalse()
        {
            // Arrange
            var cache = new ReferralCacheTree();

            // Act
            bool removed = cache.Remove(@"\\domain\share");

            // Assert
            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Remove_ChildEntry_KeepsParent()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var parentEntry = CreateEntry(@"\\domain\dfs", 300);
            var childEntry = CreateEntry(@"\\domain\dfs\folder", 300);

            cache.Add(parentEntry);
            cache.Add(childEntry);

            // Act
            bool removed = cache.Remove(@"\\domain\dfs\folder");

            // Assert
            Assert.IsTrue(removed);
            Assert.IsNotNull(cache.Lookup(@"\\domain\dfs"));
            // After removing child, lookup for child path now returns parent (longest prefix match)
            var result = cache.Lookup(@"\\domain\dfs\folder");
            Assert.IsNotNull(result, "Parent should still be returned as longest prefix match");
            Assert.AreEqual(@"\\domain\dfs", result.DfsPathPrefix);
            Assert.AreEqual(1, cache.Count);
        }

        #endregion

        #region Clear Tests

        [TestMethod]
        public void Clear_RemovesAllEntries()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            cache.Add(CreateEntry(@"\\domain\dfs", 300));
            cache.Add(CreateEntry(@"\\domain\dfs\folder1", 300));
            cache.Add(CreateEntry(@"\\domain\dfs\folder2", 300));

            // Act
            cache.Clear();

            // Assert
            Assert.AreEqual(0, cache.Count);
            Assert.IsNull(cache.Lookup(@"\\domain\dfs"));
        }

        [TestMethod]
        public void ClearExpired_RemovesOnlyExpiredEntries()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            
            var validEntry = CreateEntry(@"\\domain\dfs\valid", 300);
            
            var expiredEntry = new ReferralCacheEntry(@"\\domain\dfs\expired");
            expiredEntry.TtlSeconds = 1;
            expiredEntry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);
            expiredEntry.TargetList.Add(new TargetSetEntry(@"\\server\expired"));

            cache.Add(validEntry);
            cache.Add(expiredEntry);
            Assert.AreEqual(2, cache.Count);

            // Act
            cache.ClearExpired();

            // Assert
            Assert.AreEqual(1, cache.Count);
            Assert.IsNotNull(cache.Lookup(@"\\domain\dfs\valid"));
        }

        #endregion

        #region Count Tests

        [TestMethod]
        public void Count_ReflectsEntryCount()
        {
            // Arrange
            var cache = new ReferralCacheTree();

            // Act & Assert
            Assert.AreEqual(0, cache.Count);

            cache.Add(CreateEntry(@"\\domain\share1", 300));
            Assert.AreEqual(1, cache.Count);

            cache.Add(CreateEntry(@"\\domain\share2", 300));
            Assert.AreEqual(2, cache.Count);

            cache.Remove(@"\\domain\share1");
            Assert.AreEqual(1, cache.Count);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullEntry_ThrowsException()
        {
            var cache = new ReferralCacheTree();
            cache.Add(null);
        }

        [TestMethod]
        public void Lookup_NullPath_ReturnsNull()
        {
            var cache = new ReferralCacheTree();
            Assert.IsNull(cache.Lookup(null));
        }

        [TestMethod]
        public void Lookup_EmptyPath_ReturnsNull()
        {
            var cache = new ReferralCacheTree();
            Assert.IsNull(cache.Lookup(string.Empty));
        }

        [TestMethod]
        public void Add_SamePathTwice_UpdatesEntry()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry1 = CreateEntry(@"\\domain\share", 300);
            var entry2 = CreateEntry(@"\\domain\share", 600);

            // Act
            cache.Add(entry1);
            cache.Add(entry2);

            // Assert - should have updated, not duplicated
            Assert.AreEqual(1, cache.Count);
            var result = cache.Lookup(@"\\domain\share");
            Assert.AreEqual(600u, result.TtlSeconds);
        }

        [TestMethod]
        public void Lookup_CaseInsensitive()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = CreateEntry(@"\\DOMAIN\SHARE", 300);
            cache.Add(entry);

            // Act & Assert - should match regardless of case
            Assert.IsNotNull(cache.Lookup(@"\\domain\share"));
            Assert.IsNotNull(cache.Lookup(@"\\DOMAIN\SHARE"));
            Assert.IsNotNull(cache.Lookup(@"\\Domain\Share"));
        }

        [TestMethod]
        public void Lookup_HandlesForwardSlashes()
        {
            // Arrange
            var cache = new ReferralCacheTree();
            var entry = CreateEntry(@"\\domain\share\folder", 300);
            cache.Add(entry);

            // Act - lookup with forward slashes
            var result = cache.Lookup("//domain/share/folder/file.txt");

            // Assert
            Assert.IsNotNull(result);
        }

        #endregion

        #region Helpers

        private static ReferralCacheEntry CreateEntry(string prefix, uint ttlSeconds)
        {
            var entry = new ReferralCacheEntry(prefix);
            entry.TtlSeconds = ttlSeconds;
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(ttlSeconds);
            entry.TargetList.Add(new TargetSetEntry(@"\\server\target"));
            return entry;
        }

        #endregion
    }
}
