/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Tree-based referral cache for DFS path resolution per MS-DFSC 3.1.5.1.
    /// Provides O(k) longest-prefix-match lookups where k is the path depth.
    /// Thread-safe via locking.
    /// </summary>
    public class ReferralCacheTree
    {
        private readonly ReferralCacheNode m_root;
        private readonly object m_lock = new object();
        private int m_count;

        public ReferralCacheTree()
        {
            m_root = new ReferralCacheNode(string.Empty);
            m_count = 0;
        }

        /// <summary>
        /// Adds or updates a referral cache entry.
        /// </summary>
        public void Add(ReferralCacheEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (string.IsNullOrEmpty(entry.DfsPathPrefix))
            {
                throw new ArgumentException("DfsPathPrefix cannot be null or empty", "entry");
            }

            List<string> components = SplitPath(entry.DfsPathPrefix);
            if (components.Count == 0)
            {
                return;
            }

            lock (m_lock)
            {
                ReferralCacheNode node = m_root;
                foreach (string component in components)
                {
                    node = node.GetOrCreateChild(component);
                }

                bool isNew = node.Entry == null;
                node.Entry = entry;

                if (isNew)
                {
                    m_count++;
                }
            }
        }

        /// <summary>
        /// Looks up a referral entry by path. Returns the longest matching prefix.
        /// Returns null if no match found or if the entry is expired.
        /// </summary>
        public ReferralCacheEntry Lookup(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            List<string> components = SplitPath(path);
            if (components.Count == 0)
            {
                return null;
            }

            lock (m_lock)
            {
                ReferralCacheEntry bestMatch = null;
                ReferralCacheNode node = m_root;

                foreach (string component in components)
                {
                    ReferralCacheNode child = node.GetChild(component);
                    if (child == null)
                    {
                        break;
                    }

                    node = child;

                    // Check if this node has a valid (non-expired) entry
                    if (node.Entry != null && !node.Entry.IsExpired)
                    {
                        bestMatch = node.Entry;
                    }
                }

                return bestMatch;
            }
        }

        /// <summary>
        /// Removes a referral entry by path prefix.
        /// Returns true if an entry was removed.
        /// </summary>
        public bool Remove(string pathPrefix)
        {
            if (string.IsNullOrEmpty(pathPrefix))
            {
                return false;
            }

            List<string> components = SplitPath(pathPrefix);
            if (components.Count == 0)
            {
                return false;
            }

            lock (m_lock)
            {
                ReferralCacheNode node = m_root;
                foreach (string component in components)
                {
                    ReferralCacheNode child = node.GetChild(component);
                    if (child == null)
                    {
                        return false;
                    }
                    node = child;
                }

                if (node.Entry != null)
                {
                    node.Entry = null;
                    m_count--;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Clears all expired entries from the cache.
        /// </summary>
        public void ClearExpired()
        {
            lock (m_lock)
            {
                ClearExpiredRecursive(m_root);
            }
        }

        private void ClearExpiredRecursive(ReferralCacheNode node)
        {
            if (node.Entry != null && node.Entry.IsExpired)
            {
                node.Entry = null;
                m_count--;
            }

            foreach (ReferralCacheNode child in node.GetChildren())
            {
                ClearExpiredRecursive(child);
            }
        }

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        public void Clear()
        {
            lock (m_lock)
            {
                m_root.ClearChildren();
                m_count = 0;
            }
        }

        /// <summary>
        /// Gets the number of entries in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                lock (m_lock)
                {
                    return m_count;
                }
            }
        }

        /// <summary>
        /// Splits a UNC path into components.
        /// </summary>
        private static List<string> SplitPath(string path)
        {
            List<string> components = new List<string>();
            char[] separators = new char[] { '\\', '/' };
            string[] parts = path.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    components.Add(part);
                }
            }

            return components;
        }

        /// <summary>
        /// Internal tree node for the referral cache.
        /// </summary>
        private class ReferralCacheNode
        {
            private readonly Dictionary<string, ReferralCacheNode> m_children;
            private ReferralCacheEntry m_entry;

            public ReferralCacheNode(string component)
            {
                // Component parameter kept for potential future use (e.g., debugging, path reconstruction)
                m_children = new Dictionary<string, ReferralCacheNode>(StringComparer.OrdinalIgnoreCase);
                m_entry = null;
            }

            public ReferralCacheEntry Entry
            {
                get { return m_entry; }
                set { m_entry = value; }
            }

            public ReferralCacheNode GetChild(string component)
            {
                ReferralCacheNode child;
                if (m_children.TryGetValue(component, out child))
                {
                    return child;
                }
                return null;
            }

            public ReferralCacheNode GetOrCreateChild(string component)
            {
                ReferralCacheNode child;
                if (!m_children.TryGetValue(component, out child))
                {
                    child = new ReferralCacheNode(component);
                    m_children[component] = child;
                }
                return child;
            }

            public IEnumerable<ReferralCacheNode> GetChildren()
            {
                return m_children.Values;
            }

            public void ClearChildren()
            {
                m_children.Clear();
            }
        }
    }
}
