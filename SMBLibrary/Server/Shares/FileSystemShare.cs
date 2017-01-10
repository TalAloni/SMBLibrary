/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
    public class FileSystemShare : ISMBShare
    {
        private string m_name;
        public List<string> ReadAccess;
        public List<string> WriteAccess;
        public IFileSystem FileSystem;

        public FileSystemShare(string shareName)
        {
            m_name = shareName;
        }

        public bool HasReadAccess(string userName)
        {
            return Contains(ReadAccess, userName);
        }

        public bool HasWriteAccess(string userName)
        {
            return Contains(WriteAccess, userName);
        }

        public static bool Contains(List<string> list, string value)
        {
            return (IndexOf(list, value) >= 0);
        }

        public static int IndexOf(List<string> list, string value)
        {
            for (int index = 0; index < list.Count; index++)
            {
                if (string.Equals(list[index], value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return index;
                }
            }
            return -1;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }
    }
}
