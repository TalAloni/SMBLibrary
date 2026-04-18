/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBLibrary
{
    /// <summary>
    /// Referral cache for DFS path resolution per MS-DFSC 3.1.5.1.
    /// Stores referral entries keyed by DFS path prefix for O(1) exact lookups.
    /// Supports longest-prefix matching for path resolution.
    /// Thread-safe via locking.
    /// </summary>
    public class ReferralCache
    {
        private readonly Dictionary<string, ReferralCacheEntry> m_cache;
        private readonly object m_lock = new object();

        public ReferralCache()
        {
            m_cache = new Dictionary<string, ReferralCacheEntry>(StringComparer.OrdinalIgnoreCase);
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

            lock (m_lock)
            {
                m_cache[entry.DfsPathPrefix] = entry;
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

            lock (m_lock)
            {
                // Try exact match first
                ReferralCacheEntry exactMatch;
                if (m_cache.TryGetValue(path, out exactMatch))
                {
                    if (!exactMatch.IsExpired)
                    {
                        return exactMatch;
                    }
                }

                // Find longest prefix match
                ReferralCacheEntry bestMatch = null;
                int bestMatchLength = 0;

                foreach (KeyValuePair<string, ReferralCacheEntry> kvp in m_cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        continue;
                    }

                    string prefix = kvp.Key;
                    if (IsPathPrefix(prefix, path) && prefix.Length > bestMatchLength)
                    {
                        bestMatch = kvp.Value;
                        bestMatchLength = prefix.Length;
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
            lock (m_lock)
            {
                return m_cache.Remove(pathPrefix);
            }
        }

        /// <summary>
        /// Clears all expired entries from the cache.
        /// </summary>
        public void ClearExpired()
        {
            lock (m_lock)
            {
                List<string> expiredKeys = new List<string>();
                foreach (KeyValuePair<string, ReferralCacheEntry> kvp in m_cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (string key in expiredKeys)
                {
                    m_cache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all entries from the cache.
        /// </summary>
        public void Clear()
        {
            lock (m_lock)
            {
                m_cache.Clear();
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
                    return m_cache.Count;
                }
            }
        }

        /// <summary>
        /// Checks if prefix is a path prefix of fullPath (case-insensitive).
        /// </summary>
        private static bool IsPathPrefix(string prefix, string fullPath)
        {
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            if (fullPath.Length < prefix.Length)
            {
                return false;
            }

            // Case-insensitive prefix check
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Ensure it's a path boundary (exact match or followed by backslash)
            if (fullPath.Length == prefix.Length)
            {
                return true;
            }

            char nextChar = fullPath[prefix.Length];
            return nextChar == '\\' || nextChar == '/';
        }
    }
}
