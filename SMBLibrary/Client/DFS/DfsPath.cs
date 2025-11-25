/* Copyright (C) 2014-2024 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Helper class for parsing and manipulating DFS UNC paths.
    /// Handles path component extraction, special share detection, and prefix replacement.
    /// </summary>
    public class DfsPath
    {
        private readonly List<string> m_components;

        /// <summary>
        /// Creates a new DfsPath from a UNC path string.
        /// </summary>
        /// <param name="uncPath">The UNC path (e.g., \\server\share\folder).</param>
        /// <exception cref="ArgumentNullException">Thrown when uncPath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when uncPath is empty.</exception>
        public DfsPath(string uncPath)
        {
            if (uncPath == null)
            {
                throw new ArgumentNullException("uncPath");
            }

            if (uncPath.Length == 0)
            {
                throw new ArgumentException("UNC path cannot be empty", "uncPath");
            }

            m_components = SplitPath(uncPath);
        }

        private DfsPath(List<string> components)
        {
            m_components = components;
        }

        /// <summary>
        /// Splits a UNC path into its component parts, handling both forward and back slashes.
        /// </summary>
        private static List<string> SplitPath(string path)
        {
            List<string> components = new List<string>();
            
            // Normalize slashes and split
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
        /// Gets the path components (server, share, folders, file).
        /// </summary>
        public IList<string> PathComponents
        {
            get { return m_components; }
        }

        /// <summary>
        /// Gets the server name (first component).
        /// </summary>
        public string ServerName
        {
            get { return m_components.Count > 0 ? m_components[0] : null; }
        }

        /// <summary>
        /// Gets the share name (second component), or null if not present.
        /// </summary>
        public string ShareName
        {
            get { return m_components.Count > 1 ? m_components[1] : null; }
        }

        /// <summary>
        /// Returns true if the path has only one component (server name only).
        /// </summary>
        public bool HasOnlyOneComponent
        {
            get { return m_components.Count == 1; }
        }

        /// <summary>
        /// Returns true if the share component is SYSVOL or NETLOGON (case-insensitive).
        /// These shares require special DFS handling per MS-DFSC.
        /// </summary>
        public bool IsSysVolOrNetLogon
        {
            get
            {
                if (m_components.Count < 2)
                {
                    return false;
                }

                string share = m_components[1];
                return string.Equals(share, "SYSVOL", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(share, "NETLOGON", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Returns true if the share component is IPC$ (case-insensitive).
        /// IPC$ is excluded from DFS resolution.
        /// </summary>
        public bool IsIpc
        {
            get
            {
                if (m_components.Count < 2)
                {
                    return false;
                }

                string share = m_components[1];
                return string.Equals(share, "IPC$", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Converts the path back to a UNC string with single leading backslash.
        /// </summary>
        /// <returns>UNC path string (e.g., \server\share\folder).</returns>
        public string ToUncPath()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string component in m_components)
            {
                sb.Append('\\');
                sb.Append(component);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Replaces the specified prefix with a new path prefix.
        /// </summary>
        /// <param name="prefixToReplace">The prefix to match and replace (e.g., \server\share).</param>
        /// <param name="newPrefix">The new prefix to use.</param>
        /// <returns>A new DfsPath with the prefix replaced.</returns>
        /// <exception cref="ArgumentException">Thrown when the prefix does not match.</exception>
        public DfsPath ReplacePrefix(string prefixToReplace, DfsPath newPrefix)
        {
            // Parse the prefix to replace
            List<string> prefixComponents = SplitPath(prefixToReplace);

            // Validate prefix matches
            if (prefixComponents.Count > m_components.Count)
            {
                throw new ArgumentException("Prefix does not match the current path", "prefixToReplace");
            }

            for (int i = 0; i < prefixComponents.Count; i++)
            {
                if (!string.Equals(m_components[i], prefixComponents[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Prefix does not match the current path", "prefixToReplace");
                }
            }

            // Build new component list: new prefix + remaining components from original
            List<string> newComponents = new List<string>();
            foreach (string component in newPrefix.m_components)
            {
                newComponents.Add(component);
            }

            for (int i = prefixComponents.Count; i < m_components.Count; i++)
            {
                newComponents.Add(m_components[i]);
            }

            return new DfsPath(newComponents);
        }

        /// <summary>
        /// Returns the UNC path representation.
        /// </summary>
        public override string ToString()
        {
            return ToUncPath();
        }
    }
}
