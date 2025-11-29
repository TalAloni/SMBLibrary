/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using SMBLibrary.DFS;

namespace SMBLibrary
{
    /// <summary>
    /// Represents a single target in a DFS referral response.
    /// Used for target failover and load balancing per MS-DFSC 3.1.5.4.5.
    /// </summary>
    public class TargetSetEntry
    {
        private string m_targetPath;
        private int m_priority;
        private bool m_isTargetSetBoundary;
        private DfsServerType m_serverType;

        public TargetSetEntry(string targetPath)
        {
            m_targetPath = targetPath;
            m_priority = 0;
            m_isTargetSetBoundary = false;
            m_serverType = DfsServerType.NonRoot;
        }

        /// <summary>
        /// The UNC path to the target server/share (NetworkAddress from referral).
        /// </summary>
        public string TargetPath
        {
            get { return m_targetPath; }
            set { m_targetPath = value; }
        }

        /// <summary>
        /// Target priority for failover ordering (lower = higher priority).
        /// Derived from referral Proximity or target set position.
        /// </summary>
        public int Priority
        {
            get { return m_priority; }
            set { m_priority = value; }
        }

        /// <summary>
        /// True if this target marks the start of a new target set (V4 referrals).
        /// </summary>
        public bool IsTargetSetBoundary
        {
            get { return m_isTargetSetBoundary; }
            set { m_isTargetSetBoundary = value; }
        }

        /// <summary>
        /// The type of DFS server (Root or NonRoot).
        /// </summary>
        public DfsServerType ServerType
        {
            get { return m_serverType; }
            set { m_serverType = value; }
        }
    }
}
