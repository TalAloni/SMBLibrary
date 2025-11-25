/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace SMBLibrary
{
    /// <summary>
    /// Represents a cached DFS referral entry per MS-DFSC 3.1.5.1.
    /// Contains the DFS path prefix, targets, TTL, and failover state.
    /// </summary>
    public class ReferralCacheEntry
    {
        private string m_dfsPathPrefix;
        private ReferralCacheEntryType m_rootOrLink;
        private bool m_isInterlink;
        private uint m_ttlSeconds;
        private DateTime m_expiresUtc;
        private bool m_targetFailback;
        private List<TargetSetEntry> m_targetList;
        private int m_targetHintIndex;

        public ReferralCacheEntry(string dfsPathPrefix)
        {
            m_dfsPathPrefix = dfsPathPrefix;
            m_rootOrLink = ReferralCacheEntryType.Root;
            m_isInterlink = false;
            m_ttlSeconds = 0;
            m_expiresUtc = DateTime.MinValue;
            m_targetFailback = false;
            m_targetList = new List<TargetSetEntry>();
            m_targetHintIndex = 0;
        }

        /// <summary>
        /// The DFS path prefix that this entry matches.
        /// </summary>
        public string DfsPathPrefix
        {
            get { return m_dfsPathPrefix; }
            set { m_dfsPathPrefix = value; }
        }

        /// <summary>
        /// Indicates whether this is a root or link referral.
        /// </summary>
        public ReferralCacheEntryType RootOrLink
        {
            get { return m_rootOrLink; }
            set { m_rootOrLink = value; }
        }

        /// <summary>
        /// True if this is a DFS root entry.
        /// </summary>
        public bool IsRoot
        {
            get { return m_rootOrLink == ReferralCacheEntryType.Root; }
        }

        /// <summary>
        /// True if this is a DFS link entry.
        /// </summary>
        public bool IsLink
        {
            get { return m_rootOrLink == ReferralCacheEntryType.Link; }
        }

        /// <summary>
        /// True if this is an interlink (link that points to another DFS namespace).
        /// </summary>
        public bool IsInterlink
        {
            get { return m_isInterlink; }
            set { m_isInterlink = value; }
        }

        /// <summary>
        /// Time-to-live in seconds from the referral response.
        /// </summary>
        public uint TtlSeconds
        {
            get { return m_ttlSeconds; }
            set { m_ttlSeconds = value; }
        }

        /// <summary>
        /// Absolute UTC time when this entry expires.
        /// </summary>
        public DateTime ExpiresUtc
        {
            get { return m_expiresUtc; }
            set { m_expiresUtc = value; }
        }

        /// <summary>
        /// True if this entry has expired and should be refreshed.
        /// </summary>
        public bool IsExpired
        {
            get { return DateTime.UtcNow >= m_expiresUtc; }
        }

        /// <summary>
        /// True if the server supports target failback (returning to higher priority targets).
        /// </summary>
        public bool TargetFailback
        {
            get { return m_targetFailback; }
            set { m_targetFailback = value; }
        }

        /// <summary>
        /// List of targets for this referral, in priority order.
        /// </summary>
        public List<TargetSetEntry> TargetList
        {
            get { return m_targetList; }
        }

        /// <summary>
        /// Gets the current target hint (the target to try next).
        /// Returns null if no targets are available.
        /// </summary>
        public TargetSetEntry GetTargetHint()
        {
            if (m_targetList.Count == 0)
            {
                return null;
            }

            int index = m_targetHintIndex;
            if (index >= m_targetList.Count)
            {
                index = 0;
            }

            return m_targetList[index];
        }

        /// <summary>
        /// Advances to the next target in the list (round-robin failover).
        /// Thread-safe via Interlocked.CompareExchange.
        /// </summary>
        public void NextTargetHint()
        {
            if (m_targetList.Count == 0)
            {
                return;
            }

            int currentIndex;
            int newIndex;
            do
            {
                currentIndex = m_targetHintIndex;
                newIndex = (currentIndex + 1) % m_targetList.Count;
            }
            while (Interlocked.CompareExchange(ref m_targetHintIndex, newIndex, currentIndex) != currentIndex);
        }

        /// <summary>
        /// Resets the target hint to the first (highest priority) target.
        /// </summary>
        public void ResetTargetHint()
        {
            Interlocked.Exchange(ref m_targetHintIndex, 0);
        }
    }
}
