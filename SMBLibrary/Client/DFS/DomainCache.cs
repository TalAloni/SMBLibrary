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
    /// Domain cache for DFS domain referrals per MS-DFSC 3.1.5.1.
    /// Stores DomainCacheEntry instances keyed by domain name.
    /// Thread-safe via locking.
    /// </summary>
    public class DomainCache
    {
        private readonly Dictionary<string, DomainCacheEntry> m_cache;
        private readonly object m_lock = new object();

        public DomainCache()
        {
            m_cache = new Dictionary<string, DomainCacheEntry>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds or updates a domain cache entry.
        /// </summary>
        public void Add(DomainCacheEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            lock (m_lock)
            {
                m_cache[entry.DomainName] = entry;
            }
        }

        /// <summary>
        /// Looks up a domain entry by domain name.
        /// Returns null if not found or expired.
        /// </summary>
        public DomainCacheEntry Lookup(string domainName)
        {
            if (string.IsNullOrEmpty(domainName))
            {
                return null;
            }

            lock (m_lock)
            {
                DomainCacheEntry entry;
                if (m_cache.TryGetValue(domainName, out entry))
                {
                    if (!entry.IsExpired)
                    {
                        return entry;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Removes an entry by domain name.
        /// Returns true if an entry was removed.
        /// </summary>
        public bool Remove(string domainName)
        {
            lock (m_lock)
            {
                return m_cache.Remove(domainName);
            }
        }

        // Clears only expired entries from the cache.
        public void ClearExpired()
        {
            lock (m_lock)
            {
                List<string> expired = new List<string>();
                foreach (KeyValuePair<string, DomainCacheEntry> kvp in m_cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expired.Add(kvp.Key);
                    }
                }

                foreach (string key in expired)
                {
                    m_cache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all entries.
        /// </summary>
        public void Clear()
        {
            lock (m_lock)
            {
                m_cache.Clear();
            }
        }

        /// <summary>
        /// Number of entries in the cache.
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
    }
}
