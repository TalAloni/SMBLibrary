using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMBLibrary;
using System;
using System.Collections.Generic;

namespace SMBLibrary.Tests.DFS
{
    [TestClass]
    public class ReferralCacheEntryTests
    {
        [TestMethod]
        public void Ctor_WithPathPrefix_SetsDfsPathPrefix()
        {
            // Arrange & Act
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Assert
            Assert.AreEqual(@"\\contoso.com\dfs", entry.DfsPathPrefix);
        }

        [TestMethod]
        public void TtlSeconds_SetValue_ReturnsCorrectValue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Act
            entry.TtlSeconds = 300;

            // Assert
            Assert.AreEqual((uint)300, entry.TtlSeconds);
        }

        [TestMethod]
        public void IsRoot_WhenRootOrLinkIsRoot_ReturnsTrue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.RootOrLink = ReferralCacheEntryType.Root;

            // Assert
            Assert.IsTrue(entry.IsRoot);
            Assert.IsFalse(entry.IsLink);
        }

        [TestMethod]
        public void IsLink_WhenRootOrLinkIsLink_ReturnsTrue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs\folder");
            entry.RootOrLink = ReferralCacheEntryType.Link;

            // Assert
            Assert.IsTrue(entry.IsLink);
            Assert.IsFalse(entry.IsRoot);
        }

        [TestMethod]
        public void IsExpired_WhenExpiresUtcInPast_ReturnsTrue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(-10);

            // Assert
            Assert.IsTrue(entry.IsExpired);
        }

        [TestMethod]
        public void IsExpired_WhenExpiresUtcInFuture_ReturnsFalse()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.ExpiresUtc = DateTime.UtcNow.AddSeconds(300);

            // Assert
            Assert.IsFalse(entry.IsExpired);
        }

        [TestMethod]
        public void TargetList_Default_IsEmpty()
        {
            // Arrange & Act
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Assert
            Assert.IsNotNull(entry.TargetList);
            Assert.AreEqual(0, entry.TargetList.Count);
        }

        [TestMethod]
        public void TargetList_AddTargets_ContainsTargets()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            TargetSetEntry target1 = new TargetSetEntry(@"\\server1\share");
            TargetSetEntry target2 = new TargetSetEntry(@"\\server2\share");

            // Act
            entry.TargetList.Add(target1);
            entry.TargetList.Add(target2);

            // Assert
            Assert.AreEqual(2, entry.TargetList.Count);
            Assert.AreEqual(@"\\server1\share", entry.TargetList[0].TargetPath);
            Assert.AreEqual(@"\\server2\share", entry.TargetList[1].TargetPath);
        }

        [TestMethod]
        public void GetTargetHint_WithTargets_ReturnsFirstTarget()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.TargetList.Add(new TargetSetEntry(@"\\server1\share"));
            entry.TargetList.Add(new TargetSetEntry(@"\\server2\share"));

            // Act
            TargetSetEntry hint = entry.GetTargetHint();

            // Assert
            Assert.IsNotNull(hint);
            Assert.AreEqual(@"\\server1\share", hint.TargetPath);
        }

        [TestMethod]
        public void GetTargetHint_NoTargets_ReturnsNull()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Act
            TargetSetEntry hint = entry.GetTargetHint();

            // Assert
            Assert.IsNull(hint);
        }

        [TestMethod]
        public void NextTargetHint_AdvancesToNextTarget()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.TargetList.Add(new TargetSetEntry(@"\\server1\share"));
            entry.TargetList.Add(new TargetSetEntry(@"\\server2\share"));
            entry.TargetList.Add(new TargetSetEntry(@"\\server3\share"));

            // Act - advance to next target
            entry.NextTargetHint();
            TargetSetEntry hint = entry.GetTargetHint();

            // Assert
            Assert.AreEqual(@"\\server2\share", hint.TargetPath);
        }

        [TestMethod]
        public void NextTargetHint_WrapsAroundToFirst()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.TargetList.Add(new TargetSetEntry(@"\\server1\share"));
            entry.TargetList.Add(new TargetSetEntry(@"\\server2\share"));

            // Act - advance past end
            entry.NextTargetHint();
            entry.NextTargetHint();
            TargetSetEntry hint = entry.GetTargetHint();

            // Assert - should wrap to first
            Assert.AreEqual(@"\\server1\share", hint.TargetPath);
        }

        [TestMethod]
        public void ResetTargetHint_ResetsToFirst()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");
            entry.TargetList.Add(new TargetSetEntry(@"\\server1\share"));
            entry.TargetList.Add(new TargetSetEntry(@"\\server2\share"));
            entry.NextTargetHint(); // move to second

            // Act
            entry.ResetTargetHint();
            TargetSetEntry hint = entry.GetTargetHint();

            // Assert
            Assert.AreEqual(@"\\server1\share", hint.TargetPath);
        }

        [TestMethod]
        public void IsInterlink_Default_IsFalse()
        {
            // Arrange & Act
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Assert
            Assert.IsFalse(entry.IsInterlink);
        }

        [TestMethod]
        public void IsInterlink_SetTrue_ReturnsTrue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Act
            entry.IsInterlink = true;

            // Assert
            Assert.IsTrue(entry.IsInterlink);
        }

        [TestMethod]
        public void TargetFailback_Default_IsFalse()
        {
            // Arrange & Act
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Assert
            Assert.IsFalse(entry.TargetFailback);
        }

        [TestMethod]
        public void TargetFailback_SetTrue_ReturnsTrue()
        {
            // Arrange
            ReferralCacheEntry entry = new ReferralCacheEntry(@"\\contoso.com\dfs");

            // Act
            entry.TargetFailback = true;

            // Assert
            Assert.IsTrue(entry.TargetFailback);
        }
    }
}
