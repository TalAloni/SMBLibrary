/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.Server
{
    public class ShareCollection : List<FileSystemShare>
    {
        public void Add(string shareName, List<string> readAccess, List<string> writeAccess, IFileSystem fileSystem)
        {
            FileSystemShare share = new FileSystemShare();
            share.Name = shareName;
            share.ReadAccess = readAccess;
            share.WriteAccess = writeAccess;
            share.FileSystem = fileSystem;
            this.Add(share);
        }
        public bool Contains(string shareName, StringComparison comparisonType)
        {
            return (this.IndexOf(shareName, comparisonType) != -1);
        }

        public int IndexOf(string shareName, StringComparison comparisonType)
        {
            for (int index = 0; index < this.Count; index++)
            {
                if (this[index].Name.Equals(shareName, comparisonType))
                {
                    return index;
                }
            }

            return -1;
        }

        public List<string> ListShares()
        {
            List<string> result = new List<string>();
            foreach (FileSystemShare share in this)
            {
                result.Add(share.Name);
            }
            return result;
        }

        /// <param name="relativePath">e.g. \Shared</param>
        public FileSystemShare GetShareFromRelativePath(string relativePath)
        {
            if (relativePath.StartsWith(@"\"))
            {
                relativePath = relativePath.Substring(1);
            }

            int indexOfSeparator = relativePath.IndexOf(@"\");
            if (indexOfSeparator >= 0)
            {
                relativePath = relativePath.Substring(0, indexOfSeparator);
            }

            int index = IndexOf(relativePath, StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                return this[index];
            }
            else
            {
                return null;
            }
        }
    }
}
