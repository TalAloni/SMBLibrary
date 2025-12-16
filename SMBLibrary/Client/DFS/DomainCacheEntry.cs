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
    /// Represents a cached domain entry for DFS domain referrals per MS-DFSC 3.1.5.1.
    /// Contains the domain name and list of domain controllers.
    /// </summary>
    public class DomainCacheEntry
    {
        private string m_domainName;
        private List<string> m_dcList;
        private DateTime m_expiresUtc;
        private int m_dcHintIndex;

        public DomainCacheEntry(string domainName)
        {
            m_domainName = domainName;
            m_dcList = new List<string>();
            m_expiresUtc = DateTime.MinValue;
            m_dcHintIndex = 0;
        }

        /// <summary>
        /// The domain name (e.g., "contoso.com").
        /// </summary>
        public string DomainName
        {
            get { return m_domainName; }
            set { m_domainName = value; }
        }

        /// <summary>
        /// List of domain controllers for this domain.
        /// </summary>
        public List<string> DcList
        {
            get { return m_dcList; }
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
        /// True if this entry has expired.
        /// </summary>
        public bool IsExpired
        {
            get { return DateTime.UtcNow >= m_expiresUtc; }
        }

        /// <summary>
        /// Gets the current DC hint (the DC to try next).
        /// Returns null if no DCs are available.
        /// </summary>
        public string GetDcHint()
        {
            if (m_dcList.Count == 0)
            {
                return null;
            }

            int index = m_dcHintIndex;
            if (index >= m_dcList.Count)
            {
                index = 0;
            }

            return m_dcList[index];
        }

        /// <summary>
        /// Advances to the next DC in the list (round-robin failover).
        /// </summary>
        public void NextDcHint()
        {
            if (m_dcList.Count == 0)
            {
                return;
            }

            int currentIndex;
            int newIndex;
            do
            {
                currentIndex = m_dcHintIndex;
                newIndex = (currentIndex + 1) % m_dcList.Count;
            }
            while (Interlocked.CompareExchange(ref m_dcHintIndex, newIndex, currentIndex) != currentIndex);
        }

        /// <summary>
        /// Resets the DC hint to the first DC.
        /// </summary>
        public void ResetDcHint()
        {
            Interlocked.Exchange(ref m_dcHintIndex, 0);
        }
    }
}
