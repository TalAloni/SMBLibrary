/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBLibrary.Client.DFS
{
    public class DfsPath
    {
        private List<string> m_components;

        public DfsPath(string uncPath)
        {
            if (uncPath == null)
            {
                throw new ArgumentNullException(nameof(uncPath));
            }

            if (uncPath.Length == 0)
            {
                throw new ArgumentException("UNC path must not be empty.", nameof(uncPath));
            }

            m_components = new List<string>();
            string[] parts = uncPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                m_components.Add(part);
            }

            if (m_components.Count == 0)
            {
                throw new ArgumentException("UNC path contains no components.", nameof(uncPath));
            }
        }

        private DfsPath(List<string> components)
        {
            m_components = components;
        }

        public string ToUncPath()
        {
            return @"\\" + String.Join(@"\", m_components.ToArray());
        }

        public DfsPath ReplacePrefix(DfsPath oldPrefix, DfsPath newPrefix)
        {
            List<string> oldComponents = oldPrefix.m_components;
            List<string> newComponents = newPrefix.m_components;

            if (oldComponents.Count > m_components.Count)
            {
                return this;
            }

            for (int i = 0; i < oldComponents.Count; i++)
            {
                if (!String.Equals(m_components[i], oldComponents[i], StringComparison.OrdinalIgnoreCase))
                {
                    return this;
                }
            }

            List<string> result = new List<string>(newComponents);
            for (int i = oldComponents.Count; i < m_components.Count; i++)
            {
                result.Add(m_components[i]);
            }

            return new DfsPath(result);
        }

        public override string ToString()
        {
            return ToUncPath();
        }

        public string ServerName
        {
            get
            {
                return m_components[0];
            }
        }

        public string ShareName
        {
            get
            {
                if (m_components.Count > 1)
                {
                    return m_components[1];
                }
                return null;
            }
        }

        public bool HasOnlyOneComponent
        {
            get
            {
                return m_components.Count == 1;
            }
        }

        public bool IsSysVolOrNetLogon
        {
            get
            {
                if (m_components.Count < 2)
                {
                    return false;
                }

                string share = m_components[1];
                return String.Equals(share, "SYSVOL", StringComparison.OrdinalIgnoreCase) ||
                       String.Equals(share, "NETLOGON", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsIpc
        {
            get
            {
                if (m_components.Count < 2)
                {
                    return false;
                }

                return String.Equals(m_components[1], "IPC$", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
